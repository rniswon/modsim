using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Csu.Modsim.ModsimModel;
using RTI.CWR.SQLiteUtils;

namespace RTI.CWR.MODSIMUtils.RRModelOps
{
    class ReleaseCapacity 
    {
        private DataTable dataTbl;
        private Dictionary<string, Link> ResNodesCollection;
        private double accuracyFactor;

        public ReleaseCapacity(string DBSource,string tableName, ref Model m_Model)
        {
            MyDBSqlite m_DB = new MyDBSqlite(DBSource);
            dataTbl = m_DB.GetTableFromDB($"SELECT * FROM {tableName}",tableName);
            accuracyFactor = m_Model.ScaleFactor;

            //Get the Minimum flows Nodes
            Link m_ResLink;
            DataTable linksTbl = m_DB.GetTableFromDB($"SELECT Link FROM {tableName} GROUP BY Link", tableName);
            ResNodesCollection = new Dictionary<string, Link>();
            foreach (DataRow dr in linksTbl.Rows)
            {
                m_ResLink = m_Model.FindLink(dr["Link"].ToString());
                if (m_ResLink.m.maxConstant > 0)
                {
                    m_ResLink.mlInfo.hiVariable = new long[m_Model.TimeStepManager.noModelTimeSteps, 1];
                    for (int val  = 0; val< m_ResLink.mlInfo.hiVariable.Length;val++)
                        m_ResLink.mlInfo.hiVariable[val,0] = m_ResLink.m.maxConstant;
                    m_ResLink.m.maxConstant = 0;
                }
                ResNodesCollection.Add(dr["Link"].ToString(), m_ResLink);
            }
        }

        /// <summary>
        /// Computes the link capacity based on the provided reservoir current average elevation
        /// It relies on a table stored in a database (provided) that has  the 'Elevation'-'Capacity' relationship for each link to be set.
        /// The database table should have the following columns: | Link | Elevation_ft | Capacity_cfs |
        /// The table units are converted to MODSIM [ac-ft/day]
        /// </summary>
        /// <param name="CurrentModelTimeStepIndex"></param>
        /// <param name="resNode">Reservoir node with average elevation to set the release capacity.</param>
        /// <param name="interpolate">result will be interpolated from the entries in the table.</param>
        /// <returns></returns>
        public void SetReleaseCapacity(int CurrentModelTimeStepIndex, Node resNode, bool interpolate)
        {
            //set the minimum flow value in all demands
            double avg_Elev = resNode.mnInfo.avg_elevation;
            foreach (string key in ResNodesCollection.Keys)
            {
                double relCap1 = 0;
                double elev1 = 0;
                double relCap2 = 0;
                double elev2 = 0;

                Link m_link = ResNodesCollection[key];

                DataRow[] drs1 = dataTbl.Select($"Link = '{m_link.name}' AND Elevation_ft <= {avg_Elev}", "Elevation_ft DESC");
                if (drs1.Length == 0)
                {
                    relCap1 = 0;
                }
                else
                {
                    relCap1 = double.Parse(drs1[0]["Capacity_cfs"].ToString()) * 1.98347 * accuracyFactor; //assume MODSIM in acre-ft per day
                    elev1 = double.Parse(drs1[0]["Elevation_ft"].ToString());
                    if (interpolate)
                    {
                        DataRow[] drs2 = dataTbl.Select($"Link = '{m_link.name}' AND Elevation_ft > {avg_Elev}", "Elevation_ft");
                        //relCap1 will be used if interpolate is FALSE
                        if (elev1 != avg_Elev)
                        {
                            if (drs2.Length >= 1)
                            {
                                relCap2 = double.Parse(drs2[0]["Capacity_cfs"].ToString()) * 1.98347 * accuracyFactor; //assume MODSIM in acre-ft per day
                                elev2 = double.Parse(drs2[0]["Elevation_ft"].ToString());
                                relCap1 = relCap1 + ((relCap2 - relCap1) * (avg_Elev - elev1) / (elev2 - elev1));
                            }
                            else
                            {
                                Console.WriteLine($"ERROR [setting relase capacity] Unexpected number of rows returned for link {m_link.name}. Calcuation skipped.");
                                return;
                            }
                        }
                    }
                }
                //Setting upper bound value in MODSIM array
                //m_link.mlInfo.hiVariable[CurrentModelTimeStepIndex, 0] = (long)Math.Round(relCap1, 0);  // This is not used during the iterations!!!
                m_link.mlInfo.hi = (long)Math.Round(relCap1, 0);
                //Console.WriteLine($"    Setting {m_link.name} to : {relCap1} - elev: {avg_Elev} stor end: {resNode.mnInfo.stend}");
            }
        }
    }
}
