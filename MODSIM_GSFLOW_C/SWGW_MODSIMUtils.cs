using Csu.Modsim.ModsimModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;

namespace MODSIM_GSFLOW_C
{
    class SWGW_MODSIMUtils
    {
        public DataTable m_SyncTblSEG, m_SyncTblRES, m_SyncTblSettings; //, m_SyncTblDIV;
        public Int32 maxNoIterations, iterCount;
        private Model myModel;

        public SWGW_MODSIMUtils(ref Model m_Model)
        {
            myModel = m_Model;
        }

        public void PrepareMODSIMNetwork(string m_TblPath, double EXCHNGVol_Tolerance, double LAKEVol_Tolerance)
        {
            SqliteHelper sqliteHelper = new SqliteHelper(m_TblPath);
            //MWH.MWHUtils.GeneralUtils.MyDBUtils m_DBUtils = new MWH.MWHUtils.GeneralUtils.MyDBUtils(m_TblPath);
            string m_Sql = "SELECT [MS-GSF_mapping_info].[Link Name], [MS-GSF_mapping_info].[iseg], [MS-GSF_mapping_info].[Diversion], [MS-GSF_mapping_info].ResRelease, [MS-GSF_mapping_info].AssocRes FROM [MS-GSF_mapping_info] ORDER BY [MS-GSF_mapping_info].iseg;";
            m_SyncTblSEG = sqliteHelper.GetTableFromDB(m_Sql, "SegmentSync"); //"SELECT Modsim_Streams.MF_iseg, Modsim_Streams.MOD_Name FROM Modsim_Streams WHERE (((Modsim_Streams.MF_iseg) Is Not Null)) GROUP BY Modsim_Streams.MF_iseg, Modsim_Streams.MOD_Name;", "Streams");
            m_SyncTblSEG.Columns.Add("adjted", typeof(System.Int32));

            // Initialize column values
            foreach (DataRow m_row in m_SyncTblSEG.Rows)
            {
                m_row["adjted"] = 0;
            }

            // Get Reservoir mapping table 
            m_Sql = "SELECT * FROM [MS-GSF_Lake_Mapping_Info] ORDER BY [MS-GSF_Lake_Mapping_Info].GSF_LAK_ID;";
            m_SyncTblRES = sqliteHelper.GetTableFromDB(m_Sql, "ReservoirSync");

            // Get the settings from the database
            m_Sql = "SELECT * FROM [Settings];";
            m_Sql = "SELECT * FROM [Settings];";
            m_SyncTblSettings = sqliteHelper.GetTableFromDB(m_Sql, "Settings");

            // Assign settings
            foreach (DataRow mrow in m_SyncTblSettings.Rows)
            {
                if ((string)mrow["Key"] == "MaxIter") { maxNoIterations = int.Parse(mrow["Value"].ToString()); iterCount = 0; }
                if ((string)mrow["Key"] == "FLowTolerance") { EXCHNGVol_Tolerance = Convert.ToDouble(mrow["Value"]); }
                if ((string)mrow["Key"] == "VolumeTolerance") { LAKEVol_Tolerance = Convert.ToDouble(mrow["Value"]); }
            }

            // Create GW-SW Sink Node
            Node m_Sink = myModel.AddNewNode(true);
            m_Sink.nodeType = NodeType.Sink;
            //m_Sink.graphics.point.X = -1150;
            //m_Sink.graphics.point.Y = 5000;
            m_Sink.name = "MF_SINK";

            // Create GW-SW Source Node
            Node m_Source = myModel.AddNewNode(true);
            m_Source.nodeType = NodeType.NonStorage;
            //m_Source.graphics.point.X = 7200;
            //m_Source.graphics.point.Y = 180;
            m_Source.name = "MF_SOURCE";
            DataTable m_TSTbl = m_Source.m.adaInflowsM.dataTable;
            SetDefaultTableValue(ref m_TSTbl, 9900000000);

            // Connect both of just instantiated nodes so that unused source water 
            // is shunted out of the model through the sink Node
            Link m_Link = myModel.AddNewLink(true);
            Utils.ConnectFromNode(m_Link, m_Source);
            Utils.ConnectToNode(m_Link, m_Sink);
            m_Link.name = "MF_SINK_TO_SOURCE";
            m_Link.m.cost = -1;

            // Create Depletion Links
            count = 0;
            foreach (DataRow mrow in m_SyncTblSEG.Rows)
            {
                try
                {
                    Link baseLink;
                    if (mrow["Link Name"].ToString() != "")
                    {
                        baseLink = myModel.FindLink((string)mrow["Link Name"]);
                    }
                    else
                    {
                        baseLink = null;
                    }
                    if (baseLink != null)
                    {
                        //Depletions to a link simulated at the downstream node of the MODSIM link.
                        CreateDepAccLinks(m_Sink, m_Source, baseLink.to, baseLink.name);
                    }
                }
                finally { }
            }
            for (Node m_res = myModel.firstNode; m_res != null; m_res = m_res.next)
            {
                //TODO: Check if the reservoir modeling approach is consistent here
                if (m_res.nodeType == NodeType.Reservoir)
                {
                    //TODO: if water is be made avaialble to the owners, this code might need to be modified.
                    //      Is possible the balancing routine take care of the additions/losses.
                    CreateDepAccLinks(m_Sink, m_Source, m_res, m_res.name);
                }
            }
        }

        private int count;
        private void CreateDepAccLinks(Node m_Sink, Node m_Source, Node m_Node, String baseName)
        {
            //Create Depletion Link
            Link m_DepLink = myModel.AddNewLink(true);
            Utils.ConnectFromNode(m_DepLink, m_Node);
            Utils.ConnectToNode(m_DepLink, m_Sink);
            m_DepLink.name = "MF_Dep_" + baseName;
            m_DepLink.m.cost = -500000 - count;
            DataTable m_TSTbl = m_DepLink.m.maxVariable.dataTable;
            SetDefaultTableValue(ref m_TSTbl, 0);
            //Create Accretion Link
            Link m_AccLink = myModel.AddNewLink(true);
            Utils.ConnectFromNode(m_AccLink, m_Source);
            Utils.ConnectToNode(m_AccLink, m_Node);
            m_AccLink.name = "MF_Acc_" + baseName;
            m_AccLink.m.cost = -500000 - count;
            m_TSTbl = m_AccLink.m.maxVariable.dataTable;
            SetDefaultTableValue(ref m_TSTbl, 0);
            count++;
        }

        private void SetDefaultTableValue(ref DataTable m_Tbl, long value)
        {
            DataRow tsRow = m_Tbl.NewRow();
            tsRow[0] = myModel.TimeStepManager.dataStartDate;
            tsRow[1] = value;
            m_Tbl.Rows.Add(tsRow);
        }

    }
}
