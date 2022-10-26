using Csu.Modsim.ModsimIO;
using Csu.Modsim.ModsimModel;
using RTI.CWR.MWC_MODSIMUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RRModelingSystem
{
    public partial class SimulationRun : UserControl
    {
        private string _fileName;
        private string _runFileName;
        private int _runID;
        private bool _riparianON;
        private int _riparianCost;
        private Stopwatch sw ;
        private List<string> errorLines,maxLines;

        private Thread standardOutputThread;
        private Process process;
        private static StreamReader _standardOutput;
        private RiparianAllocation allocationTool;
        private int run;
        private List<string> _runMsgs;
        private long lastStatusTick = Environment.TickCount;
        private Model _ActiveModel;
        private MyDBSqlite sqliteDB { get; set; }

        public event ProcessMessage messageOut; // event

        public SimulationRun(int runID, string logFileName,string runFileName, bool riparianLogicOn,int riparianCost, string MMS_db, List<string> runMgs=null)
        {
            InitializeComponent();
            _fileName = logFileName;
            _runFileName = runFileName;
            _runID = runID;
            _riparianON = riparianLogicOn;
            _riparianCost = riparianCost;
            textBoxHeading.Text = $"Simulation Run Monitoring | Run: {runID}";
            sw = Stopwatch.StartNew();

            if (runMgs != null)
            {

                richTextBox1.Lines = runMgs.ToArray();
                buttonUpdate.Enabled = false;
                toolStripStatusLabel1.Text = "Simulation completed.";
                sw.Stop();
                _runMsgs = runMgs;
            }
            else
            {
                _runMsgs = new List<string>();
                toolStripStatusLabel1.Text = "Worker Initialized.";
            }

            sqliteDB = new MyDBSqlite(Path.Combine(MMS_db));
            sqliteDB.messageOut += OnMessageOut;
        }

        private void SimulationRun_Load(object sender, EventArgs e)
        {
            richTextBox1.Lines = _runMsgs.ToArray();
            AnalyzeMsgs();
            if (_fileName != "")
            {
                var item = listView1.Items.Add("Log File:");
                item.SubItems.Add(_fileName);
            }
            if (_runFileName != "")
            {
                var item = listView1.Items.Add("Run File:");
                item.SubItems.Add(_runFileName);
            }
        }

        public void StartSimulation()
        {
            toolStripStatusLabel1.Text = "Simulation initialized.";
            standardOutputThread = null;
            //Adding 'plug-ins'
            if (Path.GetExtension(_runFileName) ==".control")
            {
                try
                {
                    process = new Process();
                    process.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "MWC_MS_GSF_Run.exe";
                    string riparianArgs = _riparianON ? $"-RiparianON {_riparianCost} " : "";
                    process.StartInfo.Arguments = riparianArgs + "\"" + Path.GetFileName(_runFileName) + "\"";
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(_runFileName);
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.Start();
                    _standardOutput = process.StandardOutput;
                    standardOutputThread = startThread("StandardOutput", Path.Combine(Path.GetDirectoryName(_runFileName), $"MMS_Run{_runID}Log.txt"));
                    process.WaitForExit();
                    run = 0;
                }
                catch (Exception ex)
                {
                    OnMessageOut(ex.Message);
                    run = -1;
                    throw;
                }
                finally
                {
                    if (standardOutputThread != null)
                        standardOutputThread.Join();
                    if (process != null)
                        process.Dispose();
                    //buttonExecuteModel.BeginInvoke((Action)(() =>
                    //{
                    //    buttonExecuteModel.Visible = true;
                    //}));
                    toolStripProgressBar1.GetCurrentParent().BeginInvoke((Action)(() =>
                    {
                        toolStripProgressBar1.Value = 0;
                        toolStripStatusLabel1.Text = "Done.";
                    }));
                    
                }
            }
            else
            {
                try
                {
                    _ActiveModel = new Model();
                    
                    OnMessageOut($"Reading MODSIM file: {_runFileName}");
                    XYFileReader.Read(_ActiveModel, _runFileName);

                    //Adding 'plug-ins'
                    if (_riparianON)
                    {
                        OnMessageRunOut("\tActivating riparian logic allocation...");
                        allocationTool = new RiparianAllocation(ref _ActiveModel, _riparianCost);
                        allocationTool.messageOutRun += OnMessageRunOut;
                    }
                    else
                    {
                        _ActiveModel.OnMessage += OnMessageRunOut;
                        _ActiveModel.OnModsimError += OnMessageRunOut;
                    }
                    OnMessageOut("Executing MODSIM model...");
                    OnMessageRunOut($"File: {_ActiveModel.fname}");
                    run = Modsim.RunSolver(_ActiveModel);

                    if (run == 0)
                    {
                        OnMessageOut($"Sucessful completion of the MODSIM run!");
                    }
                    
                }
                catch (Exception ex)
                {
                    OnMessageOut(ex.Message);
                    throw;
                }
                finally
                {
                    //simulationStarted(_runid < 0 ? 0 : _runid, "", _runMsgs);
                }
            }
            if (_runID != -1)
                UpdateRunInfo(_runID, run == 0 ? 2 : 3);
            buttonRestart.Enabled = true;
        }

        private void UpdateRunInfo(int runid, int status)
        {
            try
            {
                string sql = "SELECT * FROM MMS_RunsInfo WHERE (RunID = " + runid + ")";

                DataTable runInfoDT = sqliteDB.GetTableFromDB(sql, "MMS_RunsInfo");

                if (runInfoDT.Rows.Count > 0)
                {
                    runInfoDT.Rows[0]["SimulationStatus"] = status; // runIssues ? 3 : 2;
                    runInfoDT.Rows[0]["LastAccess"] = DateTime.Now.ToString();
                    //runInfoDT.Rows[0]["BasePath"] = basePath.Replace(_workSpace, "");
                    sqliteDB.UpdateTableFromDB(runInfoDT);
                }
            }
            catch (Exception ex)
            {
                OnMessageOut(String.Concat("ERROR: ", ex.Message));
            }
        }

        private void OnMessageOut(string message)
        {
            messageOut($"\t [r:{_runID}] {message}");
            
        }

        private void OnMessageRunOut(string message)
        {
            _runMsgs.Add(message);
            if (message.Contains("percent done"))
            {
                SetStatusStripProgressValue(Convert.ToInt32(message.Replace("percent done ", "")));
            }
            else
            {
                if (message.ToLower().Contains("error"))
                    UpdateStatusMessage(message, true);
                else
                    UpdateStatusMessage(message);

                if (message.StartsWith("Done"))
                {
                    SetStatusStripProgressValue(0);
                    UpdateStatusMessage("Done.");
                }
            }
        }

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

        /// <summary>Start a thread.</summary>
        /// <param name="startInfo">start information for this thread</param>
        /// <param name="name">name of the thread</param>
        /// <returns>thread object</returns>
        private static Thread startThread(string name, string parameter)
        {
            //Thread t = new Thread(startInfo);
            var t = new Thread(() => writeStandardOutput(parameter));
            t.IsBackground = true;
            t.Name = name;
            t.Start();
            return t;
        }

        /// <summary>Thread which outputs standard output from the running executable to the appropriate file.</summary>
        private static void writeStandardOutput(string logFileName)
        {
            string _standardOutputFileName = logFileName;
            using (StreamWriter writer = File.CreateText(_standardOutputFileName))
            using (StreamReader reader = _standardOutput)
            {
                writer.AutoFlush = true;

                for (; ; )
                {
                    string textLine = reader.ReadLine();

                    if (textLine == null)
                        break;

                    writer.WriteLine(textLine);
                }
            }

            if (File.Exists(_standardOutputFileName))
            {
                FileInfo info = new FileInfo(_standardOutputFileName);

                // if the error info is empty or just contains eof etc.

                if (info.Length < 4)
                    info.Delete();
            }
        }

        private void AnalyzeMsgs()
        {
            errorLines = new List<string>();
            maxLines = new List<string>();
            foreach (string line in richTextBox1.Lines)
            {
                if (line.ToLower().Contains("error"))
                    errorLines.Add(line);

                if (line.ToLower().Contains("max"))
                    maxLines.Add(line);
                if(line.ToLower().Contains("elapsed run"))
                {
                    var item = listView1.FindItemWithText("GSFLOW");
                    if (item == null)
                    {
                        item = listView1.Items.Add((new ListViewItem(new string[] { "GSFLOW Time elapsed : ", line})));
                    }
                    else
                        item.SubItems[1].Text = line;
                }
                if(line.ToLower().Contains("elapsed: "))
                {
                    var item = listView1.FindItemWithText("MODSIM Time elapsed");
                    if (item == null)
                    {
                        item = listView1.Items.Add((new ListViewItem(new string[] { "MODSIM Time elapsed : ", line })));
                    }
                    else
                        item.SubItems[1].Text = line;
                }
                if (line.Contains("percent done"))
                {
                    toolStripProgressBar1.Value = Convert.ToInt32(line.Replace("percent done ", ""));
                }
                if (line.StartsWith("Done"))
                {
                    toolStripProgressBar1.Value = 0;
                    toolStripStatusLabel1.Text = "Simulation completed.";
                    sw.Stop();
                }
            }
            treeViewMsgGroup.Nodes["NodeErrors"].Text = "Errors: " + errorLines.Count;
            treeViewMsgGroup.Nodes["NodeConvergence"].Text = "Convergence Issues: " + maxLines.Count;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UpdateTxtFile();
        }

        private void treeViewMsgGroup_AfterSelect(object sender, TreeViewEventArgs e)
        {
            richTextBox2.Clear();
            if (treeViewMsgGroup.SelectedNode == treeViewMsgGroup.Nodes["NodeErrors"])
                richTextBox2.Lines = errorLines.ToArray();
            if (treeViewMsgGroup.SelectedNode == treeViewMsgGroup.Nodes["NodeConvergence"])
                richTextBox2.Lines = maxLines.ToArray();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            richTextBox2.Clear();
            List<string> searchLines = new List<string>();
            foreach (string line in richTextBox1.Lines)
            {
                if (line.ToLower().Contains(comboBoxSearch.Text))
                    searchLines.Add(line);
            }
            richTextBox2.Lines = searchLines.ToArray();
        }

        private void buttonRestart_Click(object sender, EventArgs e)
        {
            _runMsgs = new List<string>();
            if(_fileName!="")
                File.WriteAllText(_fileName, String.Empty);
            StartSimulation();
        }

        private void UpdateTxtFile()
        {
            if (_fileName != "")
            {
                FileStream fs = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
                using (StreamReader sr = new StreamReader(fs))
                {
                    richTextBox1.Text = sr.ReadToEnd();
                    sr.Close();
                }
            }
            else
            {
                richTextBox1.Lines = _runMsgs.ToArray();
            }
            
            var item = listView1.FindItemWithText("Time elapsed (Run start) : ");
            if (item == null)
                listView1.Items.Add(new ListViewItem(new string[] { "Time elapsed (Run start) : ", sw.Elapsed.TotalSeconds.ToString() + " sec." }));
            else
                item.SubItems[1].Text = sw.Elapsed.TotalSeconds.ToString() + " sec.";

            AnalyzeMsgs();
        }
    }
}
