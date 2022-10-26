using RTI.CWR.MWC_MODSIMUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RRModelingSystem
{
    public delegate void ProcessSimulationRum(int runID, string fileName, List<string> MODSIMMsgs);  // delegate
    public partial class RRSurfModelingMain : Form
    {
        private Simulation m_SimUserControl;
        private DataProcessing m_DataProcessing;
        private RRPreferences m_RRPreferences;
        private RunsManager m_RunsManager;
        private string MMSDatabase { get; set; }
        private Dictionary<string,SimulationRun> simRunWindows;
        public RRSurfModelingMain()
        {
            InitializeComponent();
            //Initialize user controls
            //m_SimUserControl = new Simulation();
            //m_DataProcessing = new DataProcessing();
            //m_RRPreferences = new RRPreferences();
            simRunWindows = new Dictionary<string, SimulationRun>();
        }

        private void treeView1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            splitContainer1.Panel2.Controls.Clear();
            if (m_RRPreferences != null)
            {
                switch (treeView1.SelectedNode.Text)
                {
                    case "Preferences":
                        splitContainer1.Panel2.Controls.Add(m_RRPreferences);
                        m_RRPreferences.Dock = DockStyle.Fill;
                        if (m_DataProcessing != null)
                        {
                            m_RRPreferences.loading = true;
                            m_RRPreferences.textBoxPREFSRiparianCost.Text = m_DataProcessing.textBoxRiparianCost.Text;
                            m_RRPreferences.loading = false;
                        }
                            break;
                    case "Pre-Processing":
                        if (m_DataProcessing == null || m_RRPreferences.hasChanges)
                        {
                            m_DataProcessing = new DataProcessing(Path.Combine(m_RRPreferences.textBoxWorkspace.Text, m_RRPreferences.textBoxMMSDatabase.Text),
                                                                  Path.Combine(m_RRPreferences.textBoxWorkspace.Text, m_RRPreferences.textBoxMODSIMFile.Text),
                                                                  Path.Combine(m_RRPreferences.textBoxWorkspace.Text, m_RRPreferences.textBoxControlFile.Text),
                                                                  Path.Combine(m_RRPreferences.textBoxWorkspace.Text, m_RRPreferences.textBoxSyncingDB.Text));
                            m_DataProcessing.messageOut += ProcessMessage;
                            ProcessMessage($"Active MODSIM File: {m_RRPreferences.textBoxMODSIMFile.Text}");
                        }
                        //m_DataProcessing.ProjectDB = //@"C:\Users\etrianasanchez\Research Triangle Institute\USGS Russian River MODSIM Model - Documents\Modeling\MODSIM_GSFLOW\RRMS_Database.mdb";
                        m_DataProcessing.textBoxRiparianCost.Text = m_RRPreferences.textBoxPREFSRiparianCost.Text;
                        splitContainer1.Panel2.Controls.Add(m_DataProcessing);
                        m_DataProcessing.Dock = DockStyle.Fill;
                        break;
                    case "Coupled Simulation":
                        if (m_SimUserControl == null || m_RRPreferences.hasChanges)
                        {
                            if (m_RRPreferences.textBoxPREFSRiparianCost.Text == "")
                            {
                                MessageBox.Show("Cost for riparian links cannot be empty. Please define a value.");
                                treeView1.SelectedNode = treeView1.Nodes["Node0"];
                                treeView1_AfterSelect(null, null);
                                break;
                            }
                            
                            m_SimUserControl = new Simulation( m_RRPreferences.textBoxMODSIMFile.Text,
                                                                m_RRPreferences.textBoxSyncingDB.Text,
                                                                int.Parse(m_RRPreferences.textBoxPREFSRiparianCost.Text),
                                                                m_RRPreferences.textBoxMMSDatabase.Text,
                                                                m_RRPreferences.textBoxControlFile.Text,
                                                                m_RRPreferences.textBoxPumpingFile.Text,
                                                                m_RRPreferences.textBoxWorkspace.Text  );
                            m_SimUserControl.messageOut += ProcessMessage;
                            m_SimUserControl.simulationStarted += startSimulationRunWindow;
                            ProcessMessage($"Base MODSIM File: {m_RRPreferences.textBoxMODSIMFile.Text}");
                            ProcessMessage($"Active MODSIM-GSFLOW Sync Database: {m_RRPreferences.textBoxSyncingDB.Text}");
                        }

                        splitContainer1.Panel2.Controls.Add(m_SimUserControl);
                        m_SimUserControl.Dock = DockStyle.Fill;
                        break;
                    case "Runs Manager":
                        if (m_RunsManager == null || m_RRPreferences.hasChanges)
                        {
                            m_RunsManager = new RunsManager( m_RRPreferences.textBoxMMSDatabase.Text,
                                m_RRPreferences.textBoxWorkspace.Text);
                            m_RunsManager.MessageOut += ProcessMessage;
                            
                        }
                        splitContainer1.Panel2.Controls.Add(m_RunsManager);
                        m_RunsManager.Dock = DockStyle.Fill;
                        m_RunsManager.ReLoadForm();
                        break;
                    default:
                        if(simRunWindows.ContainsKey(treeView1.SelectedNode.Text))
                        {
                            //simRunWindows[treeView1.SelectedNode.Text].listView1.BeginInvoke((Action)(() =>
                            //{
                            //    splitContainer1.Panel2.Controls.Add(simRunWindows[treeView1.SelectedNode.Text]);
                            //    simRunWindows[treeView1.SelectedNode.Text].Dock = DockStyle.Fill;
                            //}));
                            splitContainer1.Panel2.Controls.Add(simRunWindows[treeView1.SelectedNode.Text]);
                            simRunWindows[treeView1.SelectedNode.Text].Dock = DockStyle.Fill;
                        }
                        break;
                }
                m_RRPreferences.hasChanges = false;
            }
        }

        private void startSimulationRunWindow(int runID, string fileName,List<string> runMgs)
        {
            string nodeName = "Run: " + runID.ToString();
            SimulationRun sRWin = new SimulationRun(runID, fileName,runMgs);
            if (simRunWindows.ContainsKey(nodeName))
                simRunWindows[nodeName] = sRWin;
            else
            {
                simRunWindows.Add(nodeName, sRWin);
                treeView1.BeginInvoke((Action)(() =>
                {
                    treeView1.Nodes["Node2"].Nodes.Add(nodeName, nodeName);
                }));
            }
        }

        private void ProcessMessage(string msg)
        {
            if (richTextBoxMsgs != null)
            {
                richTextBoxMsgs.BeginInvoke((Action)(() =>
                {
                    richTextBoxMsgs.SelectionStart = richTextBoxMsgs.TextLength;
                    richTextBoxMsgs.SelectionColor = richTextBoxMsgs.ForeColor;
                    if (msg.ToLower().Contains("error"))
                        richTextBoxMsgs.SelectionColor = System.Drawing.Color.Red;
                    if (msg.ToLower().Contains("warning"))
                        richTextBoxMsgs.SelectionColor = System.Drawing.Color.Orange;
                    richTextBoxMsgs.AppendText(DateTime.Now.ToString() + " " + msg + Environment.NewLine);
                    richTextBoxMsgs.ScrollToCaret();
                }));
            }
            else
            {
                Console.WriteLine(msg);
            }
            //richTextBoxMsgs.SelectionColor = richTextBoxMsgs.ForeColor;
            //if (msg.ToLower().Contains("error"))
            //    richTextBoxMsgs.SelectionColor = System.Drawing.Color.Red;
            //richTextBoxMsgs.AppendText(msg + Environment.NewLine);
            //richTextBoxMsgs.SelectionStart = richTextBoxMsgs.Text.Length;
            //// scroll it automatically
            //richTextBoxMsgs.ScrollToCaret();
            ////richTextBoxMsgs.AppendText(String.Format("[{0}] {1} {2}", (includeTime ? DateTime.Now.ToString() : ""), msg, (isnewline ? Environment.NewLine : null)));

        }

        private void loadProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "SQLite Database File (*.sqlite)|*.sqlite|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    LoadProject(dlg.FileName);
                    m_RRPreferences.hasChanges = true;
                }
            }
        }

        private void LoadProject(string fileName)
        {
            this.Text = $"RTI-USGS Conjunctive SW-GW Modeling System - {fileName}";
            MMSDatabase = fileName;
            m_RRPreferences = new RRPreferences(MMSDatabase);
            m_RRPreferences.messageOut += ProcessMessage;
            
            treeView1.SelectedNode = treeView1.Nodes["Node0"];
            treeView1_AfterSelect(null, null);
        }

        private void saveProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if(m_RRPreferences != null)
            {
                m_RRPreferences.SavePreferencesToDatabase();
                ProcessMessage("Project preferences saved to the MMS database.");
            }
        }

        private void newMMSProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            

        }

        private bool CreateProjectDatabase(string db_file, bool newProject)
        {
            try
            {
                if (newProject && File.Exists(db_file))
                {
                    if (MessageBox.Show("The project file already exist. Do you want to ovewrite the project file?", "New project name", MessageBoxButtons.YesNo) == DialogResult.No)
                        return false;
                    File.Delete(db_file);
                }

                using (MyDBSqlite sqhelper = new MyDBSqlite())
                {
                    sqhelper.messageOut += ProcessMessage;
                    sqhelper.SetupDatabase(db_file);
                }

                return true;
            }
            catch (Exception ex)
            {
                ProcessMessage("ERROR [DATABASE_SETUP]: " + ex.Message + ex.StackTrace);
                return false;
            }
        }

        public DialogResult InputBox(string title, string promptText, ref string value)
        {
            Form form = new Form();
            Label label = new Label();
            TextBox textBox = new TextBox();
            Button buttonOk = new Button();
            Button buttonCancel = new Button();

            form.Text = title;
            label.Text = promptText;
            textBox.Text = value;

            buttonOk.Text = "OK";
            buttonCancel.Text = "Cancel";
            buttonOk.DialogResult = DialogResult.OK;
            buttonCancel.DialogResult = DialogResult.Cancel;

            label.SetBounds(9, 20, 372, 13);
            textBox.SetBounds(12, 36, 372, 20);
            buttonOk.SetBounds(228, 72, 75, 23);
            buttonCancel.SetBounds(309, 72, 75, 23);

            label.AutoSize = true;
            textBox.Anchor = textBox.Anchor | AnchorStyles.Right;
            buttonOk.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;

            form.ClientSize = new System.Drawing.Size(396, 107);
            form.Controls.AddRange(new Control[] { label, textBox, buttonOk, buttonCancel });
            form.ClientSize = new System.Drawing.Size(Math.Max(300, label.Right + 10), form.ClientSize.Height);
            form.FormBorderStyle = FormBorderStyle.FixedDialog;
            form.StartPosition = FormStartPosition.CenterScreen;
            form.MinimizeBox = false;
            form.MaximizeBox = false;
            form.AcceptButton = buttonOk;
            form.CancelButton = buttonCancel;

            DialogResult dialogResult = form.ShowDialog();
            value = textBox.Text;
            return dialogResult;
        }

        private void logWindowToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainer2.Panel2Collapsed = !logWindowToolStripMenuItem.Checked;
        }

        private void navigationPaneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            splitContainer1.Panel1Collapsed = !navigationPaneToolStripMenuItem.Checked;
        }

        private void newDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // Set the help text description for the FolderBrowserDialog.
            FolderBrowserDialog folderBrowserDialog1 = new FolderBrowserDialog();
            folderBrowserDialog1.Description =
                "Select the workspace location for the new project.";

            // Do not allow the user to create new files via the FolderBrowserDialog.
            folderBrowserDialog1.ShowNewFolderButton = true;

            // Default to the My Documents folder.
            //this.folderBrowserDialog1.RootFolder = Environment.SpecialFolder.Personal;


            if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                string pName = "New Project";
                if (InputBox("New MMS Project", "New project name:", ref pName) == DialogResult.OK)
                {
                    string newProject = folderBrowserDialog1.SelectedPath + $"\\{pName}.sqlite";
                    CreateNewProjectInDB(newProject,isNewProject:true);

                    
                }
            }
        }

        private void CreateNewProjectInDB(string newProject, bool isNewProject)
        {
            this.Cursor = Cursors.WaitCursor;
            if (CreateProjectDatabase(newProject, isNewProject))
            {
                ProcessMessage($"INFO: project database created sucessfully.");
                LoadProject(newProject);
            }
            else
            {
                ProcessMessage($"WARNING: project creation not completed.");
            }
            this.Cursor = Cursors.Default;
        }

        private void inExistingDatabaseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "SQLite Database File (*.sqlite)|*.sqlite|WaterALLOC Database File (*.waprj)|*.waprj|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    CreateNewProjectInDB(dlg.FileName,isNewProject:false);
                }
            }
        }
    }

}
