using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RRModelingSystem
{
    public partial class SimulationRun : UserControl
    {
        private string _fileName;
        private Stopwatch sw ;
        private List<string> errorLines,maxLines;
        public SimulationRun(int runID, string fileName, List<string> runMgs)
        {
            InitializeComponent();
            _fileName = fileName;
            textBoxHeading.Text = $"Simulation Run Monitoring | Run: {runID}";
            sw = Stopwatch.StartNew();
            
            if (runMgs!=null)
            {
                richTextBox1.Lines = runMgs.ToArray();
                buttonUpdate.Enabled = false;
                toolStripStatusLabel1.Text = "Simulation completed.";
                sw.Stop();
                
            }
        }

        private void SimulationRun_Load(object sender, EventArgs e)
        {
            AnalyzeMsgs();
            if (_fileName != "")
            {
                var item = listView1.Items.Add("Log File:");
                item.SubItems.Add(_fileName);
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
                if(line.ToLower().Contains("elapsed"))
                {
                    var item = listView1.FindItemWithText("Elapsed:");
                    if (item == null)
                    {
                        item = listView1.Items.Add("Elapsed:");
                        item.SubItems.Add(line);
                    }
                    else
                        item.SubItems[0].Text = line;
                }
                if (line.Contains("percent done"))
                {
                    toolStripProgressBar1.Value = Convert.ToInt32(line.Replace("percent done ", ""));
                }
                if (line.StartsWith("Done"))
                    toolStripProgressBar1.Value = 0;
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
            if (treeViewMsgGroup.SelectedNode == treeViewMsgGroup.Nodes["NodeError"])
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

        

        private void UpdateTxtFile()
        {
            FileStream fs = new FileStream(_fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite,4096,FileOptions.SequentialScan);
            using (StreamReader sr = new StreamReader(fs))
            {
                richTextBox1.Text = sr.ReadToEnd();
                sr.Close();
            }

            //sw.Stop();
            listView1.Items.Add(new ListViewItem(new string[] { "Time elapsed : ", sw.Elapsed.TotalSeconds.ToString() + " sec." }));

            AnalyzeMsgs();
        }
    }
}
