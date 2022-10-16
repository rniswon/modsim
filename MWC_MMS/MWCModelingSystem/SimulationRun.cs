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
            if(runMgs!=null)
            {
                richTextBox1.Lines = runMgs.ToArray();
                buttonUpdate.Enabled = false;
                toolStripStatusLabel1.Text = "Simulation completed.";
                sw.Stop();
                AnalyzeMsgs();
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
            if (treeViewMsgGroup.SelectedNode == treeViewMsgGroup.Nodes["NodeError"])
                richTextBox2.Lines = errorLines.ToArray();
            if (treeViewMsgGroup.SelectedNode == treeViewMsgGroup.Nodes["NodeConvergence"])
                richTextBox2.Lines = maxLines.ToArray();
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
            listView1.Items[1].SubItems[2].Text = "Time elapsed : " + sw.Elapsed.TotalSeconds.ToString() + " sec.";

            AnalyzeMsgs();
        }
    }
}
