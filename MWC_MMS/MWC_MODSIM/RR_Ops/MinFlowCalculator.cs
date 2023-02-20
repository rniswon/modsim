using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using Csu.Modsim.ModsimModel;
using RTI.CWR.SQLiteUtils;

namespace RTI.CWR.MODSIMUtils.RRModelOps
{
    class MinFlowCalculator 
    {
        private DataTable dataTbl;
        private Dictionary<string, Node> minNodesCollection;
        private double accuracyFactor;

        public MinFlowCalculator(string DBSource,string tableName, ref Model m_Model)
        {
            MyDBSqlite m_DB = new MyDBSqlite(DBSource);
            dataTbl = m_DB.GetTableFromDB($"SELECT * FROM {tableName}",tableName);
            accuracyFactor = m_Model.ScaleFactor;

            //Get the Minimum flows Nodes
        Node m_MinNode;
            DataTable NodesTbl = m_DB.GetTableFromDB($"SELECT Node FROM { tableName} GROUP BY Node ", tableName);
            minNodesCollection = new Dictionary<string, Node>();
            foreach (DataRow dr in NodesTbl.Rows)
            {
                m_MinNode = m_Model.FindNode(dr["Node"].ToString());
                minNodesCollection.Add(dr["Node"].ToString(), m_MinNode);
            }
        }

        /// <summary>
        /// Retrieves the minimum flow for the TSID = 1, which is a function of the storage state and changes during the year (is a date function)
        /// The table accepts any combination of month, day and repeates the value in the monht until a new one is found.  
        /// Flow values are converted to ac-ft per day (hardcoded).
        /// </summary>
        /// <param name="CurrentModelTimeStepIndex"></param>
        /// <param name="thisDate"></param>
        /// <param name="stateCol"></param>
        /// <returns></returns>
        public void AssignMinFlowsToNodes(int CurrentModelTimeStepIndex, DateTime thisDate, string stateCol, int lags)
        {
            //set the minimum flow value in all demands
            //long minFlow = 0;
            for (int i = 0; i < lags; i++)
            {
                if (int.Parse(stateCol) == 0) return;// minFlow;
                foreach (string key in minNodesCollection.Keys)
                {
                    Node node = minNodesCollection[key];
                    DateTime m_Date = thisDate.AddDays(i);
                    DataRow[] drs = dataTbl.Select($"Node = '{node.name}' AND Month = {m_Date.Month} AND [Day] = {m_Date.Day}");
                    if (drs.Length == 0)
                        drs = dataTbl.Select($"Node = '{node.name}' AND Month = {m_Date.Month} AND [Day]< {m_Date.Day}", "Day DESC");
                    if (drs.Length >= 1)
                    {
                        long minFlow = long.Parse(drs[0][stateCol].ToString());
                        //min flow value in CFS - needs to be converted to standard MODSIM english units [acre-ft]
                        minFlow = (long)Math.Round(minFlow * accuracyFactor * 1.98347, 0); // Includes the buffer min flow in the db table
                        if(node.mnInfo.nodedemand.Length > CurrentModelTimeStepIndex + i) 
                            node.mnInfo.nodedemand[CurrentModelTimeStepIndex+i, 0] = minFlow;
                        //Console.WriteLine($"Setting {node.name} to : {minFlow}");
                    }
                    else
                    {
                        Console.WriteLine($"ERROR [setting minimum flows] Unexpected number of rows returned for node {node.name}. Calcuation skipped.");
                    }
                }
            }
            //return minFlow;
        }

        /// <summary>
        /// This fuction retrieves the minimum flow for the TSID = 2.
        /// </summary>
        /// <param name="CurrentModelTimeStepIndex"></param>
        /// <param name="stateCol">This is the year type number. Only populated for 2 and 3 in the Mendocino implementation.</param>
        /// <returns></returns>
        public long AssignMinFlowsToNodes(int CurrentModelTimeStepIndex, string stateCol)
        {
            //set the minimum flow value in all demands
            long minFlow = 0;
            foreach (string key in minNodesCollection.Keys)
            {
                Node node = minNodesCollection[key];
                DataRow[] drs = dataTbl.Select($"TSID = 2 AND Node = '{node.name}'");
                if (drs.Length == 1)
                {
                    minFlow = long.Parse(drs[0][stateCol].ToString());
                    //min flow value in CFS - needs to be converted to standard MODSIM english units [acre-ft]
                    minFlow = (long)(minFlow * accuracyFactor * 1.98347); // Includes the buffer min flow in the db table
                    node.mnInfo.nodedemand[CurrentModelTimeStepIndex, 0] = minFlow;
                    //Console.WriteLine($"Setting {node.name} to : {minFlow}");
                }
                else
                {
                    Console.WriteLine($"ERROR [setting minimum flows] Unexpected number of rows returned for node {node.name} on TSID = 2. Calcuation skipped.");
                }
            }
            return minFlow;
        }

        internal int storageState;
        internal int GetStorageState(Model m_Model, long LMendocinoStor, long pillsburyStorage)
        {
            double combStorCap = 160370.0;
            //StorageState variable.
            DateTime myDate = m_Model.TimeStepManager.Index2Date(m_Model.mInfo.CurrentModelTimeStepIndex, TypeIndexes.ModelIndex);
            DateTime thisYearCheck = new DateTime(myDate.Year, 5, 31);
            //Set the state in May 31st for the Case 1 the remaining of the year.
            double LMStorCombined = (LMendocinoStor + pillsburyStorage) / m_Model.ScaleFactor;
            if (myDate >= thisYearCheck)
            {
                if (myDate == thisYearCheck)
                {
                    //'Evaluates the state in May 31st and stays for the remaining of the year
                    if (LMStorCombined >= 150000 )//| LMStorCombined / combStorCap > 0.9)
                        storageState = 1;
                    else if (LMStorCombined >= 130000)// | LMStorCombined / combStorCap > 0.8)
                        storageState = 2;
                    else
                        storageState = 4;
                }
            }
            else
                storageState = 1;

            if (myDate >= new DateTime(myDate.Year, 10, 1) && (LMendocinoStor / m_Model.ScaleFactor) < 30000)
                storageState = 3;

            return storageState;
        }
    }
}
