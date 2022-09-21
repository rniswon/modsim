using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using RTI.CWR.MMS_Support;

namespace RRModelingSystem
{
    public partial class RRPreferences : UserControl
    {
        public event ProcessMessage messageOut; // event

        public bool hasChanges;
        public string ProjectDatabase { get; set; }
        public string ModsimFile { get; set; }
        private FolderBrowserDialog folderBrowserDialog1;
        private MyDBSqlite m_DBUtils;

        private string _MMSDatabase { get; set; }
        public Dictionary<string, DataRow> MMSPrefs { get; private set; }

        private DataTable prefsTbl;
        private bool loading = true;
        public static string rutaPumping = "";

        public RRPreferences(string MMSDatabase)
        {
            InitializeComponent();
            _MMSDatabase = MMSDatabase;
        }

        private void RRPreferences_Load(object sender, EventArgs e)
        {
            m_DBUtils = new MyDBSqlite(_MMSDatabase);
            m_DBUtils.messageOut += PrintMessage;
            LoadPreferences();
        }

        public void SavePreferencesToDatabase()
        {
            m_DBUtils.UpdateTableFromDB(prefsTbl);
        }

        private void LoadPreferences()
        {
            loading = true;
            MMSPrefs = new Dictionary<string, DataRow>();
            ClearPrefsText();
            textBoxWorkspace.Text = Path.GetDirectoryName(_MMSDatabase) + "\\";
            
            prefsTbl = m_DBUtils.GetTableFromDB("SELECT * FROM MMS_Preferences", "MMS_Preferences");

            foreach (DataRow dr in prefsTbl.Rows)
            {
                MMSPrefs.Add(dr[0].ToString(), dr);
                switch (dr[0].ToString())
                {
                    case "MODSIM File":
                        textBoxMODSIMFile.Text = dr[1].ToString();
                        break;
                    case "Workspace":
                        //Get the full path
                        string fullPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_MMSDatabase), dr[1].ToString()));
                        textBoxWorkspace.Text = fullPath.EndsWith("\\")?fullPath:fullPath + "\\";
                        textBoxMMSDatabase.Text = _MMSDatabase.Replace(textBoxWorkspace.Text, "");
                        break;
                    case "Priority Cost":
                        textBoxPREFSRiparianCost.Text = dr[1].ToString();
                        break;
                    case "Syncing DB":
                        textBoxSyncingDB.Text = dr[1].ToString();
                        break;
                    case "Control File":
                        textBoxControlFile.Text = dr[1].ToString();
                        break;
                    default:
                        break;
                }
            }
            textBoxMMSDatabase.Text = _MMSDatabase.Replace(textBoxWorkspace.Text, "");
            loading = false;
        }

        private void ClearPrefsText()
        {
            textBoxControlFile.Text = "";
            textBoxSyncingDB.Text = "";
            textBoxPREFSRiparianCost.Text = "";
            textBoxWorkspace.Text = "";
            textBoxMMSDatabase.Text = "";
            textBoxMODSIMFile.Text = "";
        }

        private void PrintMessage(string msg)
        {
            if (messageOut != null)
                messageOut.Invoke(msg);
        }

        private void textBoxProjectDB_TextChanged(object sender, EventArgs e)
        {
            if (!loading)
            {
                if (!_MMSDatabase.StartsWith(textBoxWorkspace.Text))
                {
                    MessageBox.Show("The selected path is not in the MMS database. Select a database parent directory of the MMS database or create a new MMS project/database in a different location.", "Path Error", MessageBoxButtons.OK);
                    return;
                }
                hasChanges = true;
                //find the relative path for workspace 
                //Uri MMSDB = new Uri(_MMSDatabase);
                UpdatePreferences("Workspace", textBoxWorkspace.Text);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Modsim File (*.xy)|*.xy|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    textBoxMODSIMFile.Text = Uri.UnescapeDataString(dlg.FileName);
                    if (textBoxMODSIMFile.Text.Contains(textBoxWorkspace.Text))
                        textBoxMODSIMFile.Text = textBoxMODSIMFile.Text.Replace(textBoxWorkspace.Text, "");
                    else
                    {
                        MessageBox.Show("File not found in the workspace. Please move the file to the specified workspace.");
                        textBoxMODSIMFile.Text = "";
                    }
                }
                textBoxMODSIMFile.Text= textBoxMODSIMFile.Text.StartsWith("\\") ? textBoxMODSIMFile.Text.Substring(1) : textBoxMODSIMFile.Text;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            // Show the FolderBrowserDialog.
            folderBrowserDialog1 = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog1.ShowDialog();
            if (result == DialogResult.OK)
            {
                if (!_MMSDatabase.StartsWith(folderBrowserDialog1.SelectedPath))
                {
                    MessageBox.Show("The selected path is not in the MMS database. Select a database parent directory of the MMS database or create a new MMS project/database in a different location.", "Path Error", MessageBoxButtons.OK);
                    return;
                }
                textBoxWorkspace.Text = folderBrowserDialog1.SelectedPath + "\\";
                UpdatePreferences("Workspace", textBoxWorkspace.Text);
            }

        }

        private void textBoxMODSIMFile_TextChanged(object sender, EventArgs e)
        {
            hasChanges = true;
            UpdatePreferences("MODSIM File", textBoxMODSIMFile.Text);
            
        }

        private void UpdatePreferences(string v, string text)
        {
            if (!loading)
            {
                string unsText = Uri.UnescapeDataString(text);
                if (MMSPrefs != null)
                {
                    if (MMSPrefs.ContainsKey(v))
                        MMSPrefs[v][1] = unsText;
                    else
                    {
                        DataRow dr = prefsTbl.Rows.Add(new object[] { v, unsText });
                        MMSPrefs.Add(v, dr);
                    }
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "WaterALLOC Database File (*.waprj)|*.waprj|SQLite Database File (*.sqlite)|*.sqlite|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    textBoxSyncingDB.Text = Uri.UnescapeDataString(dlg.FileName);
                    if (textBoxSyncingDB.Text.Contains(textBoxWorkspace.Text))
                        textBoxSyncingDB.Text = textBoxSyncingDB.Text.Replace(textBoxWorkspace.Text, "");
                    else
                    {
                        MessageBox.Show("File not found in the workspace. Please move the file to the specified workspace.");
                        textBoxSyncingDB.Text = "";
                    }
                    textBoxSyncingDB.Text = textBoxSyncingDB.Text.StartsWith("\\") ? textBoxSyncingDB.Text.Substring(1) : textBoxSyncingDB.Text;
                }
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Control File (*.control)|*.control|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    textBoxControlFile.Text = Uri.UnescapeDataString(dlg.FileName);
                    if (textBoxControlFile.Text.Contains(textBoxWorkspace.Text))
                        textBoxControlFile.Text = textBoxControlFile.Text.Replace(textBoxWorkspace.Text, "");
                    else
                    {
                        MessageBox.Show("File not found in the workspace. Please move the file to the specified workspace.");
                        textBoxControlFile.Text = "";
                    }
                    textBoxControlFile.Text = textBoxControlFile.Text.StartsWith("\\") ? textBoxControlFile.Text.Substring(1) : textBoxControlFile.Text;
                }
            }
        }

        private void textBoxPREFSRiparianCost_TextChanged(object sender, EventArgs e)
        {
            hasChanges = true;
            UpdatePreferences("Priority Cost", textBoxPREFSRiparianCost.Text);
        }

        private void textBoxSyncingDB_TextChanged(object sender, EventArgs e)
        {
            hasChanges = true;
            UpdatePreferences("Syncing DB", textBoxSyncingDB.Text);
        }

        private void textBoxControlFile_TextChanged(object sender, EventArgs e)
        {
            hasChanges = true;
            UpdatePreferences("Control File", textBoxControlFile.Text);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "Pumping File (*.wel)|*.wel|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    textBoxPumpingFile.Text = Uri.UnescapeDataString(dlg.FileName);
                    if (textBoxPumpingFile.Text.Contains(textBoxWorkspace.Text))
                        textBoxPumpingFile.Text = textBoxPumpingFile.Text.Replace(textBoxWorkspace.Text, "");
                    else
                    {
                        MessageBox.Show("File not found in the workspace. Please move the file to the specified workspace.");
                        textBoxPumpingFile.Text = "";
                    }
                    textBoxPumpingFile.Text = textBoxPumpingFile.Text.StartsWith("\\") ? textBoxPumpingFile.Text.Substring(1) : textBoxPumpingFile.Text;
                    rutaPumping = textBoxPumpingFile.Text;
                }
            }
        }

        private void textBoxPumpingFile_TextChanged(object sender, EventArgs e)
        {
            hasChanges = true;
            UpdatePreferences("Pumping File", textBoxPumpingFile.Text);
        }
    }
}
