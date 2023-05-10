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
using System.Runtime.InteropServices;
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
        private Stopwatch sw;
        private List<string> errorLines, maxLines;

        private Thread standardOutputThread;
        private Process process;
        private static StreamReader _standardOutput;
        private RiparianAllocation allocationTool;
        private int run;
        private List<string> _runMsgs;
        private long lastStatusTick = Environment.TickCount;
        private Model _ActiveModel;
        private MyDBSqlite sqliteDB { get; set; }
        private string _workspace;

        public event ProcessMessage messageOut; // event

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);


        public SimulationRun(int runID, string logFileName, string runFileName, bool riparianLogicOn, int riparianCost, 
                                string MMS_db, string workspace, List<string> runMgs = null)
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
                SetButtonEnabled(buttonUpdate, false);
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
            _workspace = workspace;

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
                item = listView1.Items.Add("MODSIM File:");
                string modsimFile = Path.Combine(_workspace, sqliteDB.GetRunsInfoValue(_runID, "ModsimFile"));
                item.SubItems.Add(modsimFile);
            }
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        public void StartSimulation(object sender, DoWorkEventArgs e)
        {
            toolStripStatusLabel1.Text = "Simulation initialized.";
            standardOutputThread = null;
            //Adding 'plug-ins'
            if (Path.GetExtension(_runFileName) == ".control" || Path.GetExtension(_runFileName) == ".xy")
            {
                try
                {
                    process = new Process();
                    process.StartInfo.FileName = AppDomain.CurrentDomain.BaseDirectory + "MWC_MS_GSF_Run.exe";
                    string riparianArgs = _riparianON ? $" -RiparianON {_riparianCost} " : "";
                    process.StartInfo.Arguments = "" + Path.GetFileName(_runFileName) + "" + riparianArgs  ;
                    process.StartInfo.WorkingDirectory = Path.GetDirectoryName(_runFileName);
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    SetButtonEnabled(buttonStopRun, true);
                    process.Start();
                    _standardOutput = process.StandardOutput;
                    standardOutputThread = startThread("StandardOutput", Path.Combine(Path.GetDirectoryName(_runFileName), $"MMS_Run{_runID}Log.txt"));
                    _fileName = Path.Combine(Path.GetDirectoryName(_runFileName), $"MMS_Run{_runID}Log.txt");
                    //SpinWait.SpinUntil(() => process.MainWindowHandle != IntPtr.Zero);
                    Thread.Sleep(100);  // <-- ugly hack
                    //SetWindowText(process.MainWindowHandle, "MWC_MS_GSF_Run.exe - Run " + _runID);
                    
                    Dictionary<string, object> runInfo = new Dictionary<string, object>();
                    runInfo.Add("ProcessID", process.Id);
                    sqliteDB.UpdateRunsInfoTable(_runID, runInfo);

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
                    process = null;
                    //buttonExecuteModel.BeginInvoke((Action)(() =>
                    //{
                    //    buttonExecuteModel.Visible = true;
                    //}));
                    UpdateStatusMessage("Done.");
                    SetStatusStripProgressValue(0);
                  
                    //toolStripProgressBar1.GetCurrentParent().BeginInvoke((Action)(() =>
                    //{
                    //    toolStripProgressBar1.Value = 0;
                    //    toolStripStatusLabel1.Text = "Done.";
                    //}));

                }
            }
            else
            {
                // This is not used anymore - issues with static model in the simulation.cs!!!
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
            {
                Dictionary<string, object> runInfo = new Dictionary<string, object>();
                runInfo.Add("SimulationStatus", run == 0 ? 2 : 3);
                runInfo.Add("LastAccess", DateTime.Now.ToString());
                runInfo.Add("ProcessID", -1);
                sqliteDB.UpdateRunsInfoTable(_runID, runInfo);
                //UpdateRunInfo(_runID, run == 0 ? 2 : 3);
            }
            SetButtonEnabled(buttonRestart, true);
        }

        public void CheckExecutingProcess(object sender, DoWorkEventArgs e)
        {
            string processID = sqliteDB.GetRunsInfoValue(_runID, "ProcessID");
            process = GetOpenProcess(processID);
            if (process != null)
            {
                toolStripStatusLabel1.Text = "Found running process.  Simulation in progress.";
                SetButtonEnabled(buttonRestart, false);
                SetButtonEnabled(buttonStopRun, true);
            }
            else
            {
                toolStripStatusLabel1.Text = "Simulation process completed.";
                SetButtonEnabled(buttonRestart, true);
                Dictionary<string, object> runInfo = new Dictionary<string, object>();
                runInfo.Add("ProcessID", -1);
                sqliteDB.UpdateRunsInfoTable(_runID, runInfo);
            }
        }


        private void SetButtonEnabled(Button button, bool v)
        {
            if (button.InvokeRequired)
            {
                button.BeginInvoke((Action)(() =>
                {
                    button.Enabled = v;
                }));
            }
            else
                button.Enabled = v;
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
                    toolStripProgressBar1.Value = value;
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
                if (line.ToLower().Contains("elapsed run"))
                {
                    var item = listView1.FindItemWithText("GSFLOW");
                    if (item == null)
                    {
                        item = listView1.Items.Add((new ListViewItem(new string[] { "GSFLOW Time elapsed : ", line })));
                    }
                    else
                        item.SubItems[1].Text = line;
                }
                if (line.ToLower().Contains("elapsed: "))
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
                if (line == "Done")
                {
                    toolStripProgressBar1.Value = 0;
                    toolStripStatusLabel1.Text = "Simulation completed.";
                    checkBoxAutoUpdate.Checked = false;
                    sw.Stop();
                }
            }
            treeViewMsgGroup.Nodes["NodeErrors"].Text = "Errors: " + errorLines.Count;
            treeViewMsgGroup.Nodes["NodeConvergence"].Text = "Convergence Issues: " + maxLines.Count;
            listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UpdateTxtFile(_fileName);
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
                if (line.ToLower().Contains(comboBoxSearch.Text.ToLower()))
                    searchLines.Add(line);
            }
            richTextBox2.Lines = searchLines.ToArray();
        }

        private void buttonRestart_Click(object sender, EventArgs e)
        {
            _runMsgs = new List<string>();
            if (_fileName != "")
                File.WriteAllText(_fileName, String.Empty);
            SetButtonEnabled(buttonRestart, false);
            richTextBox1.Clear();
            using (BackgroundWorker bgworker = new BackgroundWorker())
            {
                bgworker.DoWork += StartSimulation;
                bgworker.RunWorkerAsync(new object[] { });
            }
        }

        private void buttonStopRun_Click(object sender, EventArgs e)
        {
            if (process != null)
            {
                try
                {
                    Console.WriteLine("****** Processed killed by the user ********");
                    process.Kill();
                    process = null;
                    SetButtonEnabled(buttonStopRun, false);
                    SetButtonEnabled(buttonRestart, true);
                    checkBoxAutoUpdate.Checked = false;

                    Dictionary<string, object> runInfo = new Dictionary<string, object>();
                    runInfo.Add("SimulationStatus", 4); //incomplete run
                    runInfo.Add("LastAccess", DateTime.Now.ToString());
                    runInfo.Add("ProcessID", -1);
                    sqliteDB.UpdateRunsInfoTable(_runID, runInfo);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    throw;
                }
                
            }
        }

        public Process GetOpenProcess(string name)
        {
            if (name != "-1")
            {
                //here we're going to get a list of all running processes on
                //the computer
                foreach (Process clsProcess in Process.GetProcesses())
                {
                    //now we're going to see if any of the running processes
                    //match the currently running processes. Be sure to not
                    //add the .exe to the name you provide, i.e: NOTEPAD,
                    //not NOTEPAD.EXE or false is always returned even if
                    //notepad is running.
                    //Remember, if you have the process running more than once, 
                    //say IE open 4 times the loop thr way it is now will close all 4,
                    //if you want it to just close the first one it finds
                    //then add a return; after the Kill
                    //if (clsProcess.ProcessName.Contains(name))
                    if (clsProcess.Id == int.Parse(name))
                    {
                        //if the process is found to be running then we
                        //return a true
                        return clsProcess;
                    }
                }
            }
            //otherwise we return a false
            return null;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            UpdateTxtFile(_fileName);
        }

        private void checkBoxAutoUpdate_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAutoUpdate.Checked)
                timer1.Start();
            else
                timer1.Stop();
        }

        private void richTextBox2_TextChanged(object sender, EventArgs e)
        {

        }

        private void richTextBox2_Click(object sender, EventArgs e)
        {
            //int linenumber = richTextBox1.GetLineFromCharIndex(richTextBox1.Text.IndexOf(richTextBox1.SelectedText));
            //MessageBox.Show("Linenumber: " + (richTextBox1.Lines[linenumber + 1]).ToString());
            
        }

        private void richTextBox2_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                int p = richTextBox2.GetCharIndexFromPosition(e.Location);
                int line = richTextBox2.GetLineFromCharIndex(p);
                // code...
                //MessageBox.Show("Linenumber: " + (richTextBox2.Lines[line]).ToString());
                
                int wordstartIndex = richTextBox1.Find(richTextBox2.Lines[line]);
                if (wordstartIndex != -1)
                {
                    richTextBox1.SelectionStart = wordstartIndex;
                    richTextBox1.SelectionLength = richTextBox2.Lines[line].Length;
                    richTextBox1.SelectionBackColor = Color.Yellow;
                }
                //else
                //    break;
                //startindex += wordstartIndex + word.Length;
                richTextBox1.ScrollToCaret();
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            
        }

        private void listView1_MouseClick(object sender, MouseEventArgs e)
        {
            //Point localPoint = listView1.PointToClient(e.Location);
            ListViewItem item = listView1.GetItemAt(e.Location.X, e.Location.Y);
            string newTxt = _fileName;
            switch (item.Text)
            {
                case "Log File:":
                case "Run File:":
                case "MODSIM File:":
                    newTxt = item.SubItems[1].Text;
                    labelDisplayFile.Text = item.Text;
                    break;
            }

            UpdateTxtFile(newTxt);
        }

        public void UpdateTxtFile(string fileName="")
        {
            if (fileName == "")
            {
                fileName = _fileName;
                labelDisplayFile.Text = "Log File:";
            }

            if (fileName != "")
            {
                if (File.Exists(fileName))
                {
                    FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 4096, FileOptions.SequentialScan);
                    using (StreamReader sr = new StreamReader(fs))
                    {
                        richTextBox1.Text = sr.ReadToEnd();
                        sr.Close();
                    }
                }
                else
                {
                    messageOut("ERROR: Log file not found.");
                }
            }
            else
            {
                richTextBox1.Lines = _runMsgs.ToArray();
            }
            richTextBox1.SelectionStart = richTextBox1.TextLength;
            richTextBox1.ScrollToCaret();

            var item = listView1.FindItemWithText("Time elapsed (Run start) : ");
            if (item == null)
                listView1.Items.Add(new ListViewItem(new string[] { "Time elapsed (Run start) : ", sw.Elapsed.TotalSeconds.ToString() + " sec." }));
            else
                item.SubItems[1].Text = sw.Elapsed.TotalSeconds.ToString() + " sec.";

            AnalyzeMsgs();

            if (process != null)
            {
                SetButtonEnabled(buttonStopRun, true);
            }
        }
    }
}
