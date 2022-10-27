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
using System.Threading;

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
        public event ProcessSimulationRum simulationStarted; // event

        private Model m_ActiveModel;
        public bool modelReady { get; set; }
        private DataTable ISFTargetsTbl { get; set; }
        private Dictionary<string,long> nodeSetCost { get; set; }
        private  MyDBSqlite sqliteDB { get; set; }
        private  MyDBSqlite sqliteDBsync_db { get; set; }
        private string _rutaPumping;

        private static StreamReader _standardOutput;
        private Process process;
        //private Thread standardOutputThread;
        private List<string> runMsgs;
        private string _workSpace;
        private bool simDatesSet;

        public Simulation(string ModsimFile, string opsDB, int riparianCost, string MMS_db, string controlFile, string rutaPumping, string workSpace)
        {
            InitializeComponent();

            if (rutaPumping != "" && radioButtonMS_GS.Checked)
            {
                groupBox2.Visible = true;
            }
            else {
                groupBox2.Visible = false;
            }

            _ModsimFile = Path.Combine(workSpace,ModsimFile);
            _OpsDB = Path.Combine(workSpace, opsDB);
            _riparianCost = riparianCost;
            _controlFile = Path.Combine(workSpace, controlFile);
            if(rutaPumping!="")
                _rutaPumping = Path.Combine(workSpace, rutaPumping);
            _workSpace = workSpace;
            modelReady = false;
            sqliteDB = new MyDBSqlite(Path.Combine(workSpace, MMS_db));
            sqliteDB.messageOut += ProcessMessageOut;
            sqliteDBsync_db = new MyDBSqlite(Path.Combine(workSpace, opsDB));
            sqliteDBsync_db.messageOut += ProcessMessageOut;

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
            else
            {
                if (File.Exists(_controlFile))
                {
                    MODSIM_GSFLOW_C.ControlHelper ctrHlpr = new MODSIM_GSFLOW_C.ControlHelper(_controlFile);
                    string[] v = ctrHlpr.ReadKeyValue("start_time");
                    dateTimePickerStart.Value = new DateTime(int.Parse(v[0]), int.Parse(v[1]), int.Parse(v[2]));
                    v = ctrHlpr.ReadKeyValue("end_time");
                    dateTimePickerEnd.Value = new DateTime(int.Parse(v[0]), int.Parse(v[1]), int.Parse(v[2]));
                    messageOut("\tSimulation dates extracted from the control file.");
                    simDatesSet = true;
                }
            }
            
        }



        private void buttonImportTS_Click(object sender, EventArgs e)
        {
            Cursor.Current = Cursors.WaitCursor;
            buttonExecuteModel.Enabled = false;
            runMsgs = new List<string>();
            int runid = -1;

            string m_DateTime = DateTime.Now.ToString("yyyy-MM-dd HH:MM:ss");
            string sql = "INSERT INTO MMS_RunsInfo (ScnName, SimulationStatus, Keyword, LastAccess, Notes, Options) VALUES ('{0}',{1},'{2}','{3}','{4}','{5}')";
            if (!radioButtonMMSRun.Checked)
                sql = "INSERT OR REPLACE INTO MMS_RunsInfo (runID, ScnName, SimulationStatus, Keyword, LastAccess, Notes, Options) VALUES (0,'{0}',{1},'{2}','{3}','{4}','{5}')";

            string varTxt = BuildOptionsTxt(comboBoxPumpingScn.Text);
            sql = string.Format(sql, textBoxScnName.Text, 0, comboBoxKeyword.Text, m_DateTime, richTextBoxRunNotes.Text, varTxt);
            runid = sqliteDB.ExecuteQuery(sql);
            messageOut($"Logged run {runid} to the MMS database under keyword {comboBoxKeyword.Text}.\n");



            //find output location and file name

            string runFile = GetActiveMODSIMFile(comboBoxMODSIMFile.Text, _ModsimFile);
            if (runid >0)
            {
                if (checkBoxUseInName.Checked)
                    runFile = runFile.Replace(".xy", $"_{textBoxScnName.Text}.xy");
                runFile = runFile.Replace(".xy", $"_r{runid}.xy");
            }

            if (runid >0 && comboBoxKeyword.Text != "")
            {
                string folder = Path.Combine(Path.GetDirectoryName(runFile), comboBoxKeyword.Text);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);
                runFile = Path.Combine(folder, Path.GetFileName(runFile));
            }

            //string runFile = "";
            string runControlFile = _controlFile;
            try
            {
                if (m_ActiveModel == null)
                    throw new Exception("ERROR: Active model is not loaded in memory.  Try again later or select a different active model.");

                //Processing Management Options
                OnMessageRunOut("\tAdjusting costs in the MODSIM network...");
                AdjustNetworkCost(ref m_ActiveModel);

                //Setting ISF Targets
                OnMessageRunOut("\tSetting ISF targets...");
                AdjustISFTargets(ref m_ActiveModel);

                m_ActiveModel.TimeStepManager.startingDate = dateTimePickerStart.Value;
                m_ActiveModel.TimeStepManager.endingDate = dateTimePickerEnd.Value;
                m_ActiveModel.TimeStepManager.UpdateTimeStepsInfo(m_ActiveModel.timeStep); // redo the time steps info in case time step or dataend date changed.
                OnMessageRunOut("\tSetting simulation start and end dates...");

                OnMessageRunOut("\tSaving changes to active network...");
                ////find output location and file name
                //runFile = GetActiveMODSIMFile(_comboMODSIMFile, _ModsimFile);
                //if (_runid != -1)
                //{
                //    if (checkBoxUseInName.Checked)
                //        runFile = runFile.Replace(".xy", $"_{textBoxScnName.Text}.xy");
                //    runFile = runFile.Replace(".xy", $"_r{_runid}.xy");
                //}

                //if (_runid != -1 && _comboBoxKeyword != "")
                //{
                //    string folder = Path.Combine(Path.GetDirectoryName(runFile), _comboBoxKeyword);
                //    if (!Directory.Exists(folder))
                //        Directory.CreateDirectory(folder);
                //    runFile = Path.Combine(folder, Path.GetFileName(runFile));
                //}
                XYFileWriter.Write(m_ActiveModel, runFile);

                //Processing GSFLOW Input files
                if (radioButtonMS_GS.Checked)
                {

                    //radioButtonAgPckge
                    if (radioButtonWRIMS.Checked)
                    {
                        ProcessDB("1");
                    }
                    if (radioButtonAgPckge.Checked)
                    {
                        ProcessDB("2");
                    }

                    //buttonExecuteModel.BeginInvoke((Action)(() =>
                    //{
                    //    buttonExecuteModel.Visible = false;
                    //}));

                    toolStripStatusLabel1.Text = "MODSIM-GSFLOW Simulation in progress ...";
                    messageOut("\tActivating MODSIM-GSFLOW simulation mode...");

                    //Set input directories
                    if (runid > 0)
                    {
                        string inputFolder = Path.GetDirectoryName(_controlFile);
                        CopyFilesRecursively(inputFolder, inputFolder + "_r" + runid,"output");
                        runControlFile = Path.Combine(inputFolder + "_r" + runid, Path.GetFileName(_controlFile));
                        File.Move(runControlFile, runControlFile.Replace(".control", $"r{runid}.control"));
                        runControlFile = runControlFile.Replace(".control", $"r{runid}.control");
                    }

                    //ProcessPumpingFactor(RRPreferences.rutaPumping, Convert.ToDouble(txtFactor.Text), checkFactor.Checked);

                    //// TODO: Need to update the xyfile in the control file.
                    MODSIM_GSFLOW_C.ControlHelper ctrHlpr = new MODSIM_GSFLOW_C.ControlHelper(runControlFile);
                    ctrHlpr.messageOut += OnMessageOut;
                    ctrHlpr.ReplaceKeyRelativePath("xyFileName", new string[] { runFile });
                    ctrHlpr.ReplaceKeyRelativePath("mappingFileName", new string[] { _OpsDB });
                    ctrHlpr.ReplaceKeyValue("start_time", new string[] { dateTimePickerStart.Value.Year.ToString(),
                                                                                dateTimePickerStart.Value.Month.ToString(),
                                                                                dateTimePickerStart.Value.Day.ToString(),"0","0","0"  });
                    ctrHlpr.ReplaceKeyValue("end_time", new string[] { dateTimePickerEnd.Value.Year.ToString(),
                                                                                dateTimePickerEnd.Value.Month.ToString(),
                                                                                dateTimePickerEnd.Value.Day.ToString(),"0","0","0" });



                    string[] namName = ctrHlpr.ReadKeyValue("modflow_name");
                    string namPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(runControlFile), namName[0]));
                    if (runid > 0)
                    {
                        File.Move(namPath, namPath.Replace(".nam", $"r{runid}.nam"));
                        namPath = namPath.Replace(".nam", $"r{runid}.nam");
                    }
                    MODSIM_GSFLOW_C.ControlHelper namHlpr = new MODSIM_GSFLOW_C.ControlHelper(namPath);
                    namHlpr.messageOut += OnMessageOut;
                    //Process pumping file with user factors - Only done if in MS-GSF mode
                    if (_rutaPumping != null && _rutaPumping != "")
                    {
                        string[] wellVals = namHlpr.ReadLineWithKeyValue("WEL");
                        string outputWELFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(runControlFile), wellVals[2]));
                        if (runid > 0)
                            outputWELFile = outputWELFile.Replace("SRP_mf_strm_dpl_v0_run.wel", $"SRP_mf_strm_dpl_v0_run{runid}.wel");
                        if (_rutaPumping == outputWELFile)
                        {
                            messageOut("The output .wel file is the same than the seed.  They should be different to avoid overwritting the seed pumping file.");
                            throw new Exception("The seed.wel file would be overwritten - simulation stopped.");
                        }
                        ProcessPumpingFactor(checkFactor.Checked, Convert.ToDouble(txtFactor.Text), comboBoxPumpingScn.Text, outputWELFile);
                        namHlpr.ReplaceString("SRP_mf_strm_dpl_v0_run.wel", $"SRP_mf_strm_dpl_v0_run{runid}.wel");
                    }

                    //Set ouput directories
                    if (runid > 0)
                    {
                        ctrHlpr.ReplaceString("output\\", $"output_r{runid}\\");
                        ctrHlpr.CreatePaths($"output_r{runid}\\");

                        namHlpr.ReplaceString("output\\", $"output_r{runid}\\");
                        namHlpr.CreatePaths($"output_r{runid}\\");
                        //namHlpr.SaveChangesToFile(namPath.Replace(".nam", $"r{runid}.nam"));

                        ctrHlpr.ReplaceKeyRelativePath("modflow_name", new string[] { namPath });
                        //runControlFile = runControlFile.Replace(".control", $"r{runid}.control");
                        //ctrHlpr.SaveChangesToFile(runControlFile);
                    }
                    else
                    {
                        ////no changes in the output folder of base files
                        //ctrHlpr.SaveChangesToFile();  //save changes to the base control
                        //namHlpr.SaveChangesToFile();
                    }
                    ctrHlpr.SaveChangesToFile();  //save changes to the working control
                    namHlpr.SaveChangesToFile();
                }

            }
            catch (Exception ex)
            {
                messageOut(String.Concat("ERROR: ", ex.Message));
            }

            if (runid != -1)
            {
                Dictionary<string, object> runInfo = new Dictionary<string, object>();
                runInfo.Add("SimulationStatus", 1);
                runInfo.Add("LastAccess", DateTime.Now.ToString());
                runInfo.Add("BasePath", runFile.Replace(_workSpace, ""));
                runInfo.Add("RiparianON", checkBoxRiparianLogic.Checked);
                runInfo.Add("OutputDBScenario", false);
                runInfo.Add("RunType", radioButtonMODSIMOnly.Checked ? "MODSIMOnly" : "MODSIM-GSFLOW");
                sqliteDB.UpdateRunsInfoTable(runid, runInfo);
                //UpdateRunInfo(_runid, 1, _runFile);
            }

            using (BackgroundWorker bgworker = new BackgroundWorker())
            {
                bgworker.DoWork += RunSimulation;
                bgworker.RunWorkerAsync(new object[]
                                        { runFile,
                                          runid,
                                          runControlFile});
            }
            //Reload active network
            modelReady = false;
            buttonExecuteModel.Enabled = false;
            pictureBoxStatus.Image = Resources.icons8_error_64;
            m_ActiveModel = null;
            if (radioButtonMMSRun.Checked)
                comboBoxMODSIMFile_SelectedIndexChanged(null, null);
            else
            {   
                //comboBoxMODSIMFile.Text = "";
                //comboBoxMODSIMFile.SelectedIndex = -1;
            }
                
            Cursor.Current = Cursors.Default;

        }

        private static void CopyFilesRecursively(string sourcePath, string targetPath, string omitFolderContaining ="")
        {
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
            {
                if(omitFolderContaining!="" && !dirPath.ToLower().Contains(omitFolderContaining.ToLower()))
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
            {
                if (omitFolderContaining != "" && !Path.GetDirectoryName(newPath).ToLower().Contains(omitFolderContaining.ToLower()))
                    File.Copy(newPath, newPath.Replace(sourcePath, targetPath), true);
            }
        }


        private void RunSimulation(object sender, DoWorkEventArgs e)
        {
            object[] args = e.Argument as object[];
            string _runFile = args[0].ToString();
            int _runid = int.Parse(args[1].ToString());
            string runControlFile = args[2].ToString();

            //standardOutputThread = null;
            messageOut($"\n\t Simulation worker for run {_runid} initializing on thread [{Thread.CurrentThread.ManagedThreadId}].");
            try
            {
                if (radioButtonMS_GS.Checked)
                {
                    //Start the run execution/monitoring control
                    string logFileName = Path.Combine(Path.GetDirectoryName(runControlFile), $"MMS_Run{_runid}Log.txt");
                    _runFile = runControlFile;
                    simulationStarted(_runid, logFileName, _runFile, checkBoxRiparianLogic.Checked, _riparianCost, null);
                }
                else
                {
                    simulationStarted(_runid, "", _runFile, checkBoxRiparianLogic.Checked, _riparianCost, null);

                }
            }
            catch (Exception ex)
            {
                messageOut(ex.Message);
                throw;
            }

           

        }

       
        void ProcessDB(string opcion)
        {
            string sql;
            if (opcion == "1")
            {
                sql = "UPDATE [MS-GSF_mapping_Info] SET AgDem = 0 WHERE AssocDem Is not null";
                sqliteDBsync_db.ExecuteQuery(sql);
            }
            if (opcion == "2")
            {
                sql = "UPDATE [MS-GSF_mapping_Info] SET AgDem = 1 WHERE AssocDem Is not null";
                sqliteDBsync_db.ExecuteQuery(sql);
            }
        }

        private void ProcessPumpingFactor(Boolean aplicafactor, double factor, string tipo, string outputWELFile)
        {
            int line = 0;
            string lineOut;
            string lineIn;
            string tipofactor;
            switch (tipo)
            {
                case "Multiplier factor in agricultural groundwater pumping":
                    tipofactor = "irr_ag";
                    break;
                case "Multiplier factor in municipal and industrial groundwater pumping":
                    tipofactor = "mni";
                    break;
                case "Multiplier factor in all groundwater pumping":
                    tipofactor = "todos";
                    break;
                case "Multiplier factor in residential groundwater pumping":
                    tipofactor = "rur_dom";
                    break;
                case "Multiplier factor in outdoor residential groundwater pumping":
                    tipofactor = "rur_dom";
                    break;
                case "Multiplier factor in indoor residential groundwater pumping":
                    tipofactor = "rur_dom";
                    break;
                default:
                    tipofactor = tipo;
                    break;
            }
            if (aplicafactor)
            {
                StreamWriter sw = new StreamWriter(outputWELFile);
                using (StreamReader sr = File.OpenText(_rutaPumping))
                {
                    while ((lineIn = sr.ReadLine()) != null)
                    {
                        if (!lineIn.StartsWith("#"))
                        {
                            //Found the first line
                            lineOut = lineIn + "\r";
                            sw.Write("{0}\n", lineOut);
                            break;
                        }
                        else
                            lineOut = lineIn + "\r";
                        sw.Write("{0}\n", lineOut);
                    }
                    int numcolumnas;
                    while ((lineIn = sr.ReadLine()) != null)
                    {
                        numcolumnas = 0;
                        if (!lineIn.StartsWith("#") && !lineIn.StartsWith("specify"))
                        {
                            string[] stringValues = lineIn.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                            Console.Write("Processing line {0}\r", ++line);
                            int noRows = int.Parse(stringValues[0]);

                            lineOut = lineIn + "\r";
                            sw.Write("{0}\n", lineOut);

                            for (int r= 0; r < noRows; r++)
                            {
                                lineIn = sr.ReadLine();
                                if (!lineIn.StartsWith("#"))
                                {
                                    stringValues = lineIn.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                    numcolumnas = stringValues.Length;
                                    int column1 = Int32.Parse(stringValues[0]);
                                    int column2 = Int32.Parse(stringValues[1]);
                                    int column3 = Int32.Parse(stringValues[2].Replace("-", ""));
                                    double column4 = Convert.ToDouble(stringValues[3]);
                                    if (numcolumnas == 6)
                                    {
                                        string column5 = stringValues[4];
                                        string column6 = stringValues[5];
                                        if (tipofactor == "todos")
                                        {
                                            column4 *= factor;
                                        }
                                        else
                                        {
                                            if (column6 == tipofactor)
                                            {
                                                column4 *= factor;
                                            }
                                            else
                                            {
                                                column4 = column4 * 1;
                                            }
                                        }
                                        lineOut = string.Format("{0,10}{1,10}{2,10}{3,16:F2}     {4,-16} {5,-16}", column1, column2, column3, column4, column5, column6);
                                    }
                                    else {
                                        column4 *= factor;
                                        lineOut = string.Format("{0,10}{1,10}{2,10}{3,16:N2}", column1, column2, column3, column4);
                                    }

                                    
                                   // lineOut = string.Format("{0,10}{1,10}{2,10}{3,16:N2}{4,10}{5,10}", column1, column2, column3, column4, column5, column6);
                                }
                                else
                                lineOut = lineIn + "\r";
                                sw.Write("{0}\n", lineOut);
                            }
                            //else
                            //    lineOut = lineIn + "\r";
                        }
                    }
                }

                sw.Close();
                messageOut($"Pumping factor applied in file {outputWELFile}");
            }
            else
            {
                File.Copy(_rutaPumping, outputWELFile, true);
                messageOut($"Copying base file of pumping flows {outputWELFile}");
            }
        }
        /// <summary>
        /// update run status in project database
        /// </summary>
        /// <param name="runid"></param>
        private void UpdateRunInfo(int runid, int status,string basePath)
        {
            try
            {
                string sql = "SELECT * FROM MMS_RunsInfo WHERE (RunID = " + runid + ")";

                DataTable runInfoDT = sqliteDB.GetTableFromDB(sql, "MMS_RunsInfo");

                if (runInfoDT.Rows.Count > 0)
                {
                    runInfoDT.Rows[0]["SimulationStatus"] = status; // runIssues ? 3 : 2;
                    runInfoDT.Rows[0]["LastAccess"] = DateTime.Now.ToString();
                    runInfoDT.Rows[0]["BasePath"] = basePath.Replace(_workSpace, "");
                    sqliteDB.UpdateTableFromDB(runInfoDT);
                }
            }
            catch (Exception ex)
            {
                messageOut(String.Concat("ERROR: ",ex.Message));
            } 
        }

        private string BuildOptionsTxt(string comboPumpingText)
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
            if(comboPumpingText!="")
                sb.AppendLine(comboPumpingText + ":" + txtFactor.Text);

            return sb.ToString();
        }

        private void AdjustISFTargets(ref Model _ActiveModel)
        {
            foreach(DataRow dr in ISFTargetsTbl.Rows)
            {
                Node ISFNode = _ActiveModel.FindNode(dr["Location"].ToString());
                if(ISFNode!=null)
                {
                    if (dr["Target Flow [cfs]"].ToString() != "<<variable>>")
                    {
                        if (ISFNode.m.adaDemandsM.dataTable.Rows.Count == 0)
                            ISFNode.m.adaDemandsM.dataTable.Rows.Add(new object[] { m_ActiveModel.TimeStepManager.dataStartDate, 0 });
                        ISFNode.m.adaDemandsM.dataTable.Rows[0][1] = (long)Math.Round(double.Parse(dr["Target Flow [cfs]"].ToString()) * m_ActiveModel.ScaleFactor, 0);
                        ISFNode.m.adaDemandsM.units = "cfs";
                    }
                    else
                    {
                        OnMessageRunOut("WARNING - Processing variable target. NOT IMPLEMENTED");
                    }
                }
            }
        }

        private void AdjustNetworkCost(ref Model _ActiveModel)
        {
            for (int i = 0; i < treeViewPsdoCost.Nodes.Count; i++)
            {
                TreeNode tn = treeViewPsdoCost.Nodes[i];
                int setCost = (int)nodeSetCost[tn.Name];
                int deltaCost = setCost - (int)costRange["Min"];
                if (tn.Name != "Riparian WRs" && tn.Name != "Normal Appropriative WR Block")
                {
                    OnMessageRunOut($"    Processing nodes {tn.Name}: Cost={setCost}, DeltaCost={deltaCost}");
                    int countISF = 0;
                    foreach (Link l in _ActiveModel.Links_Real)
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

        private void OnMessageRunOut(string message)
        {
            runMsgs.Add(message);
            if (message.Contains("percent done"))
            {
                SetStatusStripProgressValue(Convert.ToInt32(message.Replace("percent done ", "")));
            }
            else
            {
                if(message.ToLower().Contains("error"))
                    UpdateStatusMessage(message,true);
                else
                    UpdateStatusMessage(message);

                if (message.StartsWith("Done"))
                {
                    SetStatusStripProgressValue(0);
                    UpdateStatusMessage("Done.");
                }
            }
        }

        private long lastStatusTick = Environment.TickCount;
        
        public void UpdateStatusMessage(string message, bool runError = false)
        {
            //If a model is running limit message display to once per milisecond
            //to fix the statusStripMessage freezing Modsim if messages are being
            //sent really really really fast and the display can't keep up
            if (!runError)
            {
                long currentTick = Environment.TickCount;

                if (currentTick > lastStatusTick + 1)
                {
                    if (!message.Contains("Last Iter") && message != "writing output")
                    {
                        toolStripStatusLabel1.Text = message;
                        lastStatusTick = currentTick;
                    }
                }
                if (message == "Done")
                {
                    toolStripStatusLabel1.Text = message;
                }
            }
            else
            {
                toolStripStatusLabel1.Text = message;
            }
        }

        private delegate void SetStatusStripProgressValueDelegate(int value);
        private void SetStatusStripProgressValue(int value)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (toolStripProgressBar1.GetCurrentParent().InvokeRequired)
            {
                toolStripProgressBar1.GetCurrentParent().BeginInvoke((Action)(() =>
                {
                    toolStripProgressBar1.Value = 0;
                    //toolStripStatusLabel1.Text = "Done.";
                }));
                SetStatusStripProgressValueDelegate d =
                    new SetStatusStripProgressValueDelegate(SetStatusStripProgressValue);
                this.Invoke(d, new object[] { value });
            }
            else
            {
                toolStripProgressBar1.Value = value;
            }
        }

        //public void RunCommandCom(string command, string arguments, bool permanent, string workingDir)
        //{
        //    // runs model in the command line
        //    using (Process p = new Process())
        //    {
        //        ProcessStartInfo pi = new ProcessStartInfo();
        //        pi.Arguments = " " + (permanent ? "/K" : "/C") + " " + command + " " + arguments;
        //        pi.FileName = "cmd.exe";
        //        pi.WorkingDirectory = workingDir;
        //        p.StartInfo = pi;
        //        //pi.UseShellExecute = true;
        //        p.Start();

        //        // when window closes, the thread will continue 
        //        //p.WaitForExit();
        //        //p.Close();
        //    }
        //}

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
            //m_ActiveModel.OnMessage += OnMessageOut;
            //m_ActiveModel.OnModsimError += OnMessageOut;

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
                                if (ISFNode.m.adaDemandsM.dataTable != null)
                                {
                                    if (ISFNode.m.adaDemandsM.dataTable.Rows.Count == 0)
                                    {
                                        ISFNode.m.adaDemandsM.dataTable.Rows.Add(m_ActiveModel.TimeStepManager.dataStartDate, 0);
                                    }
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

                if(!simDatesSet)
                {
                    dateTimePickerStart.Value = m_ActiveModel.TimeStepManager.startingDate;
                    dateTimePickerEnd.Value = m_ActiveModel.TimeStepManager.endingDate;
                    messageOut("\tSimulation dates set from MODSIM active file.");
                }

                modelReady = true;
                if (buttonExecuteModel.InvokeRequired)
                {
                    buttonExecuteModel.BeginInvoke((Action)(() =>
                    {
                        buttonExecuteModel.Enabled = true;
                    }));
                }
                else
                    buttonExecuteModel.Enabled = true;

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
            groupBoxRunInfo.Enabled = radioButtonMMSRun.Checked;
        }

        private void radioButtonRunActive_CheckedChanged(object sender, EventArgs e)
        {
            groupBoxRunInfo.Enabled = radioButtonMMSRun.Checked;
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

        private void checkFactor_CheckStateChanged(object sender, EventArgs e)
        {
            comboBoxPumpingScn.Visible = checkFactor.Checked;
            txtFactor.Visible = checkFactor.Checked;
        }

        private void radioButtonMS_GS_CheckedChanged(object sender, EventArgs e)
        {
            if (_rutaPumping != "" && radioButtonMS_GS.Checked)
            {
                groupBox2.Visible = true;
            }
            else
            {
                groupBox2.Visible = false;
            }


        }

        private void radioButtonGSFLOWRun_CheckedChanged(object sender, EventArgs e)
        {
            checkBoxRiparianLogic.Checked = !radioButtonGSFLOWRun.Checked;
        }
    }
}
