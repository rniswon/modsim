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
using System.Diagnostics;
using System.IO;
using RTI.CWR.MWC_MODSIMUtils;
using RRModelingSystem.Properties;
using MODSIM_GSFLOW_C;

namespace RRModelingSystem
{

    public partial class Simulation : UserControl
    {
        private string _OpsDB { get; set; }
        private string _ModsimFile { get; set; }
        private string _controlFile { get; set; }

        private RiparianAllocation allocationTool;
        private int _riparianCost;
        private Dictionary<string,long> costRange;

        public event ProcessMessage messageOut; // event

        private Model m_ActiveModel;
        public bool modelReady { get; set; }
        private DataTable ISFTargetsTbl { get; set; }
        private Dictionary<string,long> nodeSetCost { get; set; }
        private static MyDBSqlite sqliteDB { get; set; }


        public Simulation(string ModsimFile, string opsDB, int riparianCost, string MMS_db, string controlFile)
        {
            InitializeComponent();

            _ModsimFile = ModsimFile;
            _OpsDB = opsDB;
            _riparianCost = riparianCost;
            _controlFile = controlFile;
            modelReady = false;
            sqliteDB = new MyDBSqlite(MMS_db);
            sqliteDB.messageOut += ProcessMessageOut;

        }

        private void ProcessMessageOut(string msg)
        {
            messageOut(msg);
        }

        private void Simulation_Load(object sender, EventArgs e)
        {
            treeViewPsdoCost.Nodes.Add("Riparian WRs", "Riparian WRs");
            treeViewPsdoCost.Nodes.Add("Normal Appropriative WR Block", "Normal Appropriative WR Block");
            treeViewPsdoCost.Nodes.Add("Instream Flow Target", "Instream Flow Target");

            //comboBoxMODSIMFile.SelectedIndex = 0;
            labelRiparian.Text =  _riparianCost.ToString(); //"Riparian rights cost: "

            //Keywords
            string sql= "SELECT keyword FROM MMS_RunsInfo GROUP BY Keyword";
            DataTable dtKeys = sqliteDB.GetTableFromDB(sql, "keywords");
            comboBoxKeyword.DataSource = dtKeys;
            comboBoxKeyword.DisplayMember = "keyword";

            if (!File.Exists(_OpsDB))
            {
                radioButtonMODSIMOnly.Checked = true;
                radioButtonMS_GS.Enabled = false;
                messageOut("WARNING: MODSIM-GSFLOW Mode disabled.");
            }
        }



        private void buttonImportTS_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;

            int runid = -1;
            if (radioButtonMMSRun.Checked)
            {
                string m_DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:MM:ss");
                string sql = "INSERT INTO MMS_RunsInfo (ScnName, SimulationStatus, Keyword, LastAccess, Notes, Options) VALUES ('{0}',{1},'{2}','{3}','{4}','{5}')";
                string varTxt = BuildOptionsTxt();
                sql = string.Format(sql, textBoxScnName.Text, 0, comboBoxKeyword.Text, m_DateTime, richTextBoxRunNotes.Text, varTxt);
                runid = sqliteDB.ExecuteQuery(sql);
                messageOut($"Logged run {runid} to the MMS database under keyword {comboBoxKeyword.Text}.\n");
            }

            int run = -1;
            string runFile="";
            try
            {
                if (!modelReady || m_ActiveModel == null)
                    throw new Exception("ERROR: Active model is not loaded in memory.  Try again later or select a different active model.");

                //Processing Management Options
                messageOut("\tAdjusting costs in the MODSIM network...");
                AdjustNetworkCost();

                //Setting ISF Targets
                messageOut("\tSetting ISF targets...");
                AdjustISFTargets();

                messageOut("\tSaving changes to active network...");
                //find output location and file name
                runFile = GetActiveMODSIMFile(comboBoxMODSIMFile.Text, _ModsimFile);
                if (runid != -1)
                {
                    if (checkBoxUseInName.Checked)
                        runFile = runFile.Replace(".xy", $"_{textBoxScnName.Text}.xy");
                    runFile = runFile.Replace(".xy", $"_r{runid}.xy");
                }

                if (runid!=-1 && comboBoxKeyword.Text != "")
                {
                    string folder =Path.Combine(Path.GetDirectoryName(runFile),comboBoxKeyword.Text);
                    if (!Directory.Exists(folder))
                        Directory.CreateDirectory(folder);
                    runFile = Path.Combine(folder, Path.GetFileName(runFile));
                }
                XYFileWriter.Write(m_ActiveModel, runFile);

                //Adding 'plug-ins'
                if (checkBoxRiparianLogic.Checked)
                {
                    messageOut("\tActivating riparian logic allocation...");
                    allocationTool = new RiparianAllocation(ref m_ActiveModel, _riparianCost);
                    allocationTool.messageOut += OnMessageOut;
                }

                //Adding 'plug-ins'
                if (radioButtonMS_GS.Checked)
                {
                    messageOut("\tActivating MODSIM-GSFLOW simulation mode...");
                    string[] CmdArgs = new string[] { "\"" + _controlFile + "\"" };
                    SurfGWModule sSurfGWModule = new SurfGWModule(CmdArgs);
                    sSurfGWModule.messageOut += OnMessageOut;

                    //XYFileReader.Read(myModel, sSurfGWModule.xyFileName);
                    m_ActiveModel.OnMessage += OnMessageOut;
                    m_ActiveModel.OnModsimError += OnMessageOut;

                    sSurfGWModule.InitializeRUN(ref m_ActiveModel);

                }

                messageOut("\tExecuting MODSIM model...");
                run = Modsim.RunSolver(m_ActiveModel);

                if (run == 0)
                {
                    messageOut("Sucessful completion of the MODSIM run!");
                }
                ProcessPumpingFactor(RRPreferences.rutaPumping, Convert.ToDouble(txtFactor.Text), checkFactor.Checked);
            }
            catch (Exception ex)
            {
                messageOut(String.Concat("ERROR: ", ex.Message));
            }

            if(runid!=-1)
                UpdateRunInfo(runid, run == 0 ? false : true, runFile);

            //Reload active network
            comboBoxMODSIMFile_SelectedIndexChanged(null, null);
            Cursor.Current = Cursors.Default;
        }

        static void ProcessPumpingFactor(string FileName, double factor, Boolean aplicafactor)
        {
            int line = 0;
            string lineOut;
            string lineIn;
            //string nombre1 = DateTime.Now.ToString("yyyy-MM-dd-HH-MM-ss");

            //StreamWriter sw = new StreamWriter(FileName + ".out");

            //StreamWriter sw = new StreamWriter(FileName.Replace(".wel", "_run" + nombre1 + ".wel"));
            if (aplicafactor==true)
            { 
            StreamWriter sw = new StreamWriter(FileName.Replace(".wel", "_run" + ".wel"));

            //string FileName1 = FileName.Replace("input\\MODFLOW", "output");

            //StreamWriter sw = new StreamWriter(FileName1.Replace(".wel", "_run" + ".wel"));

            using (StreamReader sr = File.OpenText(FileName))
            {
                while ((lineIn = sr.ReadLine()) != null)
                {
                    Console.Write("Procesando linea {0}\r", ++line);

                    if (line > 3)
                    {
                        if (!lineIn.Contains("#"))
                        {
                            string[] stringValues = lineIn.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                            int column1 = Int32.Parse(stringValues[0]);
                            int column2 = Int32.Parse(stringValues[1]);
                            int column3 = Int32.Parse(stringValues[2].Replace("-", ""));
                            double column4 = Convert.ToDouble(stringValues[3]);

                            column4 *= factor;

                            lineOut = string.Format("{0,10}{1,10}{2,10}{3,16:N2}", column1, column2, column3, column4);
                        }
                        else
                            lineOut = lineIn + "\r";
                    }
                    else
                        lineOut = lineIn + "\r";

                    sw.Write("{0}\n", lineOut);
                }
            }

            sw.Close();
            }
            else
            {
                File.Copy(FileName, FileName.Replace(".wel", "_run" + ".wel"));
            }

            //File.Delete(FileName);
            //File.Move(FileName + ".out", textBoxWorkspace.Text +);

            MessageBox.Show("Terminó!!!!!");
        }
        /// <summary>
        /// update run status in project database
        /// </summary>
        /// <param name="runid"></param>
        private void UpdateRunInfo(int runid, bool runIssues,string basePath)
        {
            try
            {
                string sql = "SELECT * FROM MMS_RunsInfo WHERE (RunID = " + runid + ")";

                DataTable runInfoDT = sqliteDB.GetTableFromDB(sql, "MMS_RunsInfo");

                if (runInfoDT.Rows.Count > 0)
                {
                    runInfoDT.Rows[0]["SimulationStatus"] = runIssues ? 3 : 2;
                    runInfoDT.Rows[0]["LastAccess"] = DateTime.Now.ToString();
                    runInfoDT.Rows[0]["BasePath"] = basePath;

                    sqliteDB.UpdateTableFromDB(runInfoDT);
                }
            }
            catch (Exception ex)
            {
                messageOut(String.Concat("ERROR: ",ex.Message));
            } 
        }

        private string BuildOptionsTxt()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(labelActFile.Text);
            foreach(TreeNode tn in treeViewPsdoCost.Nodes)
            {
                sb.AppendLine(tn.Text);
            }
            foreach (DataRow dr in ISFTargetsTbl.Rows)
            {
                sb.AppendLine(String.Concat(dr["Location"], ":", dr["Target Flow [cfs]"], " cfs"));               
            }
            return sb.ToString();
        }

        private void AdjustISFTargets()
        {
            foreach(DataRow dr in ISFTargetsTbl.Rows)
            {
                Node ISFNode = m_ActiveModel.FindNode(dr["Location"].ToString());
                if(ISFNode!=null)
                {
                    if (dr["Target Flow [cfs]"].ToString() != "<<variable>>")
                    {
                        if (ISFNode.m.adaDemandsM.dataTable.Rows.Count == 0)
                            ISFNode.m.adaDemandsM.dataTable.Rows.Add(new object[] { m_ActiveModel.TimeStepManager.dataStartDate, 0 });
                        ISFNode.m.adaDemandsM.dataTable.Rows[0][1] = (long)Math.Round(double.Parse(dr["Target Flow [cfs]"].ToString()) * m_ActiveModel.ScaleFactor, 0);
                        ISFNode.m.adaDemandsM.units = "cfs";
                    }
                }
            }
        }

        private void AdjustNetworkCost()
        {
            for (int i = 0; i < treeViewPsdoCost.Nodes.Count; i++)
            {
                TreeNode tn = treeViewPsdoCost.Nodes[i];
                int setCost = (int)nodeSetCost[tn.Name];
                int deltaCost = setCost - (int)costRange["Min"];
                if (tn.Name != "Riparian WRs" && tn.Name != "Normal Appropriative WR Block")
                {
                    messageOut($"    Processing nodes {tn.Name}: Cost={setCost}, DeltaCost={deltaCost}");
                    int countISF = 0;
                    foreach (Link l in m_ActiveModel.Links_Real)
                    {
                        if (tn.Name == "Instream Flow Target")
                        {
                            if (l.name.StartsWith("WR_ISF_"))
                            {
                                l.m.cost = setCost + countISF;
                                countISF += 1;
                            }
                        }
                        if (tn.Name == "Indoor Domestic")
                            if (l.name.EndsWith("_DomIndoor_Cost"))
                                l.m.cost += deltaCost;
                        if (tn.Name == "Outdoor Domestic")
                            if (l.name.EndsWith("_DomOutdoor_Cost"))
                                l.m.cost += deltaCost;
                        if (tn.Name == "Agriculture")
                        {
                            if (radioButtonWRIMS.Checked)
                            {
                                if (l.name.EndsWith("_DemOthers_Cost"))
                                    l.m.cost += deltaCost;
                            }
                            else
                            {
                                if (l.name.EndsWith("_DemAg_Cost"))
                                    l.m.cost += deltaCost;
                            }
                        }
                        if (tn.Name == "Other Demands")
                            if (l.name.EndsWith("_DemOthers_Cost"))
                                l.m.cost += deltaCost;
                    }
                }
            }
        }

        private string GetActiveMODSIMFile(string selectedTxt, string modsimFile)
        {
            if (selectedTxt.Contains("_DIV.xy"))
                modsimFile = modsimFile.Replace(".xy", "_DIV.xy");
            if (selectedTxt.Contains("_DIV_WR.xy"))
                modsimFile = modsimFile.Replace(".xy", "_DIV_WR.xy");
            if (selectedTxt.Contains("_DIV_WRTS.xy"))
                modsimFile = modsimFile.Replace(".xy", "_DIV_WRTS.xy");

            if (!File.Exists(modsimFile))
                throw new Exception($"ERROR: Selected file {modsimFile} does not exist.");
            
            return modsimFile;
        }

        private void OnMessageOut(string message)
        {
            messageOut(message);
        }

        public void RunCommandCom(string command, string arguments, bool permanent, string workingDir)
        {
            // runs model in the command line
            using (Process p = new Process())
            {
                ProcessStartInfo pi = new ProcessStartInfo();
                pi.Arguments = " " + (permanent ? "/K" : "/C") + " " + command + " " + arguments;
                pi.FileName = "cmd.exe";
                pi.WorkingDirectory = workingDir;
                p.StartInfo = pi;
                //pi.UseShellExecute = true;
                p.Start();

                // when window closes, the thread will continue 
                //p.WaitForExit();
                //p.Close();
            }
        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            if (radioButtonMODSIMOnly.Checked)
            {
                radioButtonAgPckge.Checked = false;
                radioButtonAgPckge.Enabled = false;
            }
            else
            {
                radioButtonAgPckge.Enabled = true;
            }
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        private void groupBox4_Enter(object sender, EventArgs e)
        {

        }

        private void comboBoxMODSIMFile_SelectedIndexChanged(object sender, EventArgs e)
        {
            modelReady = false;
            using (BackgroundWorker bgworker = new BackgroundWorker())
            {
                bgworker.DoWork += CalculateWRCostRange;
                pictureBoxStatus.Image = Resources.icons8_loading_67;
                bgworker.RunWorkerAsync(new object[] 
                                        { comboBoxMODSIMFile.Text });
            }
            
            if (comboBoxMODSIMFile.Text != "")
                buttonExecuteModel.Enabled = true;
            else
                buttonExecuteModel.Enabled = false;
            try
            {
                messageOut($"Active MODSIM file: {GetActiveMODSIMFile(comboBoxMODSIMFile.Text,_ModsimFile)}");
            }
            catch (Exception ex)
            {

                messageOut(ex.Message);
                comboBoxMODSIMFile.SelectedIndex = 0;
            }
            
        }

        private void CalculateWRCostRange(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            string _selectedTxt = args[0].ToString();
            

            costRange = new Dictionary<string, long>();
            costRange.Add("Min", long.MaxValue);
            costRange.Add("Max", long.MinValue);

            if(m_ActiveModel!=null)
            {
                m_ActiveModel.OnMessage -= OnMessageOut;
                m_ActiveModel.OnModsimError -= OnMessageOut;
            }
            m_ActiveModel = new Model();
            m_ActiveModel.OnMessage += OnMessageOut;
            m_ActiveModel.OnModsimError += OnMessageOut;

            //Create the ISF target table
            ISFTargetsTbl = new DataTable("ISFTargets");
            ISFTargetsTbl.Columns.Add("Location", typeof(String));
            ISFTargetsTbl.Columns.Add("Target Flow [cfs]", typeof(double));

            try
            {
                string m_FileName = GetActiveMODSIMFile(_selectedTxt,_ModsimFile);

                messageOut($"Reading MODSIM file: {m_FileName}");
                XYFileReader.Read(m_ActiveModel, m_FileName);
                messageOut("    Calculating water rights cost range...");
                int count = 0;
                int countRip = 0;
                int countISF = 0;
                foreach (Link l in m_ActiveModel.Links_Real)
                {
                    if (l.name.StartsWith("WR_"))
                    {
                        if (l.m.cost != _riparianCost)
                        {
                            if (!l.name.StartsWith("WR_ISF_"))
                            {
                                if (l.m.cost < costRange["Min"])
                                {
                                    costRange["Min"] = l.m.cost;
                                }
                                if (l.m.cost > costRange["Max"])
                                {
                                    costRange["Max"] = l.m.cost;
                                }
                                count += 1;
                            }
                            else
                            {
                                Node ISFNode = l.to;
                                double value = 0;
                                if (ISFNode.m.adaDemandsM.dataTable != null && ISFNode.m.adaDemandsM.dataTable.Rows.Count > 0)
                                {
                                    if (ISFNode.m.adaDemandsM.dataTable.Rows.Count == 1)
                                    {
                                        value = double.Parse(ISFNode.m.adaDemandsM.dataTable.Rows[0][1].ToString()) / m_ActiveModel.ScaleFactor;
                                        if (ISFNode.m.adaDemandsM.units != "cfs")
                                            value = ISFNode.m.adaDemandsM.units.ConvertTo(value, "cfs");
                                        ISFTargetsTbl.Rows.Add(new object[] { ISFNode.name, value });
                                    }
                                    else
                                    {
                                        ISFTargetsTbl.Rows.Add(new object[] { ISFNode.name, "<<variable>>" });
                                        messageOut("\tWARNING: Note that you cannot edit variable ISF target in this interface.");
                                    }
                                    countISF += 1;
                                }
                            }
                        }
                        else
                            countRip += 1;
                    }
                }
                messageOut($"    Done reading active model to memory.");
                if (count == 0)
                {
                    costRange["Min"] = 0;
                    costRange["Max"] = 0;
                    costRange["Increment"] = 0;
                    messageOut("No water rights links 'WR_*' were found.");
                }
                else
                    costRange.Add("Increment", (long)Math.Round(((double)costRange["Max"] - (double)costRange["Min"]) / count,0));
                
                labelCostBlock.BeginInvoke((Action)(() =>
                {
                    labelCostBlock.Text = $"{costRange["Min"]} to {costRange["Max"]} -> {count} links."; //Normal Water Right Cost Block:
                }));
                labelISFlinks.BeginInvoke((Action)(() =>
                {
                    labelISFlinks.Text = $"{countISF} links.";//Instream flow targets ('WR_ISF_*') ->
                }));
                labelRiparian.BeginInvoke((Action)(() =>
                {
                    labelRiparian.Text = $"{_riparianCost} -> {countRip} links."; //Riparian rights cost:
                }));
                dataGridViewISF.BeginInvoke((Action)(() =>
                {
                    dataGridViewISF.DataSource = ISFTargetsTbl;
                }));
                ProcessNodesText();
                pictureBoxStatus.BeginInvoke((Action)(() =>
                {
                    pictureBoxStatus.Image = Resources.icons8_done_64;
                }));
                labelActFile.BeginInvoke((Action)(() =>
                {
                    labelActFile.Text = "Active File: " + m_FileName;
                }));

                modelReady = true;
            }
            catch (Exception ex)
            {

                messageOut(String.Concat("ERROR: ",ex.Message));
            }
        }

        private void buttonNodeUP_Click(object sender, EventArgs e)
        {
            TreeNode node = treeViewPsdoCost.SelectedNode;
            TreeNode parent = node.Parent;
            TreeView view = node.TreeView;
            if (parent != null)
            {
                int index = parent.Nodes.IndexOf(node);
                if (index > 0)
                {
                    parent.Nodes.RemoveAt(index);
                    parent.Nodes.Insert(index - 1, node);
                }
            }
            else if (node.TreeView.Nodes.Contains(node)) //root node
            {
                int index = view.Nodes.IndexOf(node);
                if (index > 0)
                {
                    view.Nodes.RemoveAt(index);
                    view.Nodes.Insert(index - 1, node);
                }
            }
            //treeViewPsdoCost.SelectedNode = null;
            //buttonNodeUP.Enabled = false;
            //buttonNodeDW.Enabled = false;
            ProcessNodesText();
        }

        private void buttonNodeDW_Click(object sender, EventArgs e)
        {
            TreeNode node = treeViewPsdoCost.SelectedNode;
            TreeNode parent = node.Parent;
            TreeView view = node.TreeView;
            if (parent != null)
            {
                int index = parent.Nodes.IndexOf(node);
                if (index < parent.Nodes.Count - 1)
                {
                    parent.Nodes.RemoveAt(index);
                    parent.Nodes.Insert(index + 1, node);
                }
            }
            else if (view != null && view.Nodes.Contains(node)) //root node
            {
                int index = view.Nodes.IndexOf(node);
                if (index < view.Nodes.Count - 1)
                {
                    view.Nodes.RemoveAt(index);
                    view.Nodes.Insert(index + 1, node);
                }
            }
            //treeViewPsdoCost.SelectedNode = null;
            //buttonNodeUP.Enabled = false;
            //buttonNodeDW.Enabled = false;
            ProcessNodesText();
        }

        private void ProcessNodesText()
        {
            nodeSetCost = new Dictionary<string, long>();
            if (costRange == null)
                return;
            int currentCost = _riparianCost - 2* (int)costRange["Increment"];
            for(int i = 0; i < treeViewPsdoCost.Nodes.Count; i++)
            {
                TreeNode tn = treeViewPsdoCost.Nodes[i];
                string txt = i+1 + ". " + tn.Name;
                if (tn.Name == "Riparian WRs")
                {
                    txt += " (" + _riparianCost + ")";
                    currentCost = _riparianCost;
                }
                else if (tn.Name == "Normal Appropriative WR Block")
                { 
                    txt +=  $"({ costRange["Min"]} to { costRange["Max"]})";
                    currentCost = (int)costRange["Max"];
                }
                else
                {
                    currentCost += (int)costRange["Increment"];
                    if (tn.Name == "Instream Flow Target")
                    {
                        txt += " (" + currentCost + ")";
                    }
                    else
                    {
                        txt += $"({currentCost} to {currentCost - (int)(costRange["Min"]-costRange["Max"])})";
                        currentCost = currentCost - (int)(costRange["Min"] - costRange["Max"]);
                    }
                }
                
                nodeSetCost.Add(tn.Name, currentCost);
                treeViewPsdoCost.BeginInvoke((Action)(() =>
                {
                    tn.Text = txt;
                    //tn.Tag = currentCost;
                }));
                //treeViewPsdoCost.BeginInvoke((Action<int>)((int j) =>
                //{
                //    treeViewPsdoCost.Nodes[j].Text = txt;
                //    treeViewPsdoCost.Nodes[j].Tag = currentCost;
                //}), i);

            }
            treeViewPsdoCost.BeginInvoke((Action)(() =>
            {
                treeViewPsdoCost.SelectedNode = null;
            }));
            buttonNodeUP.BeginInvoke((Action)(() =>
            {
                buttonNodeUP.Enabled = false;
            }));
            buttonNodeDW.BeginInvoke((Action)(() =>
            {
                buttonNodeDW.Enabled = false;
            }));
            
        }

        private void checkBoxIndoorDomON_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxIndoorDomON.Checked)
            {
                treeViewPsdoCost.Nodes.Add("Indoor Domestic", "Indoor Domestic");
            }
            else
            {
                treeViewPsdoCost.Nodes.Remove(treeViewPsdoCost.Nodes["Indoor Domestic"]);
            }
            ProcessNodesText();
        }

        private void checkBoxOutdoorDomON_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxOutdoorDomON.Checked)
            {
                treeViewPsdoCost.Nodes.Add("Outdoor Domestic", "Outdoor Domestic");
            }
            else
            {
                treeViewPsdoCost.Nodes.Remove(treeViewPsdoCost.Nodes["Outdoor Domestic"]);
            }
            ProcessNodesText();
        }

        private void checkBoxAgON_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAgON.Checked)
            {
                treeViewPsdoCost.Nodes.Add("Agriculture", "Agriculture");
            }
            else
            {
                treeViewPsdoCost.Nodes.Remove(treeViewPsdoCost.Nodes["Agriculture"]);
            }
            ProcessNodesText();
        }

        private void checkBoxOthersON_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxOthersON.Checked)
            {
                treeViewPsdoCost.Nodes.Add("Other Demands", "Other Demands");
            }
            else
            {
                treeViewPsdoCost.Nodes.Remove(treeViewPsdoCost.Nodes["Other Demands"]);
            }
            ProcessNodesText();
        }

        private void treeViewPsdoCost_AfterSelect(object sender, TreeViewEventArgs e)
        {
            buttonNodeUP.Enabled = false;
            buttonNodeDW.Enabled = false;
            if (treeViewPsdoCost.SelectedNode == treeViewPsdoCost.Nodes["Other Demands"] ||
                treeViewPsdoCost.SelectedNode == treeViewPsdoCost.Nodes["Agriculture"] ||
                treeViewPsdoCost.SelectedNode == treeViewPsdoCost.Nodes["Outdoor Domestic"] ||
                //treeViewPsdoCost.SelectedNode == treeViewPsdoCost.Nodes["Indoor Domestic"] ||
                treeViewPsdoCost.SelectedNode == treeViewPsdoCost.Nodes["Agriculture"])
            {
                buttonNodeDW.Enabled = true;
            }
            if (treeViewPsdoCost.SelectedNode == treeViewPsdoCost.Nodes["Instream Flow Target"])
            {
                buttonNodeDW.Enabled = true;
                buttonNodeUP.Enabled = true;
            }
        }

        private void radioButtonMMSRun_CheckedChanged(object sender, EventArgs e)
        {
            groupBoxRunInfo.Visible = radioButtonMODSIMOnly.Checked;
        }

        private void radioButtonRunActive_CheckedChanged(object sender, EventArgs e)
        {
            groupBoxRunInfo.Visible = radioButtonMODSIMOnly.Checked;
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
        (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            // only allow one decimal point
            if ((e.KeyChar == '.') && ((sender as TextBox).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }
    }
}
