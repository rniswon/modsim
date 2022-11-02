using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.OleDb;
using Csu.Modsim.ModsimModel;
using Csu.Modsim.ModsimIO;
using RTI.CWR.MWC_MODSIMUtils;
using System.IO;
using MODSIM_GSFLOW_C;

namespace RRModelingSystem
{
    public delegate void ProcessMessage(string msg);  // delegate
    public partial class DataProcessing : UserControl
    {
        public event ProcessMessage messageOut; // event

        public string ProjectDB { get; set; }
        //public string ModsimFile { get; set; }

        public string ProcessModsimFile { get; set; }
        private string _outFileName;
        private string _syncDBFileName;

        public OleDbConnectionStringBuilder oleConnBuilder = new OleDbConnectionStringBuilder();
        private MyDBSqlite m_DBUtils;

        private Dictionary<string, int> _tsTypeIDs = null;

        private Model myModel;
        private DateTime dtMODFLOWstart;
        private DataTable featuresTbl;

        public DataProcessing(string dbFile, string MODSIMFile, string controlFile, string syncingDB)
        {
            InitializeComponent();

            oleConnBuilder.Provider = "Microsoft.ACE.OLEDB.12.0";
            oleConnBuilder.DataSource = "";

            ProjectDB = dbFile;
            m_DBUtils = new MyDBSqlite(ProjectDB);
            m_DBUtils.messageOut += PrintMessage;

            _syncDBFileName = syncingDB;

            //ModsimFile = textBox1.Text;
            ProcessModsimFile = MODSIMFile;

            Load_ControlFileInfo(controlFile);

            Simulation_Load(null, null);

            Load_DBInfo();

            //check for only new option
            comboBoxTSTypes_SelectedIndexChanged(null, null);

            for (int i = 1; i <= 12; i++)
            {
                dataGridViewMonthlyFactors.Rows.Add(new object[] { i, null });
            }

            //tabControl1.TabPages.Remove(tabControl1.TabPages[2]);
            //tabControl1.TabPages.Remove(tabControl1.TabPages[1]);
            tabControl2.TabPages.Remove(tabControl2.TabPages["tabPageMODSIMImport"]);
        }

        private void Load_DBInfo()
        {
            if (File.Exists(m_DBUtils.dbFile))
            {
                string tsQuery = $"SELECT * FROM Features";
                featuresTbl = m_DBUtils.GetTableFromDB(tsQuery, "Features");
                dataGridViewFeat.DataSource = featuresTbl;

                tsQuery = $"SELECT * FROM TSTypes";
                DataTable TSTypeTbl = m_DBUtils.GetTableFromDB(tsQuery, "TSTypes");
                DataRow dr = TSTypeTbl.NewRow();
                dr["TSName"] = "<< New >>";
                TSTypeTbl.Rows.Add(dr);
                dataGridViewTSType.DataSource = TSTypeTbl;

                comboBoxTSTypes.DataSource = TSTypeTbl;
                comboBoxTSTypes.DisplayMember = "TSName";

                comboBoxTSTypes3.DataSource = TSTypeTbl;
                comboBoxTSTypes3.DisplayMember = "TSName";

                comboBoxTSTypes2.Items.Clear();
                foreach (DataRow dr2 in TSTypeTbl.Rows)
                {
                    comboBoxTSTypes2.Items.Add(dr2["TSName"]);
                }

                DataTable TSTypeTbl2 = m_DBUtils.GetTableFromDB(tsQuery, "Features");
                comboBoxDSetTSTypes.DataSource = TSTypeTbl2;
                comboBoxDSetTSTypes.DisplayMember = "TSName";
            }
        }

        private void Load_ControlFileInfo(string controlFile)
        {
            if (File.Exists(controlFile))
            {
                richTextBoxGSOut.Text = $"Active Preferences \nControl File:\n  {controlFile}\n";
                TextUtils.MessageOut -= PrintMessage;
                TextUtils.MessageOut += PrintMessage;
                List<string> dateParts = TextUtils.ReadControlProperties(controlFile, new string[] { "modflow_time_zero" });
                dtMODFLOWstart = new DateTime(int.Parse(dateParts[1].ToString()), int.Parse(dateParts[2].ToString()), int.Parse(dateParts[3].ToString()), int.Parse(dateParts[4].ToString()), int.Parse(dateParts[5].ToString()), int.Parse(dateParts[6].ToString()));
                richTextBoxGSOut.AppendText($"MODFLOW time zero:\n  {dtMODFLOWstart.ToShortDateString()}\n");

                string baseFolder = Path.GetDirectoryName(controlFile);
                List<string> modFileName = TextUtils.ReadControlProperties(controlFile, new string[] { "modflow_name" });
                string _MODFLOWName = Path.Combine(baseFolder, modFileName[1]);
                richTextBoxGSOut.AppendText($"MODFLOW name file:\n  {_MODFLOWName}\n");

                List<string> modOutFileName = TextUtils.ReadControlProperties(_MODFLOWName, new string[] { "DATA", "511" }, 0);
                //baseFolder = Path.GetDirectoryName(_MODFLOWName);
                _outFileName = Path.Combine(baseFolder, modOutFileName[0]);
                richTextBoxGSOut.AppendText($"MODFLOW output file:\n    {_outFileName}\n");
            }

        }

        private void buttonNetProcess_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {

                PrintMessage("Reading base MODSIM file ...");
                PrintMessage(ProcessModsimFile);

                myModel = new Model();
                XYFileReader.Read(myModel, ProcessModsimFile);

                PrintMessage("  Processing flags in diversion segments...");

                List<string> POUsList = new List<string>();
                foreach (Node n in myModel.Nodes_NonStorage)
                {
                    if (n.name.Contains("_Diversion"))
                    {
                        //Found a POU diversion point
                        string POUName = n.name.Replace("_Diversion", "");
                        POUsList.Add(POUName);
                    }
                }

                //Process construct for each POU
                foreach (string POU in POUsList)
                {
                    // Get the diversion node
                    Node diversionNode = myModel.FindNode(POU + "_Diversion");
                    Node wrNode = null;
                    List<string> segments = new List<string>();
                    LinkList ll = diversionNode.OutflowLinks;
                    bool firstLink = true;
                    double[] x2y2 = new double[2];
                    double[] newCoords = new double[2];
                    while (ll != null)
                    {
                        //get the segment link
                        Link l = ll.link;
                        PrintMessage($"  Processing segment {l.name} ...");
                        double[] x1y1 = new double[2] { l.from.graphics.nodeLoc.X * 1.0, l.from.graphics.nodeLoc.Y * 1.0 };
                        if (firstLink)
                        {
                            //Create space for WR processing
                            wrNode = myModel.AddNewNode(true);
                            wrNode.nodeType = NodeType.NonStorage;
                            wrNode.name = diversionNode.name + "_WR";
                            wrNode.graphics.nodeLoc.X = diversionNode.graphics.nodeLoc.X;
                            wrNode.graphics.nodeLoc.Y = diversionNode.graphics.nodeLoc.Y;

                            x2y2 = new double[2] { l.to.graphics.nodeLoc.X, l.to.graphics.nodeLoc.Y };
                            firstLink = false;

                            newCoords = CalculateDivCoords(x2y2[0], x2y2[1], x1y1[0], x1y1[1], gridNo: 0, xStep: -1, yPosition: 0);

                            Link m_Link = myModel.AddNewLink(true);
                            m_Link.name = $"{diversionNode.name}{wrNode.name}";
                            Utils.ConnectFromNode(m_Link, diversionNode);
                            Utils.ConnectToNode(m_Link, wrNode);
                        }
                        string[] info = l.description.Split('|');
                        if (info.Length == 2)
                        {
                            string segmentNo = info[0].Replace("ISEG_", "");
                            //Disconnect the segment link
                            Utils.DisConnectFromNode(l);
                            Utils.DisConnectToNode(l);


                            if (info[1].Contains("AG_FLG:1"))
                            {
                                //Process Ag demand
                                //Find or create the ag demand node 
                                Node demNode = ProcessDemandNode(POU + "_DemAg", POU, xstep: 4, yPosition: 1, x1y1, x2y2);
                                Node demNode1 = ProcessDemandNode(POU + "_DemOthers", POU, xstep: 4, yPosition: 2, x1y1, x2y2);

                                //if (storageON)
                                //{
                                //    //Process storage
                                //    Node resNode = ProcessNode(POU + "_RES", xstep: 2, yPosition: 0, x1y1, x2y2, NodeType.Reservoir);

                                //    Node segNode = ProcessNode(POU + "_STO", xstep: 1, yPosition: 0, x1y1, x2y2, NodeType.NonStorage);

                                //    //Reconnect segment link
                                //    Utils.ConnectFromNode(l, segNode);
                                //    Utils.ConnectToNode(l, resNode);

                                //    //Create the diversion link
                                //    CreateConnectionLink(wrNode, segNode);

                                //    Node segNode2 = ProcessNode(POU + "_AG", xstep: 3, yPosition: 1, x1y1, x2y2, NodeType.NonStorage);


                                //    //Create the GSFLOW return link (Storage)
                                //    Link m_Link = myModel.FindLink($"{segmentNo}_StoTOAg");
                                //    if (m_Link == null)
                                //    {
                                //        m_Link = myModel.AddNewLink(true);
                                //        m_Link.name = $"{segmentNo}_StoTOAg";
                                //        Utils.ConnectFromNode(m_Link, resNode);
                                //        Utils.ConnectToNode(m_Link, segNode2);
                                //    }

                                //    //Create the demand connection
                                //    CreateDEMLink(demNode, segNode2);

                                //}
                                //else
                                //{
                                Node segNode = ProcessNode(POU + "_NOSTO-AG", xstep: 1, yPosition: 2, x1y1, x2y2, NodeType.NonStorage);

                                Node segNode_ = ProcessNode(POU + "_NOSTO-AGSplit", xstep: 2, yPosition: 2, x1y1, x2y2, NodeType.NonStorage);

                                Node segNode1 = ProcessNode(POU + "_AG", xstep: 3, yPosition: 1, x1y1, x2y2, NodeType.NonStorage);

                                Node segNode2 = ProcessNode(POU + "_NOAG1", xstep: 3, yPosition: 2, x1y1, x2y2, NodeType.NonStorage);

                                //Reconnect segment link
                                Utils.ConnectFromNode(l, segNode);
                                Utils.ConnectToNode(l, segNode_);// segNode1);

                                //Create the demand connection
                                CreateDEMLink(demNode, segNode1, -4);
                                CreateDEMLink(demNode1, segNode2, -3);

                                //Create the diversion link
                                CreateConnectionLink(wrNode, segNode);
                                //Create Ag split links
                                CreateConnectionLink(segNode_, segNode1);
                                CreateConnectionLink(segNode_, segNode2);
                                //}
                            }

                            if (info[1].Contains("NONAG:1"))
                            {
                                //Process Non-Ag demands 
                                //Find the ag demand node already created/renamed
                                Node demNode = ProcessDemandNode(POU + "_DemOthers", POU, xstep: 4, yPosition: 2, x1y1, x2y2);
                                Node demNode2 = ProcessDemandNode(POU + "_DomOutdoor", POU, xstep: 4, yPosition: 3, x1y1, x2y2);
                                Node demNode3 = ProcessDemandNode(POU + "_DomIndoor", POU, xstep: 4, yPosition: 4, x1y1, x2y2);

                                //if (storageON)
                                //{
                                //    //Process storage

                                //    Node resNode = ProcessNode(POU + "_RES", xstep: 2, yPosition: 0, x1y1, x2y2, NodeType.Reservoir);

                                //    Node segNode = ProcessNode(POU + "_STO", xstep: 1, yPosition: 0, x1y1, x2y2, NodeType.NonStorage);

                                //    //Reconnect segment link
                                //    Utils.ConnectFromNode(l, segNode);
                                //    Utils.ConnectToNode(l, resNode);

                                //    //Create the diversion link
                                //    CreateConnectionLink(wrNode, segNode);

                                //    //Create connecting nodes
                                //    Node segNode2 = ProcessNode(POU + "_NOAG1", xstep: 3, yPosition: 2, x1y1, x2y2, NodeType.NonStorage);
                                //    Node segNode3 = ProcessNode(POU + "_NOAG2", xstep: 3, yPosition: 3, x1y1, x2y2, NodeType.NonStorage);
                                //    Node segNode4 = ProcessNode(POU + "_NOAG3", xstep: 3, yPosition: 4, x1y1, x2y2, NodeType.NonStorage);

                                //    //Create Reservoir connections
                                //    CreateConnectionLink(resNode, segNode2);
                                //    CreateConnectionLink(resNode, segNode3);
                                //    CreateConnectionLink(resNode, segNode4);

                                //    //Create the demand connections
                                //    CreateDEMLink(demNode, segNode2);
                                //    CreateDEMLink(demNode2, segNode3);
                                //    CreateDEMLink(demNode3, segNode4);


                                //}
                                //else
                                //{
                                Node segNode = ProcessNode(POU + "_NOSTO-NOAG", xstep: 1, yPosition: 3, x1y1, x2y2, NodeType.NonStorage);

                                Node segNode_ = ProcessNode(POU + "_NOAG", xstep: 2, yPosition: 3, x1y1, x2y2, NodeType.NonStorage);

                                //Reconnect segment link
                                Utils.ConnectFromNode(l, segNode);
                                Utils.ConnectToNode(l, segNode_);

                                //Create the diversion link
                                CreateConnectionLink(wrNode, segNode);

                                //Create connecting nodes
                                Node segNode2 = ProcessNode(POU + "_NOAG1", xstep: 3, yPosition: 2, x1y1, x2y2, NodeType.NonStorage);
                                Node segNode3 = ProcessNode(POU + "_NOAG2", xstep: 3, yPosition: 3, x1y1, x2y2, NodeType.NonStorage);
                                Node segNode4 = ProcessNode(POU + "_NOAG3", xstep: 3, yPosition: 4, x1y1, x2y2, NodeType.NonStorage);

                                //Create the demand connections
                                CreateDEMLink(demNode, segNode2, -3);
                                CreateDEMLink(demNode2, segNode3, -2);
                                CreateDEMLink(demNode3, segNode4, -5);

                                //Create NonSto-NoAG connections
                                CreateConnectionLink(segNode_, segNode2);
                                CreateConnectionLink(segNode_, segNode3);
                                CreateConnectionLink(segNode_, segNode4);

                                //}
                            }


                            if (info[1].Contains("STO:1") || checkBoxAllSTO.Checked)
                            {
                                //Process storage
                                Node resNode = ProcessNode(POU + "_RES", xstep: 2, yPosition: 0, x1y1, x2y2, NodeType.Reservoir);
                                //setting cost in the reservoir layers
                                resNode.m.resBalance = new ResBalance();
                                resNode.m.resBalance.incrPriorities = new long[] { 1 };
                                resNode.m.resBalance.targetPercentages = new double[] { 100 };
                                resNode.m.resBalance.PercentBasedOnMaxCapacity = true;


                                Node segNode = ProcessNode(POU + "_STO", xstep: 1, yPosition: 0, x1y1, x2y2, NodeType.NonStorage);

                                //Reconnect segment link
                                Utils.ConnectFromNode(l, segNode);
                                Utils.ConnectToNode(l, resNode);
                                l.m.cost = +1;  //cost for link into the reservoir

                                //Create the diversion link
                                CreateConnectionLink(wrNode, segNode);

                                //if (info[1].Contains("AG_FLG:1"))
                                //{
                                ////Process storage
                                //Node resNode = ProcessNode(POU + "_RES", xstep: 2, yPosition: 0, x1y1, x2y2, NodeType.Reservoir);

                                //Node segNode = ProcessNode(POU + "_STO", xstep: 1, yPosition: 0, x1y1, x2y2, NodeType.NonStorage);

                                ////Reconnect segment link
                                //Utils.ConnectFromNode(l, segNode);
                                //Utils.ConnectToNode(l, resNode);

                                ////Create the diversion link
                                //CreateConnectionLink(wrNode, segNode);

                                //Node segNode2 = ProcessNode(POU + "_AG", xstep: 3, yPosition: 1, x1y1, x2y2, NodeType.NonStorage);
                                Node segNode1 = ProcessNode(POU + "_AG", xstep: 3, yPosition: 1, x1y1, x2y2, NodeType.NonStorage);


                                //Create the GSFLOW return link (Storage)
                                //  Associated with the POU not the storage segment
                                CreateConnectionLink(resNode, segNode1, $"{POU}_StoTOAg");

                                //Link m_Link = myModel.FindLink($"{POU}_StoTOAg");
                                //    if (m_Link == null)
                                //    {
                                //        m_Link = myModel.AddNewLink(true);
                                //        m_Link.name = $"{POU}_StoTOAg";
                                //        Utils.ConnectFromNode(m_Link, resNode);
                                //        Utils.ConnectToNode(m_Link, segNode1);
                                //    }

                                ////Create the demand connection
                                //CreateDEMLink(demNode, segNode1);

                                //}
                                //if (info[1].Contains("NONAG:1"))
                                //{
                                ////Process storage

                                //Node resNode = ProcessNode(POU + "_RES", xstep: 2, yPosition: 0, x1y1, x2y2, NodeType.Reservoir);

                                //Node segNode = ProcessNode(POU + "_STO", xstep: 1, yPosition: 0, x1y1, x2y2, NodeType.NonStorage);

                                ////Reconnect segment link
                                //Utils.ConnectFromNode(l, segNode);
                                //Utils.ConnectToNode(l, resNode);

                                ////Create the diversion link
                                //CreateConnectionLink(wrNode, segNode);

                                //Create connecting nodes
                                Node segNode2 = ProcessNode(POU + "_NOAG1", xstep: 3, yPosition: 2, x1y1, x2y2, NodeType.NonStorage);
                                Node segNode3 = ProcessNode(POU + "_NOAG2", xstep: 3, yPosition: 3, x1y1, x2y2, NodeType.NonStorage);
                                Node segNode4 = ProcessNode(POU + "_NOAG3", xstep: 3, yPosition: 4, x1y1, x2y2, NodeType.NonStorage);

                                //Create Reservoir connections
                                CreateConnectionLink(resNode, segNode2);
                                CreateConnectionLink(resNode, segNode3);
                                CreateConnectionLink(resNode, segNode4);

                                ////Create the demand connections
                                //CreateDEMLink(demNode, segNode2);
                                //CreateDEMLink(demNode2, segNode3);
                                //CreateDEMLink(demNode3, segNode4);
                                //}
                            }

                        }
                        else
                            PrintMessage($"  [ERROR] unexpected flag arguments on the link {l.name} description. Skipping processing.");

                        ll = ll.next;
                    }

                    //Relocate the diversion point.
                    //  Has to be done afte the strucutre processing (why?)
                    diversionNode.graphics.nodeLoc.X = (float)newCoords[0];
                    diversionNode.graphics.nodeLoc.Y = (float)newCoords[1];

                }

                XYFileWriter.Write(myModel, ProcessModsimFile.Replace(".xy", "_Div.xy"));

                PrintMessage($"Finished. \n Saved file as: {myModel.fname}");
            }
            catch (Exception ex)
            {
                PrintMessage(ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void CreateConnectionLink(Node diversionNode, Node segNode, string nameOverwrite = "")
        {
            string lName = $"{diversionNode.name}_{segNode.name}";
            if (nameOverwrite != "")
                lName = nameOverwrite;
            Link m_Link2 = myModel.FindLink(lName);
            if (m_Link2 == null)
            {
                m_Link2 = myModel.AddNewLink(true);
                Utils.ConnectFromNode(m_Link2, diversionNode);
                Utils.ConnectToNode(m_Link2, segNode);
                m_Link2.name = lName;
            }
        }

        private void CreateDEMLink(Node demNode, Node segNode, int cost)
        {
            Link m_Link3 = myModel.FindLink($"{demNode.name}_Cost");
            if (m_Link3 == null)
            {
                m_Link3 = myModel.AddNewLink(true);
                Utils.ConnectFromNode(m_Link3, segNode);
                Utils.ConnectToNode(m_Link3, demNode);
                m_Link3.name = $"{demNode.name}_Cost";
                m_Link3.m.cost = cost;
            }
        }

        private Node ProcessNode(string vName, int xstep, int yPosition, double[] x1y1, double[] x2y2, NodeType nType)
        {
            Node m_Node = myModel.FindNode(vName);
            if (m_Node == null)
            {
                double[] newCoords = CalculateDivCoords(x2y2[0], x2y2[1], x1y1[0], x1y1[1], gridNo: 0, xstep, yPosition);
                m_Node = myModel.AddNewNode(true);
                m_Node.graphics.nodeLoc.X = (float)newCoords[0];
                m_Node.graphics.nodeLoc.Y = (float)newCoords[1];
                m_Node.nodeType = nType;
                m_Node.name = vName;
            }
            return m_Node;
        }

        private Node ProcessDemandNode(string vName, string POU, int xstep, int yPosition, double[] x1y1, double[] x2y2)
        {
            Node demNode = myModel.FindNode(vName);
            if (demNode == null)
            {
                Node iniDemNode = myModel.FindNode(POU);
                if (iniDemNode != null)
                {
                    //    Uses the original demand node
                    iniDemNode.name = vName;
                    demNode = myModel.FindNode(vName);
                }
                else
                {
                    demNode = myModel.AddNewNode(true);
                    demNode.nodeType = NodeType.Demand;
                    demNode.name = vName;
                }
                double[] newCoords = CalculateDivCoords(x2y2[0], x2y2[1], x1y1[0], x1y1[1], gridNo: 0, xStep: xstep, yPosition: yPosition);
                demNode.graphics.nodeLoc.X = (float)newCoords[0];
                demNode.graphics.nodeLoc.Y = (float)newCoords[1];
            }
            return demNode;
        }

        private double[] CalculateDivCoords(double x2, double y2, double x1, double y1, int gridNo, int xStep, int yPosition)
        {
            double scalex = -200 * 0.9;
            double scaley = 200 * 0.9;
            double origx2 = x2;
            double origy2 = y2;
            double angle = Math.Atan((x2 - x1) / (y2 - y1));
            //Initial grid point
            double deltaX = Math.Sin(Math.PI / 2 - angle);
            double deltaY = Math.Abs(Math.Cos(Math.PI / 2 - angle));

            //Deltas start at x1,y1
            if (origy2 > y1)
                x2 = x1 + (deltaX * gridNo * scalex);
            else
                x2 = x1 - (deltaX * gridNo * scalex);

            if (origx2 > x1)
                y2 = y1 - (deltaY * gridNo * scaley);
            else
                y2 = y1 + (deltaY * gridNo * scaley);

            // grid position adjutment (step)
            double deltaX2 = Math.Sin(angle) * xStep * scalex / 2.0;
            if (origx2 > x1)
                x2 += deltaX2;
            else
                x2 -= deltaX2;
            double deltaY2 = Math.Abs(Math.Cos(angle)) * xStep * scaley / 2.0;
            if (origy2 > y1)
                y2 += deltaY2;
            else
                y2 -= deltaY2;

            // Find position in the grid
            if (origx2 > x1)
                x2 = x2 - (deltaX * (yPosition / 5.0) * scalex);
            else
                x2 = x2 + (deltaX * (yPosition / 5.0) * scalex);

            y2 = y2 - (deltaY * (yPosition / 5.0) * scaley);

            return new double[2] { x2, y2 };
        }

        private void Simulation_Load(object sender, EventArgs e)
        {
            LoadTSTypes();

            LoadScenarios();
        }

        private void buttonImportTS_Click(object sender, EventArgs e)
        {
            if (_tsTypeIDs.ContainsKey(cbTSTypeID.Text))
            {
                int tstypeid = _tsTypeIDs[cbTSTypeID.Text];

                // get list of features from the destination database
                DataTable featuredt = GetModsimFeatures(txtNewTSName.Text.Trim());
                if (featuredt.Rows.Count == 0) return;

                string MODSIMOutFile = ProcessModsimFile.Replace(".xy", "OUTPUT.sqlite");

                foreach (DataRow r in featuredt.Rows)
                {
                    switch (r["MOD_Type"].ToString())
                    {
                        case "Link":
                            ImportLinkTimeSeries(tstypeid, int.Parse(r["FeatureID"].ToString()), r["MOD_Name"].ToString(), MODSIMOutFile);
                            break;
                    }
                }
            }
        }

        private void buttonProcessData_Click(object sender, EventArgs e)
        {
            if (treeViewDatasets.SelectedNode == null)
            {
                PrintMessage("Error: select a timeseries dataset.");
                return;
            }

            if (!System.IO.File.Exists(ProcessModsimFile))
            {
                PrintMessage("Error: Modsim file does not exists.");
                return;
            }

            ProcessTimeSeriesDataSet();
            PrintMessage("Completed.");
        }





        private void LoadTSTypes()
        {
            if (File.Exists(m_DBUtils.dbFile))
            {
                string cmdtxt = $"SELECT TSTypeID, TSName FROM TSTypes;";
                DataTable dt = m_DBUtils.GetTableFromDB(cmdtxt, "TSType");//ExecuteCommand(cmdtxt);

                // add to colllection
                _tsTypeIDs = new Dictionary<string, int>();
                foreach (DataRow r in dt.Rows)
                {
                    _tsTypeIDs.Add(r["TSTypeID"].ToString() + " | " + r["TSName"].ToString(), int.Parse(r["TSTypeID"].ToString()));
                    cbTSTypeID.Items.Add(r["TSTypeID"].ToString() + " | " + r["TSName"].ToString());
                }
            }
            return;
        }

        private void LoadScenarios()
        {
            if (File.Exists(m_DBUtils.dbFile))
            {
                string cmdtxt = "SELECT ID, DSName FROM DatasetsInfo;";
                DataTable dt = m_DBUtils.GetTableFromDB(cmdtxt, "Scns"); //ExecuteCommand(cmdtxt);

                treeViewDatasets.Nodes.Clear();
                foreach (DataRow r in dt.Rows)
                {
                    treeViewDatasets.Nodes.Add(r["ID"].ToString(), r["DSName"].ToString());
                }
            }
        }

        private DataTable GetModsimFeatures(string featurematch)
        {
            string cmdtxt = $"SELECT FeatureID, MOD_Name, MOD_Type FROM Features WHERE MOD_Name LIKE '%{featurematch}%';";
            DataTable dt = m_DBUtils.GetTableFromDB(cmdtxt, "Features"); //ExecuteCommand(cmdtxt);
            return dt;
        }

        private void ImportLinkTimeSeries(int tstypeid, int featureid, string featurename, string ModsimFile)
        {
            OleDbConnection conn = null;
            try
            {
                string sql = "INSERT INTO TimeSeries " +
                        $"SELECT T.TSDate AS TSDate, '{featureid}' AS FeatureID, '{tstypeid}' AS TSTypeID, L.Flow AS TSValue FROM " +
                        "(" +
                        $"(SELECT* FROM LinksOutput IN '{ModsimFile}') AS L " +
                        $"INNER JOIN(SELECT* FROM TimeSteps IN '{ModsimFile}') AS T ON T.TSIndex = L.TSIndex " +
                        ") " +
                        $"WHERE L.LNumber = (SELECT LNumber FROM LinksInfo IN '{ModsimFile}' WHERE LName like '{featurename}'); ";
                int result = m_DBUtils.ExecuteQuery(sql);
                PrintMessage($"Feature: {featurename}  - Records affected: {result} in TimeSeries table.");

                //oleConnBuilder.DataSource = ProjectDB;
                //using (conn = new OleDbConnection(oleConnBuilder.ConnectionString))
                //{
                //    conn.Open();

                //    OleDbCommand cmd = conn.CreateCommand();
                //    cmd.CommandText = "INSERT INTO TimeSeries " +
                //        $"SELECT T.TSDate AS TSDate, '{featureid}' AS FeatureID, '{tstypeid}' AS TSTypeID, L.Flow AS TSValue FROM " +
                //        "(" +
                //        $"(SELECT* FROM LinksOutput IN '{ModsimFile}') AS L " +
                //        $"INNER JOIN(SELECT* FROM TimeSteps IN '{ModsimFile}') AS T ON T.TSIndex = L.TSIndex " +
                //        ") " +
                //        $"WHERE L.LNumber = (SELECT LNumber FROM LinksInfo IN '{ModsimFile}' WHERE LName like '{featurename}'); ";

                //    int result = cmd.ExecuteNonQuery();
                //    PrintMessage($"Feature: {featurename}  - Records affected: {result} in TimeSeries table.");
                //}
            }
            catch (Exception ex)
            {
                PrintMessage($"Error: " + ex.Message);
            }
            finally
            {
                if (conn != null && conn.State != ConnectionState.Closed)
                {
                    conn.Close();
                }
            }
        }

        private void ProcessTimeSeriesDataSet()
        {

            this.Cursor = Cursors.WaitCursor;
            string m_FileName = ProcessModsimFile;
            try
            {

                if (comboBoxTSFile.Text.Contains("_DIV.xy"))
                    ProcessModsimFile = ProcessModsimFile.Replace(".xy", "_DIV.xy");
                if (comboBoxTSFile.Text.Contains("_DIV_WR.xy"))
                    ProcessModsimFile = ProcessModsimFile.Replace(".xy", "_DIV_WR.xy");

                if (!radioButtonUseBase.Checked)
                {
                    string TSFile = ProcessModsimFile.Replace(".xy", "TS.xy");
                    if (!File.Exists(TSFile) || checkBoxResetTSFile.Checked)
                    {
                        PrintMessage($"Creating TS File");
                        File.Copy(ProcessModsimFile, TSFile, true);
                    }
                    ProcessModsimFile = TSFile;
                }

                PrintMessage($"Reading MODSIM file {ProcessModsimFile}...");

                Model modsim = new Model();
                XYFileReader.Read(modsim, ProcessModsimFile);

                // read the model start date
                DateTime startdate = modsim.TimeStepManager.dataStartDate;
                DateTime enddate = modsim.TimeStepManager.dataEndDate;
                if (checkBoxUpdateDates.Checked)
                {
                    startdate = dateTimePickerStart.Value;
                    modsim.TimeStepManager.dataStartDate = startdate;
                    enddate = dateTimePickerEnd.Value;
                    modsim.TimeStepManager.dataEndDate = enddate;
                }

                PrintMessage($"Processing time-series data from {startdate} to {enddate} ...");

                // get TSTypeIDs
                //string cmdtxt = $"Select TSType From DatasetsTSSet Where Scenario = {treeView1.SelectedNode.Name};";
                //Assume that the units label is the text to set the MODSIM units 
                string cmdtxt = "SELECT DatasetsTSSet.[Order],DatasetsTSSet.TSType, UnitsInfo.Units, TSTypes.MODSIMTSType, TSTypes.IsPattern, TSTypes.TSInterval" +
                         " FROM UnitsInfo INNER JOIN (DatasetsTSSet INNER JOIN TSTypes ON DatasetsTSSet.TSType = TSTypes.TSTypeID) ON UnitsInfo.UnitsID = TSTypes.UnitsID" +
                        $" WHERE(((DatasetsTSSet.[Dataset]) =  {treeViewDatasets.SelectedNode.Name}))" +
                        $"ORDER BY DatasetsTSSet.[Order];";
                DataTable Datasetdt = m_DBUtils.GetTableFromDB(cmdtxt, "Dataset");//ExecuteCommand(cmdtxt);

                // for each TSTypeID
                foreach (DataRow sr in Datasetdt.Rows)
                {
                    cmdtxt = "Select Distinct Timeseries.FeatureID, Features.MOD_Name, Features.MOD_Type, Features.Cost From Timeseries " +
                             $"INNER JOIN  Features on Timeseries.FeatureID = Features.FeatureID where TSTypeID = {sr["TSType"]}";
                    cmdtxt += @" UNION ALL Select Distinct TSPatterns.FeatureID, Features.MOD_Name, Features.MOD_Type, Features.Cost 
                            From TSPatterns 
                            INNER JOIN  Features on TSPatterns.FeatureID = Features.FeatureID 
                            where TSTypeID = " + sr["TSType"];
                    DataTable featuresdt = m_DBUtils.GetTableFromDB(cmdtxt, "FeaturesDT");//ExecuteCommand(cmdtxt);
                    foreach (DataRow fr in featuresdt.Rows)
                    {
                        DataTable tsdt;
                        bool _VariesByYear = false;
                        if (radioButtonLoadTS.Checked)
                        {
                            if (sr["IsPattern"].ToString() == "0")
                            {
                                //cmdtxt = $"Select TSDate AS [Date], ROUND(TSValue * {modsim.ScaleFactor},0) AS HS0 From Timeseries Where FeatureID={fr["FeatureID"]} AND TSTypeID={sr["TSType"]} AND TSDate>='{startdate}' ORDER BY [TSDate];";
                                cmdtxt = "SELECT [Date],max(HS0) AS HS0" +
                                    " FROM(" +
                                   $"     SELECT TSDate AS [Date], ROUND(TSValue * {modsim.ScaleFactor}, 0) AS HS0" +
                                    "     FROM Timeseries" +
                                    $"    WHERE FeatureID={fr["FeatureID"]} AND TSTypeID={sr["TSType"]} AND (TSDate>='{startdate.ToString("yyyy-MM-dd")}' AND TSDate<='{enddate.ToString("yyyy-MM-dd")}')" +
                                    " UNION" +
                                    $"    SELECT '{startdate.ToString("yyyy-MM-dd")}', 0" +
                                    "   )" +
                                    " GROUP BY[Date] ORDER BY[Date]; ";
                                tsdt = m_DBUtils.GetTableFromDB(cmdtxt, "tseries");//ExecuteCommand(cmdtxt);
                                _VariesByYear = true;
                            }
                            else
                            {
                                if (sr["TSInterval"].ToString().ToLower() != "monthly" && sr["TSInterval"].ToString().ToLower() != "daily")
                                {
                                    messageOut($"ERROR: [processing TSPattern] TSInterval not implemented for TSType {sr["TSType"]} \n Aborting processing this TSType");
                                    break;
                                }
                                DateTime _mDate = startdate;
                                tsdt = new DataTable("tseries");
                                tsdt.Columns.Add("Date", typeof(DateTime));
                                tsdt.Columns.Add("HS0", typeof(int));

                                cmdtxt = $"SELECT * FROM TSPatterns WHERE (FeatureID={fr["FeatureID"]} AND TSTypeID = {sr["TSType"]}) ";
                                DataTable patternTbl = m_DBUtils.GetTableFromDB(cmdtxt, "pattern");

                                int maxIndex = sr["TSInterval"].ToString().ToLower() == "monthly" ? 12 : 365;
                                for (int i = 0; i <= maxIndex; i++)
                                {
                                    int _Index = sr["TSInterval"].ToString().ToLower() == "monthly" ? _mDate.Month : _mDate.Day;
                                    double value = double.Parse(patternTbl.Select($"Index = {_Index}")[0]["TSValue"].ToString());
                                    tsdt.Rows.Add(new object[] { _mDate, Math.Round(value * modsim.ScaleFactor, 0) });
                                    _mDate = sr["TSInterval"].ToString().ToLower() == "monthly" ? _mDate.AddMonths(1) : _mDate.AddDays(1);
                                }
                                _VariesByYear = false;
                            }
                        }
                        else
                        {
                            tsdt = new DataTable();
                            tsdt.Columns.Add("Date", typeof(DateTime));
                            tsdt.Columns.Add("HS0", typeof(int));
                            tsdt.Rows.Add(new object[] { startdate.ToString("yyyy-MM-dd"), 0 });
                        }
                        if (tsdt != null)
                        {
                            // apply accuracy factor
                            //tsdt = ApplyAccuracyFactor(tsdt, modsim.accuracy);

                            //check for default units
                            string units = sr["Units"].ToString();
                            if (units == "Dimensionless" || units == "Default") units = "";

                            switch (fr["MOD_Type"].ToString())
                            {
                                case "Link":
                                    // assign time-series to MODSIM Link
                                    Link link = modsim.FindLink(fr["MOD_Name"].ToString());
                                    if (link != null)
                                    {
                                        PrintMessage($"Updating time-series for link {link.name}.");
                                        if (sr["MODSIMTSType"].ToString() == "maxVariable")
                                        {
                                            link.m.maxVariable.dataTable = tsdt;
                                            link.m.maxVariable.VariesByYear = _VariesByYear;
                                            link.m.maxVariable.units = units;
                                        }
                                        else if (sr["MODSIMTSType"].ToString() == "adaMeasured")
                                        {
                                            link.m.adaMeasured.dataTable = tsdt;
                                            link.m.adaMeasured.VariesByYear = _VariesByYear;
                                            link.m.adaMeasured.units = units;
                                        }
                                        else
                                            PrintMessage($"  ERROR: MODSIM time series {sr["MODSIMTSType"] + " - " + sr["Units"].ToString()} type not implemented.");
                                        //new ModsimUnits(VolumeUnitsType.kCM, new ModsimTimeStep(ModsimTimeStepType.Monthly)); 

                                        if (fr["Cost"].ToString() != "")
                                            link.m.cost = long.Parse(fr["Cost"].ToString());// - 150500;
                                    }
                                    else
                                    {
                                        PrintMessage($"link {fr["MOD_Name"]} not found!");
                                    }
                                    break;
                                case "Demand":
                                    // assign time-series to MODSIM Node
                                    Node dem = modsim.FindNode(fr["MOD_Name"].ToString());
                                    if (dem != null) PopulateTS(dem, dem.m.adaDemandsM, tsdt, units, _VariesByYear);
                                    if (fr["Cost"].ToString() != "" && dem.InflowLinks != null)
                                    {
                                        Link l = dem.InflowLinks.link;
                                        l.m.cost = long.Parse(fr["Cost"].ToString());
                                        PrintMessage($"   Setting demnand inflow link {l.name}.");
                                    }
                                    break;
                                case "NonStorage":
                                    // assign time-series to MODSIM Node
                                    Node NSNode = modsim.FindNode(fr["MOD_Name"].ToString());
                                    PopulateTS(NSNode, NSNode.m.adaInflowsM, tsdt, units, _VariesByYear);
                                    break;
                                case "Reservoir":
                                    // assign time-series to MODSIM Node
                                    Node resNode = modsim.FindNode(fr["MOD_Name"].ToString());
                                    PopulateTS(resNode, resNode.m.adaTargetsM, tsdt, units, _VariesByYear);
                                    break;
                            }
                        }
                    }
                }

                //Generate a File with TS added

                PrintMessage($"Writing updated Modsim file...");
                PrintMessage(ProcessModsimFile);

                //if (System.IO.File.Exists(ProcessModsimFile))
                //    if (MessageBox.Show("Output file already exists.  Do you want to overwrite?", "File overwrite", MessageBoxButtons.YesNo)== DialogResult.No)
                //    {
                //        PrintMessage($" --->>> Import ABORTED!");
                //        return;
                //    }
                //    else
                //    {
                //        System.IO.File.Delete(ProcessModsimFile);
                //    }
                XYFileWriter.Write(modsim, ProcessModsimFile);
            }
            catch (Exception ex)
            {
                PrintMessage(ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
                //reset main MODSIM file name.
                ProcessModsimFile = m_FileName;

                checkBoxResetTSFile.Checked = false;
            }
        }

        private void PopulateTS(Node m_Node, TimeSeries m_TimeSeries, DataTable tsdt, string units, bool variesByYear = true)
        {
            if (m_Node != null)
            {
                m_TimeSeries.dataTable = tsdt;
                m_TimeSeries.VariesByYear = variesByYear;
                if (units != "") m_TimeSeries.units = units;
                //new ModsimUnits(VolumeUnitsType.kCM, new ModsimTimeStep(ModsimTimeStepType.Monthly)); 
                PrintMessage($"Updated time series for node {m_Node.name}");
            }
            else
            {
                PrintMessage($"Node {m_Node.name} not found!");
            }
        }

        private DataTable ApplyAccuracyFactor(DataTable dt, int accuracy)
        {
            foreach (DataRow row in dt.Rows)
            {
                row[1] = Math.Round(double.Parse(row[1].ToString()) * Math.Pow(10, (double)accuracy), 0);
            }
            return dt;
        }

        //private DataTable ExecuteCommand(string commandtext)
        //{
        //    DataTable dt = new DataTable();
        //    OleDbConnection conn = null;

        //    try
        //    {
        //        oleConnBuilder.DataSource = ProjectDB;
        //        using (conn = new OleDbConnection(oleConnBuilder.ConnectionString))
        //        {
        //            conn.Open();
        //            OleDbCommand cmd = conn.CreateCommand();
        //            cmd.CommandText =commandtext;

        //            OleDbDataAdapter adapter = new OleDbDataAdapter(cmd);
        //            adapter.Fill(dt);
        //        }
        //    }
        //    catch (OleDbException ex)
        //    {
        //        PrintMessage($"Error: " + ex.Message);
        //    }
        //    finally
        //    {
        //        if (conn != null && conn.State != ConnectionState.Closed)
        //        {
        //            conn.Close();
        //        }
        //    }

        //    return dt;
        //}

        private void PrintMessage(string msg)
        {
            if (messageOut != null)
                messageOut.Invoke(msg);
            //richTextBox1.Text += msg + '\n';
            //this.Refresh();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            LoadFeaturesAndTSTypes();
            groupBoxSelDSet.Text = "Selected Dataset: " + treeViewDatasets.SelectedNode.Text + $"  (ID:{treeViewDatasets.SelectedNode.Name})";
            this.Cursor = Cursors.Default;
        }

        private void LoadFeaturesAndTSTypes()
        {
            string cmdtxt = $"SELECT DatasetsTSSet.[Order], TSTypes.TSTypeID,MOD_Name,MODSIMTSType, f.Type" +
                            " FROM DatasetsTSSet" +
                            " JOIN TSTypes ON DatasetsTSSet.TSType = TSTypes.TSTypeID" +
                            " JOIN( {0} ) as f" +
                            "    ON f.TSTypeID = DatasetsTSSet.TSType" +
                            $" WHERE DatasetsTSSet.Dataset =  {treeViewDatasets.SelectedNode.Name}";
            string TSQuery;
            TSQuery = @"SELECT TSPatterns.FeatureID, TSTypeID, MOD_Name, 'Pattern' as Type
                            FROM TSPatterns
                            JOIN Features ON Features.FeatureID = TSPatterns.FeatureID
                            GROUP BY TSPatterns.FeatureID, TSTypeID ";
            string sql = string.Format(cmdtxt, TSQuery);
            sql += " UNION ALL ";
            TSQuery = @"SELECT Timeseries.FeatureID, TSTypeID, MOD_Name,'Varies by Year' as Type
                            FROM Timeseries
                            JOIN Features ON Features.FeatureID = Timeseries.FeatureID
                            GROUP BY Timeseries.FeatureID, TSTypeID ";
            sql += string.Format(cmdtxt, TSQuery);

            //applying order/layer logic
            string layers = @"SELECT max(d.[Order]) as [Order],d.TSTypeID,d.MOD_Name,d.MODSIMTSType, d.Type
                                FROM
                                ({0})as d
                                GROUP BY d.MOD_Name,d.MODSIMTSType, d.Type
                                ORDER BY [Order];";
            sql = string.Format(layers, sql);     

            DataTable dt = m_DBUtils.GetTableFromDB(sql, "FeatTSType");//ExecuteCommand(cmdtxt);

            

            // add to colllection
            dataGridView1.DataSource = dt;

            cmdtxt = string.Format(@"SELECT DatasetsTSSet.[Order], TSType,TSName FROM DatasetsTSSet
                                    JOIN DatasetsInfo ON DatasetsInfo.ID=DatasetsTSSet.Dataset
                                    JOIN TSTypes ON TSTypes.TSTypeID=DatasetsTSSet.TSType
                                    WHERE DatasetsInfo.ID = '{0}' ORDER BY DatasetsTSSet.[Order]", treeViewDatasets.SelectedNode.Name);
            DataTable dt2 = m_DBUtils.GetTableFromDB(cmdtxt, "FeatTSType");//ExecuteCommand(cmdtxt);

            // add to colllection
            dataGridViewDSetTSTypes.DataSource = dt2;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Comma separated values files (*.csv)|*.csv|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    textBoxWRFile.Text = Uri.UnescapeDataString(dlg.FileName);
                }
            }
        }

        private void buttonProcessWR_Click(object sender, EventArgs e)
        {
            ProcessWaterRights(CostOnly: false);

        }

        private void ProcessWaterRights(bool CostOnly)
        {

            this.Cursor = Cursors.WaitCursor;
            PrintMessage($"Reading MODSIM file {ProcessModsimFile.Replace(".xy", "_Div.xy")} ...");

            myModel = new Model();
            if(CostOnly)
                XYFileReader.Read(myModel, ProcessModsimFile);
            else
                XYFileReader.Read(myModel, ProcessModsimFile.Replace(".xy", "_Div.xy"));

            // read the model start date
            DateTime startdate = myModel.TimeStepManager.dataStartDate;

            PrintMessage("Getting water rights data ...");

            TextUtils.MessageOut -= PrintMessage;
            TextUtils.MessageOut += PrintMessage;
            DataTable wrTbl = TextUtils.CSVToDataTable(textBoxWRFile.Text, ',', ColHeader: true);

            //Set WR extention active
            myModel.ExtWaterRightsActive = true;
            long ripCount = 0;
            try
            {
                if (!CostOnly)
                {
                    //Get the diversion poitns from the network to identify POUs
                    foreach (Node n in myModel.Nodes_NonStorage)
                    {
                        if (n.name.EndsWith("_Diversion"))
                        {
                            //Found a POU diversion point
                            string POUName = n.name.Replace("_Diversion", "");
                            DataRow[] drs = wrTbl.Select($"[POU_ID] = '{POUName}'");
                            double[] x2y2, x1y1;

                            if (drs.Length > 0)
                            {
                                PrintMessage($"  Processing {drs.Length} water rights data for POU {POUName} ...");
                                //relocate the diversion point (half way the diversion link)
                                LinkList ll = n.OutflowLinks;
                                x2y2 = new double[2];

                                //get the first segment link
                                Link l = ll.link;
                                x1y1 = new double[2] { l.from.graphics.nodeLoc.X, l.from.graphics.nodeLoc.Y };
                                x2y2 = new double[2] { l.to.graphics.nodeLoc.X, l.to.graphics.nodeLoc.Y };

                                //Node riverFrom = n.InflowLinks.link.from;
                                //n.graphics.nodeLoc.X = riverFrom.graphics.nodeLoc.X + (n.graphics.nodeLoc.X - riverFrom.graphics.nodeLoc.X) / 2;
                                //n.graphics.nodeLoc.Y = riverFrom.graphics.nodeLoc.Y + (n.graphics.nodeLoc.Y - riverFrom.graphics.nodeLoc.Y) / 2;

                                List<Node> connectedNodes = new List<Node>();

                                int wrCount = 0;

                                foreach (DataRow dr in drs)
                                {
                                    string WRL_Name;
                                    Link m_Link;
                                    //Create new water right connections
                                    Node inNode;
                                    string nodeName = POUName + "_" + dr["Application ID"].ToString();
                                    string defaultWRLnk = null;

                                    //Connect water rights with uses and storage
                                    if (wrCount == 0)
                                    {
                                        //Use the default WR node
                                        inNode = l.to;
                                        inNode.name = nodeName; //change the name of the default node
                                        defaultWRLnk = inNode.InflowLinks.link.name; // existing link will be repurposed for this AppID

                                        List<string> m_lnks = new List<string>();
                                        foreach (string lOutName in inNode.OutflowLinkNames)
                                        {
                                            Link lout = myModel.FindLink(lOutName);
                                            m_lnks.Add(lout.name);
                                            connectedNodes.Add(lout.to);
                                            lout.name = $"{inNode.name}-{lout.to.name}";


                                            //Set Storage
                                            if (lout.to.name.EndsWith("_STO"))
                                            {
                                                Node resNode = myModel.FindNode(POUName + "_RES");
                                                resNode.m.max_volume += (long)Math.Round(double.Parse(dr["StorageAmount_AF"].ToString()) * myModel.ScaleFactor, 0);
                                                //setting target to max storage
                                                SettingResTarget(ref resNode, resNode.m.max_volume, myModel.TimeStepManager.Index2Date(1, TypeIndexes.DataIndex));
                                                resNode.description += $" { dr["Application ID"]}+{dr["StorageAmount_AF"]} :";
                                                //SetMonthlyStorage(ref lout, dr, startdate);
                                            }
                                        }
                                        ////reconnect existing links
                                        //foreach (string lName in m_lnks)
                                        //{
                                        //    l = myModel.FindLink(lName);
                                        //    Utils.DisConnectFromNode(l);
                                        //    Utils.ConnectFromNode(l, inNode);

                                        //    //Set Storage
                                        //    if (l.to.name.Contains("_STO"))
                                        //    {
                                        //        Node resNode = myModel.FindNode(POUName + "_RES");
                                        //        resNode.m.max_volume = (long)Math.Round(double.Parse(dr["StorageAmount_AF"].ToString()) * myModel.ScaleFactor, 0);
                                        //        SetMonthlyStorage(ref l, dr, startdate);
                                        //    }
                                        //}
                                    }
                                    else
                                    {
                                        inNode = myModel.AddNewNode(true);
                                        inNode.nodeType = NodeType.NonStorage;
                                        inNode.name = nodeName;

                                        double[] newCoords = CalculateDivCoords(x2y2[0], x2y2[1], x1y1[0], x1y1[1], gridNo: 0, xStep: 1, yPosition: wrCount);
                                        inNode.graphics.nodeLoc.X = (float)newCoords[0];
                                        inNode.graphics.nodeLoc.Y = (float)newCoords[1];

                                        //connect each water right to the original uses and storage
                                        foreach (Node m_n in connectedNodes)
                                        {
                                            WRL_Name = $"{inNode.name}-{m_n.name}";
                                            m_Link = myModel.FindLink(WRL_Name);
                                            if (m_Link == null)
                                            {
                                                m_Link = myModel.AddNewLink(true);
                                                m_Link.name = WRL_Name;
                                                Utils.ConnectFromNode(m_Link, inNode);
                                                Utils.ConnectToNode(m_Link, m_n);
                                            }

                                            //Set Storage
                                            if (m_n.name.EndsWith("_STO"))
                                            {
                                                Node resNode = myModel.FindNode(POUName + "_RES");
                                                resNode.m.max_volume += (long)Math.Round(double.Parse(dr["StorageAmount_AF"].ToString()) * myModel.ScaleFactor, 0);
                                                SettingResTarget(ref resNode, resNode.m.max_volume, myModel.TimeStepManager.Index2Date(1, TypeIndexes.DataIndex));
                                                resNode.description += $" { dr["Application ID"]}+{dr["StorageAmount_AF"]} :";
                                                //SetMonthlyStorage(ref m_Link, dr, startdate);
                                            }
                                        }
                                    }

                                    //Create the link
                                    WRL_Name = $"WR_{dr["WR_Type"]}_{dr["Application ID"]}";
                                    m_Link = myModel.FindLink(defaultWRLnk != null ? defaultWRLnk : WRL_Name);
                                    if (m_Link == null)
                                    {
                                        m_Link = myModel.AddNewLink(true);
                                        Utils.ConnectFromNode(m_Link, n);
                                        Utils.ConnectToNode(m_Link, inNode);
                                    }
                                    m_Link.name = WRL_Name;
                                    m_Link.m.waterRightsDate = DateTime.Parse(dr["Priority Date"].ToString());
                                    long maxCapacity = (long)Math.Round(double.Parse(dr["Face Value"].ToString()) * myModel.ScaleFactor, 0);

                                    // Ignoring tthe entries with Face Value = 0 since it's a reporting issue (missing)
                                    //
                                    //if (maxCapacity == 0)
                                    //{
                                    //    maxCapacity += 1;
                                    //    m_Link.m.maxVariable.dataTable.Rows.Clear();
                                    //    m_Link.m.maxVariable.dataTable.Rows.Add(new object[] { startdate, 0 });
                                    //}
                                    m_Link.m.lnkallow = maxCapacity; //Face value give per year.
                                    m_Link.description = m_Link.m.waterRightsDate.ToShortDateString();

                                    if (dr["WR_Type"].ToString() == "Riparian")
                                    {
                                        m_Link.m.cost = long.Parse(textBoxRiparianCost.Text) + ripCount;
                                        //setting unique riparian cost.
                                        if (radioButtonUniqueRiparian.Checked)
                                            ripCount++;
                                    }
                                    wrCount += 1;
                                }
                            }
                        }
                    }
                }
                else
                {
                    PrintMessage($"\tAssigning cost to riparian links ...");
                    DataRow[] wrdrs2 = wrTbl.Select($"[WR_Type] = 'Riparian'", "Priority Date");
                    foreach (DataRow dr in wrdrs2)
                    {
                        Link wrL = myModel.FindLink($"WR_{dr["WR_Type"]}_{dr["Application ID"]}");
                        if (wrL != null)
                        {
                            wrL.m.cost = long.Parse(textBoxRiparianCost.Text) + ripCount; ;
                            if (radioButtonUniqueRiparian.Checked)
                                ripCount++;
                        }
                        else
                            PrintMessage($"     ERROR [Setting cost] Water right {dr["Application ID"]} (POU:{dr["POU_ID"]}) not implemented.");
                    }
                }

                //Set cost to WR links
                DataRow[] wrdrs = wrTbl.Select($"[WR_Type] <> 'Riparian'", "Priority Date");
                int upperCost = int.Parse(textBoxCostFrom.Text) > int.Parse(textBoxCostTo.Text) ? int.Parse(textBoxCostFrom.Text) : int.Parse(textBoxCostTo.Text);
                int lowerCost = int.Parse(textBoxCostFrom.Text) > int.Parse(textBoxCostTo.Text) ? int.Parse(textBoxCostTo.Text) : int.Parse(textBoxCostFrom.Text);
                int increment = (int)Math.Round((double)(upperCost - lowerCost) / wrdrs.Length, 0);
                if (increment < 10)
                    throw new Exception("   ERROR: [Assigning costs] Diversion structure internal cost needs at least 10 units of cost between water rights.");
                PrintMessage($"     Assigning cost to {wrdrs.Length} water rights between {upperCost} and {lowerCost} with {increment} increment.");
                int _cost = lowerCost;
                foreach (DataRow dr in wrdrs)
                {
                    Link wrL = myModel.FindLink($"WR_{dr["WR_Type"]}_{dr["Application ID"]}");
                    if (wrL != null)
                    {
                        wrL.m.cost = _cost;
                        _cost += increment;
                    }
                    else
                        PrintMessage($"     ERROR [Setting cost] Water right {dr["Application ID"]} (POU:{dr["POU_ID"]}) not implemented.");
                }

                if (CostOnly)
                    XYFileWriter.Write(myModel, myModel.fname);
                else
                    XYFileWriter.Write(myModel, myModel.fname.Replace(".xy", "_WR.xy"));

                PrintMessage($"Finished. \n Saved file as: {myModel.fname}");
            }
            catch (Exception ex)
            {
                PrintMessage($"ERROR processing water rights. \n{ex.Message} \n {ex.StackTrace}");

            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void SettingResTarget(ref Node resNode, long volume, DateTime startDate)
        {
            if (resNode.m.adaTargetsM.dataTable.Rows.Count == 0)
            {
                resNode.m.adaTargetsM.dataTable.Rows.Add(new object[] { startDate, volume });
            }
            else
            {
                resNode.m.adaTargetsM.dataTable.Rows[0][1] = volume;
            }

        }

        private void SetMonthlyStorage(ref Link m_Link, DataRow dr, DateTime startdate)
        {
            DataTable tsdt = m_Link.m.maxVariable.dataTable.Clone();
            DateTime m_dt = startdate;
            for (int i = 0; i < 12; i++)
            {
                string monColName = $"{m_dt.ToString("MMM")}_Allwd_DivStor";
                tsdt.Rows.Add(new object[] { m_dt, (long)Math.Round(double.Parse(dr[monColName].ToString()) * myModel.ScaleFactor, 0) });
                m_dt = m_dt.AddMonths(1);
            }
            m_Link.m.maxVariable.dataTable = tsdt;
            m_Link.m.maxVariable.VariesByYear = false;
            m_Link.m.maxVariable.units = new ModsimUnits(VolumeUnitsType.AF, ModsimTimeStep.FromLabel("Monthly"));
        }

        private void buttonImportGSData_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            string tempOutFile = _outFileName;
            try
            {
                if (radioButtonOtherSegFile.Checked)
                    _outFileName = textBoxSegFlowFile.Text;

                if (comboBoxTSTypes.Text == "<< New >>" && txtNewTSName.Text == "")
                {
                    MessageBox.Show("Please define a TS Name for the new time series to be imported.");
                    return;
                }

                MyDBSqlite m_DBUtils2 = new MyDBSqlite(_syncDBFileName);
                m_DBUtils.messageOut += PrintMessage;
                DataTable dt_Segs = m_DBUtils2.GetTableFromDB("SELECT * FROM [MS-GSF_mapping_Info]", "MS-GSF_mapping_Info");
                if (dt_Segs == null)
                {
                    messageOut($"ERROR: [reading sync table.] Unable to read the sync table.");
                    return;
                }

                //units assumed to be ft3/day
                // TODO: we could dynamically read units from MODFLOW.
                string TSTypeID = "NULL";
                string TSNametxt = txtNewTSName.Text;
                if (comboBoxTSTypes.Text != "<< New >>")
                {
                    //DataTable dtbl = (DataTable)comboBoxTSTypes.DataSource;
                    int TS = int.Parse(((System.Data.DataRowView)comboBoxTSTypes.SelectedItem).Row.ItemArray[0].ToString());//(int)dtbl.Select($"TSName = '{comboBoxTSTypes.Text}'")[0]["TSTYpeID"];
                    TSTypeID = TS.ToString();
                    TSNametxt = comboBoxTSTypes.Text;
                }
                string sql = $"INSERT OR REPLACE INTO TSTypes VALUES ({TSTypeID},'{TSNametxt}',7,'GSFLOW','{_outFileName}',1,'Daily','Imported time series - Accretion/Depletion','maxVariable',0,'{DateTime.Now.ToString("yyyy-MM-dd")}')";
                int newTS = m_DBUtils.ExecuteQuery(sql);

                //Clear time series
                if (checkBoxDelTSTypeTS.Checked)
                {
                    sql = $"DELETE FROM Timeseries WHERE TSTypeID = {newTS}";
                    PrintMessage($"Deleting time series for TSTypeID = {newTS}");
                    m_DBUtils.ExecuteQuery(sql);
                }


                DataTable dt = TextUtils.CSVToDataTable(_outFileName, ' ');
                if (dt == null)
                {
                    PrintMessage("ERROR [Loading output] Problem reading the GSFLOW output table. Check that the file exists and it not locked.");
                    return;
                }
                string tsQuery = $"SELECT * FROM Timeseries WHERE TSTypeID = {newTS}";
                DataTable TSTbl = m_DBUtils.GetTableFromDB(tsQuery, "Timeseries");

                foreach (DataRow drSeg in dt_Segs.Rows)
                {
                    DateTime _Date = dtMODFLOWstart;
                    int featureID_Dep = GetFeatureID(MODSIMName: drSeg["Link Name"].ToString(), depletion: true);
                    int featureID_Acc = GetFeatureID(MODSIMName: drSeg["Link Name"].ToString(), depletion: false);
                    int colNo = int.Parse(drSeg["iseg"].ToString());
                    if (colNo < dt.Columns.Count)
                    {
                        foreach (DataRow drData in dt.Rows)
                        {
                            double val = double.Parse(drData[colNo].ToString());
                            if (val > 0)
                            {
                                TSTbl.Rows.Add(new object[] { _Date.ToString("yyyy-MM-dd"), featureID_Acc, newTS, Math.Abs(val) });
                                TSTbl.Rows.Add(new object[] { _Date.ToString("yyyy-MM-dd"), featureID_Dep, newTS, 0 });
                            }
                            else
                            {
                                TSTbl.Rows.Add(new object[] { _Date.ToString("yyyy-MM-dd"), featureID_Dep, newTS, Math.Abs(val) });
                                TSTbl.Rows.Add(new object[] { _Date.ToString("yyyy-MM-dd"), featureID_Acc, newTS, 0 });
                            }
                            _Date = _Date.AddDays(1);
                        }
                    }
                    else
                    {
                        if (drSeg["Diversion"].ToString() == "0")
                        {
                            messageOut($"    WARNING: Missing Accretion/Depletion for segment {drSeg["Link Name"]} - file Column {colNo}");
                        }
                    }
                }
                messageOut("Updating the database time series...");
                m_DBUtils.UpdateTableFromDB(TSTbl);
                Load_DBInfo();
                messageOut("Done.");
            }
            catch (Exception ex)
            {
                PrintMessage(ex.Message);
            }
            finally
            {
                if (radioButtonOtherSegFile.Checked)
                    _outFileName = tempOutFile;
                this.Cursor = Cursors.Default;
            }
        }

        private int GetFeatureID(string MODSIMName, bool depletion)
        {
            string lnkName = $"MF_Acc_{MODSIMName}";
            if (depletion)
                lnkName = $"MF_Dep_{MODSIMName}";
            return GetFeatureID(lnkName, "Link");
        }

        private int GetFeatureID(string MODSIMName, string MOD_Type)
        {
            DataRow[] drs = featuresTbl.Select($"[MOD_Name] = '{MODSIMName}' AND [MOD_Type] = '{MOD_Type}'");
            if (drs.Length > 0)
            {
                return int.Parse(drs[0]["FeatureID"].ToString());
            }
            return -1;
        }

        private void buttonUpdateFeatures_Click(object sender, EventArgs e)
        {
            string m_FileName = ProcessModsimFile;
            if (comboBoxMODSIMFile2.Text.Contains("_DIV.xy"))
                m_FileName = m_FileName.Replace(".xy", "_DIV.xy");
            if (comboBoxMODSIMFile2.Text.Contains("_DIV_WR.xy"))
                m_FileName = m_FileName.Replace(".xy", "_DIV_WR.xy");

            if (File.Exists(m_FileName))
            {

                PrintMessage($"Reading MODSIM file {m_FileName} ...");

                myModel = new Model();
                XYFileReader.Read(myModel, m_FileName);

                foreach (Link l in myModel.Links_All)
                {
                    if (GetFeatureID(l.name, "Link") < 0)
                    {
                        string sql = $"INSERT INTO Features VALUES (NULL,'{l.name}','Link',{l.number},'','','{l.uid}',NULL,NULL,{l.from.number},{l.to.number},{l.m.cost})";
                        int newTS = m_DBUtils.ExecuteQuery(sql);
                        messageOut($"    Added: Link {l.name} ID:{newTS}");
                    }
                }
                foreach (Node n in myModel.Nodes_All)
                {
                    if (GetFeatureID(n.name, n.nodeType.ToString()) < 0)
                    {
                        string sql = $"INSERT INTO Features VALUES (NULL,'{n.name}','{n.nodeType.ToString()}',{n.number},'','','{n.uid}',NULL,NULL,NULL,NULL,NULL)";
                        int newTS = m_DBUtils.ExecuteQuery(sql);
                        messageOut($"    Added: Node {n.name} ID:{newTS}");
                    }
                }
                if (checkBoxIncludeAccDep.Checked)
                {
                    MyDBSqlite m_DBUtils2 = new MyDBSqlite(_syncDBFileName);
                    m_DBUtils.messageOut += PrintMessage;
                    DataTable dt_Segs = m_DBUtils2.GetTableFromDB("SELECT * FROM [MS-GSF_mapping_Info]", "MS-GSF_mapping_Info");

                    foreach (DataRow drSeg in dt_Segs.Rows)
                    {
                        DateTime _Date = dtMODFLOWstart;

                        if (GetFeatureID(MODSIMName: drSeg["Link Name"].ToString(), depletion: true) < 0)
                        {

                            string sql = $"INSERT INTO Features VALUES (NULL,'MF_Dep_{drSeg["Link Name"]}','Link',NULL,'','',NULL,NULL,NULL,NULL,NULL,NULL)";
                            int newTS = m_DBUtils.ExecuteQuery(sql);
                            messageOut($"    Added: Link MF_Dep_{drSeg["Link Name"]} ID:{newTS}");
                        }
                        if (GetFeatureID(MODSIMName: drSeg["Link Name"].ToString(), depletion: false) < 0)
                        {

                            string sql = $"INSERT INTO Features VALUES (NULL,'MF_Acc_{drSeg["Link Name"]}','Link',NULL,'','',NULL,NULL,NULL,NULL,NULL,NULL)";
                            int newTS = m_DBUtils.ExecuteQuery(sql);
                            messageOut($"    Added: Link MF_Acc_{drSeg["Link Name"]} ID:{newTS}");
                        }
                    }
                }
                Load_DBInfo();
                PrintMessage($"Finished adding MODSIM elements to the Features table.");
            }
            else
                MessageBox.Show($"File {m_FileName} not found. Please create before continuing.");

        }

        private void buttonAddAccDep_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                messageOut($"Processing accretion/depletion links and nodes...");
                string m_FileName = ProcessModsimFile;
                if (comboBoxMODSIMFile.Text.Contains("_DIV.xy"))
                    m_FileName = m_FileName.Replace(".xy", "_DIV.xy");
                if (comboBoxMODSIMFile.Text.Contains("_DIV_WR.xy"))
                    m_FileName = m_FileName.Replace(".xy", "_DIV_WR.xy");
                if (comboBoxMODSIMFile.Text.Contains("_DIV_WRTS.xy"))
                    m_FileName = m_FileName.Replace(".xy", "_DIV_WRTS.xy");

                if (File.Exists(m_FileName))
                {
                    messageOut($"     Reading MODSIM file {m_FileName} ...");

                    myModel = new Model();
                    XYFileReader.Read(myModel, m_FileName);

                    // read the model start date
                    DateTime startdate = myModel.TimeStepManager.dataStartDate;

                    SWGW_MODSIMUtils swgwUtils = new SWGW_MODSIMUtils(ref myModel);
                    messageOut($"     Creating accretion/depletion construct ...");
                    double vol_tol, lake_Tol;
                    swgwUtils.PrepareMODSIMNetwork(_syncDBFileName, out vol_tol, out lake_Tol);

                    messageOut($"     Saving MODSIM file ...");
                    if (checkBoxSaveMSGSF.Checked)
                    {
                        m_FileName = m_FileName.Replace(".xy", "MSGSF.xy");
                        messageOut($"\t File: {m_FileName}");
                    }
                    XYFileWriter.Write(myModel, m_FileName);

                    messageOut($"Done.");
                }
                else
                    MessageBox.Show($"File {m_FileName} not found. Please create before continuing.");
            }
            catch (Exception ex)
            {
                PrintMessage(ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void textBox6_TextChanged(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            try
            {
                messageOut($"Removing accretion/depletion links and nodes...");
                string m_FileName = ProcessModsimFile;
                if (comboBoxMODSIMFile.Text.Contains("_DIV.xy"))
                    m_FileName = m_FileName.Replace(".xy", "_DIV.xy");
                if (comboBoxMODSIMFile.Text.Contains("_DIV_WR.xy"))
                    m_FileName = m_FileName.Replace(".xy", "_DIV_WR.xy");
                if (comboBoxMODSIMFile.Text.Contains("_DIV_WRTS.xy"))
                    m_FileName = m_FileName.Replace(".xy", "_DIV_WRTS.xy");

                if (File.Exists(m_FileName))
                {
                    messageOut($"     Reading MODSIM file {m_FileName} ...");

                    myModel = new Model();
                    XYFileReader.Read(myModel, m_FileName);

                    messageOut($"     Removing accretion/depletion nodes and links ...");
                    Node accDepNode = myModel.FindNode("MF_SOURCE");
                    if (accDepNode != null)
                        myModel.Remove(accDepNode, isRealNode: true);

                    accDepNode = myModel.FindNode("MF_SINK");
                    if (accDepNode != null)
                        myModel.Remove(accDepNode, isRealNode: true);

                    messageOut($"     Saving MODSIM file ...");
                    XYFileWriter.Write(myModel, m_FileName);

                    messageOut($"Done.");
                }
                else
                    MessageBox.Show($"File {m_FileName} not found. Please create the file before continuing.");
            }
            catch (Exception ex)
            {
                PrintMessage(ex.Message);
            }
            finally
            {
                this.Cursor = Cursors.Default;
            }
        }

        private void comboBoxTSTypes_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool mview = comboBoxTSTypes.Text == "<< New >>";
            labelTSTypeNew.Visible = mview;
            txtNewTSName.Visible = mview;

        }

        private void buttonDelTSType_Click(object sender, EventArgs e)
        {
            int TSTypeID = int.Parse(dataGridViewTSType.SelectedRows[0].Cells["TSTypeID"].Value.ToString());
            string TSName = dataGridViewTSType.SelectedRows[0].Cells["TSName"].Value.ToString();
            if (MessageBox.Show($"Do you want to delete all data for TSType: {TSName}?", "Delete Data", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                string sql = $"DELETE FROM Timeseries WHERE TSTypeID = {TSTypeID}";
                int delted = m_DBUtils.ExecuteQuery(sql);
                messageOut($"   Deleted {delted} entries from the Timeseries table");
                sql = $"DELETE FROM TSPatterns WHERE TSTypeID = {TSTypeID}";
                delted = m_DBUtils.ExecuteQuery(sql);
                messageOut($"   Deleted {delted} entries from the TSPatterns table");
                sql = $"DELETE FROM TSTypes WHERE TSTypeID = {TSTypeID}";
                m_DBUtils.ExecuteQuery(sql);
                messageOut("Complete deleting TSTypeID.");
                Load_DBInfo();
            }
        }

        private void buttonRefresh_Click(object sender, EventArgs e)
        {
            Load_DBInfo();
        }

        private void buttonAddTSType_Click(object sender, EventArgs e)
        {
            int TS = int.Parse(((System.Data.DataRowView)comboBoxDSetTSTypes.SelectedItem).Row.ItemArray[0].ToString());
            int newOrder = dataGridViewDSetTSTypes.Rows.Count + 1;
            string cmdtxt = $@"INSERT INTO DatasetsTSSet
                               VALUES ({treeViewDatasets.SelectedNode.Name},{newOrder},{TS},'User added {DateTime.Now.ToShortDateString()}')";
                                    //(SELECT Max([Order])+1 AS NewOrder FROM DatasetsTSSet WHERE Dataset={treeViewDatasets.SelectedNode.Name})
            int added = m_DBUtils.ExecuteQuery(cmdtxt);
            if (added > 0)
                messageOut($"Added TSType {TS} to Dataset {treeViewDatasets.SelectedNode.Name}");
            treeView1_AfterSelect(null, null);

        }

        private void checkBoxUpdateDates_CheckedChanged(object sender, EventArgs e)
        {
            groupBoxDataDates.Visible = checkBoxUpdateDates.Checked;
        }

        private void buttonDelDSetTSType_Click(object sender, EventArgs e)
        {
            int TSTypeID = int.Parse(dataGridViewDSetTSTypes.SelectedRows[0].Cells["TSType"].Value.ToString());
            if (MessageBox.Show($"Do you want to remove TSType: {TSTypeID} from Dataset {treeViewDatasets.SelectedNode.Text}?", "Delete Data", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                string sql = $"DELETE FROM DatasetsTSSet WHERE TSType = {TSTypeID} AND Dataset = {treeViewDatasets.SelectedNode.Name}";
                m_DBUtils.ExecuteQuery(sql);
                messageOut($"   Deleted entry from the DatasetsTSSet table");
                treeView1_AfterSelect(null, null);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Output File (*.out)|*.out|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    textBoxSegFlowFile.Text = dlg.FileName;
                }
            }
        }

        private void radioButton4_CheckedChanged(object sender, EventArgs e)
        {
            textBoxSegFlowFile.Visible = radioButtonOtherSegFile.Checked;
            buttonBrowseSegFile.Visible = radioButtonOtherSegFile.Checked;
        }

        private void radioButton3_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void comboBoxTSTypes2_SelectedIndexChanged(object sender, EventArgs e)
        {
            bool mview = comboBoxTSTypes2.Text == "<< New >>";
            labelTSTypeNew2.Visible = mview;
            txtNewTSName2.Visible = mview;
        }

        private void buttonCreateTS_Click(object sender, EventArgs e)
        {
            this.Cursor = Cursors.WaitCursor;
            //string tempOutFile = _outFileName;
            try
            {
                if (comboBoxTSTypes2.Text == "<< New >>" && txtNewTSName2.Text == "")
                {
                    MessageBox.Show("Please define a TS Name for the new time series to be imported.");
                    return;
                }

                //MyDBSqlite m_DBUtils2 = new MyDBSqlite(_syncDBFileName);
                //m_DBUtils.messageOut += PrintMessage;
                //DataTable dt_Segs = m_DBUtils2.GetTableFromDB("SELECT * FROM [MS-GSF_mapping_Info]", "MS-GSF_mapping_Info");
                //if (dt_Segs == null)
                //{
                //    messageOut($"ERROR: [reading sync table.] Unable to read the sync table.");
                //    return;
                //}

                DataRow drBase = ((System.Data.DataRowView)comboBoxTSTypes3.SelectedItem).Row;
                int baseTS = int.Parse(drBase["TSTypeID"].ToString());
                bool isBasePattern = int.Parse(drBase["IsPattern"].ToString()) == 1;

                string TSTypeID = "NULL";
                string TSNametxt = txtNewTSName2.Text;
                if (comboBoxTSTypes2.Text != "<< New >>")
                {
                    DataRow[] drs = ((DataTable)dataGridViewTSType.DataSource).Select($"TSName = '{comboBoxTSTypes2.Text}'");
                    TSTypeID = drs[0]["TSTYpeID"].ToString();
                    TSNametxt = comboBoxTSTypes2.Text;
                }
                string sql = $"INSERT OR REPLACE INTO TSTypes VALUES ({TSTypeID},'{TSNametxt}',{drBase["UnitsID"]},'User calculated time series.'" +
                    $",'TSTYPE = {drBase["TSTypeID"]}',1,'{drBase["TSInterval"]}','{richTextBoxNewTSNotes.Text}','{drBase["MODSIMTSType"]}'" +
                    $",{drBase["IsPattern"]},'{DateTime.Now.ToString("yyyy-MM-dd")}')";
                int newTS = m_DBUtils.ExecuteQuery(sql);

                //Clear time series
                if (checkBoxDelTSTypeTS.Checked)
                {
                    if (isBasePattern)
                    {
                        sql = $"DELETE FROM Timeseries WHERE TSTypeID = {newTS}";
                        PrintMessage($"Deleting time series for TSTypeID = {newTS}");
                        m_DBUtils.ExecuteQuery(sql);
                    }
                    else
                    {
                        sql = $"DELETE FROM Timeseries WHERE TSTypeID = {newTS}";
                        PrintMessage($"Deleting time series for TSTypeID = {newTS}");
                        m_DBUtils.ExecuteQuery(sql);
                    }

                }

                //Create factors table in the database
                CreateFactorsInDB();

                string tsQuery;
                if (isBasePattern)
                {
                    tsQuery = $@"INSERT INTO TSPatterns
                                SELECT {newTS} AS TSTypeID, TSPatterns.FeatureID, [Index] , TSValue * _Factors.Factor
                                FROM TSPatterns
                                JOIN Features ON Features.FeatureID = TSPatterns.FeatureID
                                JOIN _Factors ON _Factors.MonthIndex = TSPatterns.[Index]
                                WHERE TSPatterns.TSTypeID = {baseTS} {filterString()}
                            ";
                }
                else
                {
                    tsQuery = $@"INSERT INTO Timeseries 
                                SELECT TSDate, Timeseries.FeatureID, {newTS} AS TSTypeID, TSValue * _Factors.Factor
                                FROM Timeseries
                                JOIN Features ON Features.FeatureID = Timeseries.FeatureID
                                JOIN _Factors ON _Factors.MonthIndex = strftime('%m', TSDate)
                                WHERE Timeseries.TSTypeID = {baseTS} {filterString()}
                            ";
                }


                messageOut("Updating the database time series...");
                m_DBUtils.ExecuteQuery(tsQuery);
                Load_DBInfo();
                messageOut("Done.");
            }
            catch (Exception ex)
            {
                PrintMessage(ex.Message);
            }
            finally
            {
                m_DBUtils.ExecuteQuery("DROP TABLE IF EXISTS [_Factors]; ");
                this.Cursor = Cursors.Default;
            }
        }

        private void CreateFactorsInDB()
        {
            DataTable dt = new DataTable("_Factors");
            foreach (DataGridViewColumn col in dataGridViewMonthlyFactors.Columns)
            {
                dt.Columns.Add(col.Name);
            }

            foreach (DataGridViewRow row in dataGridViewMonthlyFactors.Rows)
            {
                DataRow dRow = dt.NewRow();
                foreach (DataGridViewCell cell in row.Cells)
                {
                    if (cell.Value == null || cell.Value == "")
                        throw new Exception($"\t ERROR: Factor for month {dRow[0]} not specified.");
                    dRow[cell.ColumnIndex] = cell.Value;
                }
                dt.Rows.Add(dRow);
            }
            if (m_DBUtils.IsTableExist("_Factors"))
                m_DBUtils.ExecuteQuery("DROP TABLE IF EXISTS [_Factors]; ");
            m_DBUtils.ExecuteNonQuery(@"CREATE TABLE [_Factors] (
                                        [MonthIndex]   INTEGER,
                                        [Factor]    REAL,
                                        PRIMARY KEY([MonthIndex])
                                        ); ");
            m_DBUtils.UpdateTableFromDB(dt);
        }

        private void LoadFeaturesFiltered()
        {
            try
            {
                DataRow drBase = ((System.Data.DataRowView)comboBoxTSTypes3.SelectedItem).Row;
                bool isBasePattern = int.Parse(drBase["IsPattern"].ToString()) == 1;

                string tsQuery;
                if (isBasePattern)
                {
                    tsQuery = $@"SELECT TSPatterns.FeatureID, TSTypeID, MOD_Name, 'Pattern' as Type
                            FROM TSPatterns
                            JOIN Features ON Features.FeatureID = TSPatterns.FeatureID
                            WHERE TSPatterns.TSTypeID = {drBase["TSTypeID"]} {filterString()}
                            GROUP BY TSPatterns.FeatureID ";
                }
                else
                {
                    tsQuery = $@"SELECT Timeseries.FeatureID, TSTypeID, MOD_Name,'Varies by Year' as Type
                            FROM Timeseries
                            JOIN Features ON Features.FeatureID = Timeseries.FeatureID
                            WHERE Timeseries.TSTypeID = {drBase["TSTypeID"]} {filterString()}
                            GROUP BY Timeseries.FeatureID ";
                }

                DataTable dt = m_DBUtils.GetTableFromDB(tsQuery, "FeatTSType");//ExecuteCommand(cmdtxt);

                // add to colllection
                dataGridViewFilteredFeats.DataSource = dt;
            }
            catch (Exception)
            {

            }
        }

        private string filterString()
        {
            string filter = "";
            if (radioButtonStartWith.Checked)
            {
                return $" AND Features.MOD_Name LIKE '{comboBoxFilterName.Text}%'";
            }
            if (radioButtonEndsWith.Checked)
            {
                return $" AND Features.MOD_Name LIKE '%{comboBoxFilterName.Text}'";
            }
            if (radioButtonContains.Checked)
            {
                return $" AND Features.MOD_Name LIKE '%{comboBoxFilterName.Text}%'";
            }
            return filter;
        }

        private void radioButton5_CheckedChanged(object sender, EventArgs e)
        {
            LoadFeaturesFiltered();
        }

        private void radioButtonStartWith_CheckedChanged(object sender, EventArgs e)
        {
            LoadFeaturesFiltered();
        }

        private void radioButtonEndsWith_CheckedChanged(object sender, EventArgs e)
        {
            LoadFeaturesFiltered();
        }

        private void radioButtonContains_CheckedChanged(object sender, EventArgs e)
        {
            LoadFeaturesFiltered();
        }

        private void comboBoxFilterName_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadFeaturesFiltered();
        }

        private void comboBoxTSTypes3_SelectedIndexChanged(object sender, EventArgs e)
        {
            LoadFeaturesFiltered();
        }

        private void comboBoxFilterName_TextUpdate(object sender, EventArgs e)
        {
            LoadFeaturesFiltered();
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void buttonMoveTSUp_Click(object sender, EventArgs e)
        {
            if (dataGridViewDSetTSTypes.SelectedRows.Count > 0)
            {
                int datasetSel = int.Parse(treeViewDatasets.SelectedNode.Name);
                int orderSel = int.Parse(dataGridViewDSetTSTypes.SelectedRows[0].Cells["Order"].Value.ToString());
                int tsTypeSel = int.Parse(dataGridViewDSetTSTypes.SelectedRows[0].Cells["TSType"].Value.ToString());

                if (orderSel > 1)
                {
                    this.Cursor = Cursors.WaitCursor;
                    m_DBUtils.ExecuteNonQuery($"UPDATE DatasetsTSSet SET [Order] = [Order] + 1 " +
                        $"WHERE [Dataset] = {datasetSel} AND [Order] = {orderSel}-1");
                    m_DBUtils.ExecuteNonQuery($"UPDATE DatasetsTSSet SET [Order] = {orderSel - 1} " +
                        $"WHERE [Dataset] = {datasetSel} AND [TSType]={tsTypeSel}");
                    LoadFeaturesAndTSTypes();
                    this.Cursor = Cursors.Default;
                }
            }
            else
                messageOut("ERROR: Please select a TSType row to move up.");
        }

        private void buttonNewDSet_Click(object sender, EventArgs e)
        {
            if(textBoxNewDSName.Text=="")
            {
                messageOut("ERROR: Please type a name for the new Dataset.");
                return;
            }
            string TSTypeID = "NULL";
            string TSNametxt = textBoxNewDSName.Text;
            string sql = $"INSERT OR REPLACE INTO DatasetsInfo VALUES ({TSTypeID},'{TSNametxt}','User added Dataset.')";
            int newDS = m_DBUtils.ExecuteQuery(sql);
            LoadScenarios();
            messageOut($"\tDataset {TSNametxt} created in the database.");
            textBoxNewDSName.Text = "";
        }

        private void buttonMoveTSDown_Click(object sender, EventArgs e)
        {
            if (dataGridViewDSetTSTypes.SelectedRows.Count > 0)
            {
                int datasetSel = int.Parse(treeViewDatasets.SelectedNode.Name);
                int orderSel = int.Parse(dataGridViewDSetTSTypes.SelectedRows[0].Cells["Order"].Value.ToString());
                int tsTypeSel = int.Parse(dataGridViewDSetTSTypes.SelectedRows[0].Cells["TSType"].Value.ToString());

                if (orderSel < dataGridViewDSetTSTypes.Rows.Count)
                {
                    this.Cursor = Cursors.WaitCursor;
                    m_DBUtils.ExecuteNonQuery($"UPDATE DatasetsTSSet SET [Order] = [Order] - 1 " +
                        $"WHERE [Dataset] = {datasetSel} AND [Order] = {orderSel}+1");
                    m_DBUtils.ExecuteNonQuery($"UPDATE DatasetsTSSet SET [Order] = {orderSel + 1} " +
                        $"WHERE [Dataset] = {datasetSel} AND [TSType]={tsTypeSel}");
                    LoadFeaturesAndTSTypes();
                    this.Cursor = Cursors.Default;
                }
            }
            else
                messageOut("ERROR: Please select a TSType row to move up.");
        }

        private void buttonDelTSet_Click(object sender, EventArgs e)
        {
            if(MessageBox.Show($"Are you sure you want to delete dataset {treeViewDatasets.SelectedNode.Text}?", "Delete Dataset", MessageBoxButtons.YesNo)==DialogResult.Yes)
            {
                string sql = $"DELETE FROM DatasetsTSSet WHERE Dataset = {treeViewDatasets.SelectedNode.Name}";
                m_DBUtils.ExecuteQuery(sql);
                messageOut($"   Deleted entries from the DatasetsTSSet table for Dataset {treeViewDatasets.SelectedNode.Text}.");
                sql = $"DELETE FROM DatasetsInfo WHERE ID = {treeViewDatasets.SelectedNode.Name}";
                m_DBUtils.ExecuteQuery(sql);
                messageOut($"   Deleted entry from the DatasetsInfo table.");

                LoadScenarios();
                treeViewDatasets.SelectedNode = treeViewDatasets.Nodes[0];
            }
        }

        private void radioButton4_CheckedChanged_1(object sender, EventArgs e)
        {
            numericUpDownAnnFactor.Visible = radioButton4.Checked;
            dataGridViewMonthlyFactors.ReadOnly = radioButton4.Checked;
            numericUpDownAnnFactor_ValueChanged(null, null);
        }

        private void numericUpDownAnnFactor_ValueChanged(object sender, EventArgs e)
        {
            foreach(DataGridViewRow dgvr in dataGridViewMonthlyFactors.Rows)
            {
                dgvr.Cells[1].Value = numericUpDownAnnFactor.Value;
            }
        }

        private void numericUpDownAnnFactor_KeyDown(object sender, KeyEventArgs e)
        {
            
        }

        private void numericUpDownAnnFactor_KeyUp(object sender, KeyEventArgs e)
        {
            foreach (DataGridViewRow dgvr in dataGridViewMonthlyFactors.Rows)
            {
                dgvr.Cells[1].Value = numericUpDownAnnFactor.Value;
            }
        }

        private void buttonWRCostOnly_Click(object sender, EventArgs e)
        {
            messageOut($"Processing accretion/depletion links and nodes...");
            string m_FileName = ProcessModsimFile;
            if (comboBoxMODSIMFile.Text.Contains("_DIV_WR.xy"))
                ProcessModsimFile = m_FileName.Replace(".xy", "_DIV_WR.xy");
            if (comboBoxMODSIMFile.Text.Contains("_DIV_WRTS.xy"))
                ProcessModsimFile = m_FileName.Replace(".xy", "_DIV_WRTS.xy");

            ProcessWaterRights(CostOnly: true);
            ProcessModsimFile = m_FileName;
        }
    }
}
