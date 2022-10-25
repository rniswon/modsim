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
        public event ProcessMessage MessageOut; // event
        private string _workSpace;

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

            txtOutputDB.Text = currentRow.Cells["BasePath"].Value.ToString();
            txtOutputDB.Text = txtOutputDB.Text.Replace(".xy", "OUTPUT.sqlite");
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

        }

        private void btnAdaptDB_Click(object sender, EventArgs e)
        {
            if (txtOutputDB.Text.Trim() == "")
            {
                MessageOut("Select a SQLite database output with the 'Browse DB' button or select a row of text box.");
                btnBrowseDB.Focus();
            }
            else {
                string strConn, nomArchivo;
                SQLiteDataReader prefsTbl1;
                strConn = string.Format("Data Source={0};Version={1}", _workSpace + txtOutputDB.Text, 3);
                nomArchivo = Path.GetFileName(string.Format(_workSpace + txtOutputDB.Text));
                nomArchivo = nomArchivo.Substring(0, nomArchivo.Length - 13);

                string sql = @"SELECT name FROM sqlite_master WHERE type = 'table' ORDER BY 1;";
                string sql1 = "";
                using (SQLiteConnection c = new SQLiteConnection(strConn))
                {
                    c.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(sql, c))
                    {
                        prefsTbl1 = cmd.ExecuteReader();
                        while (prefsTbl1.Read())
                        {
                            sql1 = sql1 + @"ALTER TABLE " + prefsTbl1.GetString(0) + " ADD scenario VARCHAR(20) NULL;\n";
                            sql1 = sql1 + @"UPDATE " + prefsTbl1.GetString(0) + " SET scenario = '" + nomArchivo + "';\n";
                        }
                    }
                    c.Close();
                }

                using (SQLiteConnection c = new SQLiteConnection(strConn))
                {
                    c.Open();
                    using (SQLiteCommand cmd = new SQLiteCommand(sql1, c))
                    {
                        cmd.ExecuteNonQuery();
                    }
                }
                MessageOut("Updated SQLite database output.");
            }
        }
    }
}
