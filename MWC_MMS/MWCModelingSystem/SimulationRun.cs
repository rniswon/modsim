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
        public SimulationRun(int runID, string fileName)
        {
            InitializeComponent();
            _fileName = fileName;
            textBoxHeading.Text = $"Simulation Run Monitoring | Run: {runID}";
            sw = Stopwatch.StartNew();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            UpdateTxtFile();
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
        }
    }
}
