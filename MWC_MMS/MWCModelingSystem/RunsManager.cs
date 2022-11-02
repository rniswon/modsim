using RTI.CWR.MWC_MODSIMUtils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data.SQLite;
using System.IO;

namespace RRModelingSystem
{
    public partial class RunsManager : UserControl
    {
        private MyDBSqlite sqliteDB { get; set; }
        private MyDBSqlite outputSqliteDB { get; set; }
        public event ProcessMessage MessageOut; // event
        private string _workSpace;
        private string _fileName;
        private string runID;
        private string scenarioName;

        public RunsManager(string MMS_db, string workSpace)
        {
            InitializeComponent();
            sqliteDB = new MyDBSqlite(Path.Combine(workSpace,MMS_db));
            _workSpace = workSpace;
        }

        private void RunsManager_Load(object sender, EventArgs e)
        {
            string sql = "SELECT * FROM MMS_RunsInfo ORDER BY runID";
            DataTable dtRuns = sqliteDB.GetTableFromDB(sql, "keywords");
            dataGridView1.DataSource = dtRuns;
            //splitContainer1.Panel2Collapsed = true;
        }

        public void ReLoadForm()
        {
            RunsManager_Load(null, null);
        }
        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            //UpdateInfo(((DataGridView)sender).CurrentRow);
            
        }

        private void UpdateInfo(DataGridViewRow currentRow)
        {
            textBoxHeading.Text = "Preferences Run ID: " + currentRow.Cells["runID"].Value.ToString();
            richTextBox1.Text = currentRow.Cells["BasePath"].Value.ToString();
            richTextBox1.AppendText("\n__________Options____________\n");
            richTextBox1.AppendText(currentRow.Cells["Options"].Value.ToString() + "\n");
            richTextBox1.AppendText("\n__________Notes____________\n");
            richTextBox1.AppendText(currentRow.Cells["Notes"].Value.ToString());
            txtOutputRunIDDB.Text = currentRow.Cells["ModsimFile"].Value.ToString();
            txtOutputRunIDDB.Text = txtOutputRunIDDB.Text.Replace(".xy", "OUTPUT.sqlite");
            runID = currentRow.Cells["runID"].Value.ToString();
            scenarioName = currentRow.Cells["ScnName"].Value.ToString();
            //fileName = txtOutputRunIDDB.Text.Replace("OUTPUT.sqlite","");

            /*string pathDB = string.Format(_workSpace + txtOutputDB.Text);
            if (File.Exists(pathDB))
            {
                btnAdaptDB.Enabled = true;
            }
            else
            {
                btnAdaptDB.Enabled = false;
                //MessageOut("SQLITE File does not exist");
            }*/
        }

        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            
        }

        private void dataGridView1_Validated(object sender, EventArgs e)
        {
            
        }

        private void dataGridView1_RowValidated(object sender, DataGridViewCellEventArgs e)
        {
           
        }

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (dataGridView1.SelectedRows.Count > 0)
                UpdateInfo(((DataGridView)sender).CurrentRow);
        }

        private void btnBrowseDB_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Filter = "MODSIM Output File (*.sqlite)|*.sqlite";
                dlg.RestoreDirectory = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtOutputDB.Text = Uri.UnescapeDataString(dlg.FileName);
                    txtOutputDB.Text = txtOutputDB.Text.Replace(_workSpace,"");
                }
            }
            btnAdaptDB.Enabled = true;

        }

        private void btnAdaptDB_Click(object sender, EventArgs e)
        {
            if(checkBoxOutputDB.Checked==true)
            {
                if(txtOutputDB.Text.Trim()=="")
                {
                    MessageOut("Select a SQLite database output with the 'Browse DB'.");
                }
                else
                {
                    string strConn1, nomArchivo;
                    strConn1 = string.Format("Data Source={0};Version={1}", _workSpace + txtOutputDB.Text, 3);
                    nomArchivo = Path.GetFileName(string.Format(_workSpace + txtOutputDB.Text));
                    nomArchivo = nomArchivo.Substring(0, nomArchivo.Length - 13);
                    AdaptDB(strConn1, nomArchivo);
                }
            }
            else
            {
                string strConn1;
                strConn1 = string.Format("Data Source={0};Version={1}", _workSpace + txtOutputRunIDDB.Text, 3);
                AdaptDB(strConn1, _fileName);
                try
                {
                    string sql2;
                    sql2 = @"UPDATE [MMS_RunsInfo] SET OutputDBScenario = 1 WHERE runID LIKE '" + runID + "';";
                    sqliteDB.ExecuteQuery(sql2);
                }
                catch (Exception ex)
                {
                    MessageOut(ex.Message);
                }
                //MessageOut("Updated SQLite database output.");
            }
        }

        private void AdaptDB (string strConn, string filenames)
        {
            SQLiteDataReader prefsTbl;
            string sql = @"SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY 1;";
            string sql1 = "";
            using (SQLiteConnection c = new SQLiteConnection(strConn))
            {
                c.Open();
                using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                {
                    prefsTbl = cmd.ExecuteReader();
                    while (prefsTbl.Read())
                    {
                        sql1 = "";
                        try
                        {
                            sql1 = sql1 + @"SELECT Scenario FROM " + prefsTbl.GetString(0) + " where 1=2;";
                            using (SQLiteCommand cmd1 = new SQLiteCommand(sql1, c))
                            {
                                cmd1.ExecuteNonQuery();
                            }
                            MessageOut("Updated field 'scenario' in table " + prefsTbl.GetString(0) + " of " + filenames + "OUTPUT.sqlite.");
                        }
                        catch (Exception ex) //catch block for catching errors
                        {
                            sql1 = "";
                            sql1 = sql1 + @"ALTER TABLE " + prefsTbl.GetString(0) + " ADD scenario TEXT NULL;";
                            using (SQLiteCommand cmd2 = new SQLiteCommand(sql1, c))
                            {
                                cmd2.ExecuteNonQuery();
                            }
                            MessageOut("Added field 'Scenario' in table " + prefsTbl.GetString(0) + " of " + filenames + "OUTPUT.sqlite.");
                        }
                        sql1 = "";
                        sql1 = sql1 + @"UPDATE " + prefsTbl.GetString(0) + " SET Scenario = '" + filenames + "';\n";
                        using (SQLiteCommand cmd3 = new SQLiteCommand(sql1, c))
                        {
                            cmd3.ExecuteNonQuery();
                        }
                    }
                }
                c.Close();
            }
        }
        private void ProcessFileName()
        {
            if (checkBoxFileName.Checked == true)
            {
                _fileName = Path.GetFileName(txtOutputRunIDDB.Text).Replace("OUTPUT.sqlite", "");
                labelScnName.Text = "Scenario Name: " + _fileName;
            }
            else
            {
                _fileName = "";
                labelScnName.Text = "Scenario Name: ";
            }
            if(checkBoxRunID.Checked == true)
            {
                _fileName = _fileName + string.Format("r" + runID);
                labelScnName.Text = "Scenario Name: " + _fileName;
            }
            if(checkBoxScenario.Checked == true)
            {
                _fileName = _fileName + scenarioName;
                labelScnName.Text = "Scenario Name: " + _fileName;
            }
        }

        private void checkBoxRunID_CheckedChanged(object sender, EventArgs e)
        {
            ProcessFileName();
        }

        private void checkBoxScenario_CheckedChanged(object sender, EventArgs e)
        {
            ProcessFileName();
        }

        private void checkBoxFileName_CheckedChanged(object sender, EventArgs e)
        {
            ProcessFileName();
        }

        private void checkBoxOutputDB_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxOutputDB.Checked)
            {
                btnBrowseDB.Enabled = true;
                txtOutputRunIDDB.Enabled = false;
                checkBoxRunID.Enabled = false;
                checkBoxScenario.Enabled = false;
                checkBoxFileName.Enabled = false;
            }
            else
            {
                btnBrowseDB.Enabled = false;
                txtOutputRunIDDB.Enabled = true;
                checkBoxRunID.Enabled = true;
                checkBoxScenario.Enabled = true;
                checkBoxFileName.Enabled = true;
            }
        }
    }
}
