using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using RTI.CWR.MWC_MODSIMUtils;

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
        public bool loading = true;
        public static bool rutaPumping = false;
        

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
            //textBoxWorkspace.Text = Path.GetDirectoryName(_MMSDatabase) + "\\";
            
            prefsTbl = m_DBUtils.GetTableFromDB("SELECT * FROM MMS_Preferences", "MMS_Preferences");

            string _baseMMSDatabase = "";
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
                        string fullPath = dr[1].ToString();// Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_MMSDatabase), dr[1].ToString()));
                        textBoxWorkspace.Text = fullPath.EndsWith("\\")?fullPath:fullPath + "\\";
                        Directory.SetCurrentDirectory(textBoxWorkspace.Text);
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
                    case "Pumping File":
                        textBoxPumpingFile.Text = dr[1].ToString();
                        break;
                    case "MMSDatabase":
                        _baseMMSDatabase = dr[1].ToString();
                        break;
                    default:
                        break;
                }
            }

            //Check for paths change
            loading = false;
            textBoxMMSDatabase.Text = _MMSDatabase.Replace(textBoxWorkspace.Text, "");
            if (Path.IsPathRooted(textBoxMMSDatabase.Text))
            {
                string[] newWorkspace = CommonString(Path.GetDirectoryName(Path.Combine(textBoxWorkspace.Text, _baseMMSDatabase)), Path.GetDirectoryName(_MMSDatabase));
                if (newWorkspace.Length > 0 && newWorkspace[0].Length > 0)
                {

                    textBoxWorkspace.Text = Path.Combine(newWorkspace[0], newWorkspace[1]).Replace(Path.GetDirectoryName(_baseMMSDatabase), "");
                    messageOut($"Found a new workspace path.  Updating the path to: {textBoxWorkspace.Text}");
                    textBoxMMSDatabase.Text = _baseMMSDatabase;
                    _baseMMSDatabase = Path.Combine(textBoxWorkspace.Text, textBoxMMSDatabase.Text);
                }
            }
            if (_baseMMSDatabase!="" && Path.GetFileName(_baseMMSDatabase) != Path.GetFileName(_MMSDatabase))
            {
                messageOut("WARNING [Database name] Change detected preferences updated");
                textBoxMMSDatabase.Text = textBoxMMSDatabase.Text.Replace(Path.GetFileName(_baseMMSDatabase), Path.GetFileName(_MMSDatabase));
                
            }
            if (Path.Combine(textBoxWorkspace.Text, textBoxMMSDatabase.Text) != _MMSDatabase)
            {
                messageOut("ERROR [Procesing workspace] check and correct project paths.");
            }
            CheckFilesExist();

           
        }

        private void CheckFilesExist(string oldWorspace = "")
        {
            ProcessExistFile(textBoxMODSIMFile, "MODSIM", oldWorspace);
            ProcessExistFile(textBoxSyncingDB, "Syncing DB", oldWorspace);
            ProcessExistFile(textBoxPumpingFile, "Pumping", oldWorspace);
            ProcessExistFile(textBoxControlFile, "Control", oldWorspace);
            if(oldWorspace!="")
            {
                textBoxMMSDatabase.Text = _MMSDatabase.Replace(textBoxWorkspace.Text, "");
            }
        }

        private void ProcessExistFile(TextBox textBox, string v, string oldWorspace)
        {
            if (textBox.Text != "")
            {
                string filePath = Path.Combine(textBoxWorkspace.Text, textBox.Text);
                if (oldWorspace != "")
                {
                    Uri oldUri = new Uri(oldWorspace);
                    Uri newUri = new Uri(textBoxWorkspace.Text);
                    textBox.Text = newUri.MakeRelativeUri(oldUri).ToString() + textBox.Text;
                    textBox.Text = Path.Combine(Path.GetDirectoryName(textBox.Text), Path.GetFileName(textBox.Text));
                    if(!textBox.Text.StartsWith(".."))
                        textBox.Text = Path.GetFullPath(Path.Combine(textBoxWorkspace.Text, textBox.Text)).Replace(textBoxWorkspace.Text, "");
                    //if (textBoxWorkspace.Text.Length < oldWorspace.Length)
                    //{
                    //    newWorkspace = CommonString(textBoxWorkspace.Text, oldWorspace);
                    //    textBox.Text = newWorkspace[0] + textBox.Text;
                    //    textBox.Text = Path.Combine(Path.GetDirectoryName(textBox.Text), Path.GetFileName(textBox.Text));
                    //    textBox.Text = Path.GetFullPath(Path.Combine(textBoxWorkspace.Text, textBox.Text)).Replace(textBoxWorkspace.Text,"");

                    //}
                    //else
                    //{
                    //    textBox.Text = newUri.MakeRelativeUri(oldUri).ToString() + textBox.Text;
                    //    textBox.Text = Path.Combine(Path.GetDirectoryName(textBox.Text), Path.GetFileName(textBox.Text));
                    //    //newWorkspace = CommonString( oldWorspace, textBoxWorkspace.Text);
                    //    //string[] folders = newWorkspace[0].Split(new char[] { '\\' },StringSplitOptions.RemoveEmptyEntries);
                    //    //foreach(string s in folders)
                    //    //{
                    //    //    if (textBox.Text.StartsWith(s))
                    //    //        textBox.Text = textBox.Text.Replace(s+"\\", "");
                    //    //    if (textBox.Text.Contains(s))
                    //    //        textBox.Text = textBox.Text.Replace(s, "..");
                    //    //}
                    //    ////textBox.Text = Path.GetFullPath(textBox.Text);
                    //}
                }
                filePath = Path.Combine(textBoxWorkspace.Text, textBox.Text);

                if (!File.Exists(filePath))
                {
                    messageOut($"\t ERROR [missing file] {v} file {filePath} does not exist.");
                    textBox.BackColor = Color.Pink;
                }
                else
                {
                    textBox.BackColor = System.Drawing.SystemColors.Window;
                    if (oldWorspace != "")
                    {
                        //update location

                    }
                }
                    
            }
        }

        public string[] CommonString(string left, string right)
        {
            List<string> result = new List<string>();

            for (int i = 0; i < left.Length; i++)
            {
                if (right.Contains(left.Substring(i)) && !left.Substring(i).StartsWith("\\"))
                {
                    result.Add(right.Replace(left.Substring(i), ""));
                    result.Add(left.Substring(i));
                    
                    break;
                }
            }

            return result.Distinct().ToArray();
        }

        private void ClearPrefsText()
        {
            textBoxControlFile.Text = "";
            textBoxSyncingDB.Text = "";
            textBoxPREFSRiparianCost.Text = "-48888";
            textBoxWorkspace.Text = "";
            textBoxMMSDatabase.Text = "";
            textBoxMODSIMFile.Text = "";
            textBoxPumpingFile.Text = "";
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
                Directory.SetCurrentDirectory(textBoxWorkspace.Text);
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
                CheckFilesExist();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            // Show the FolderBrowserDialog.
            folderBrowserDialog1 = new FolderBrowserDialog();
            DialogResult result = folderBrowserDialog1.ShowDialog();
            string oldWorspace = textBoxWorkspace.Text;
            if (result == DialogResult.OK)
            {
                if (!_MMSDatabase.StartsWith(folderBrowserDialog1.SelectedPath))
                {
                    MessageBox.Show("The selected path is not in the MMS database. Select a database parent directory of the MMS database or create a new MMS project/database in a different location.", "Path Error", MessageBoxButtons.OK);
                    return;
                }
                textBoxWorkspace.Text = folderBrowserDialog1.SelectedPath + "\\";
                //UpdatePreferences("Workspace", textBoxWorkspace.Text);

                CheckFilesExist(oldWorspace);
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
                    CheckFilesExist();
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
                    CheckFilesExist();
                }
            }
        }

        private void textBoxPREFSRiparianCost_TextChanged(object sender, EventArgs e)
        {
            if(!loading)
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
                    CheckFilesExist();
                }
            }
            if (textBoxPumpingFile.Text.Trim() == "")
            {
                rutaPumping = false;
            }
            else
            {
                rutaPumping = true;
            }
        }

        private void textBoxPumpingFile_TextChanged(object sender, EventArgs e)
        {
            hasChanges = true;
            UpdatePreferences("Pumping File", textBoxPumpingFile.Text);
        }

        private void textBoxMMSDatabase_TextChanged(object sender, EventArgs e)
        {
            hasChanges = true;
            UpdatePreferences("MMSDatabase", textBoxMMSDatabase.Text);
        }
    }
}
