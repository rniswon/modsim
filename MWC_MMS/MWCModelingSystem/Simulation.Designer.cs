namespace RRModelingSystem
{
    partial class Simulation
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonExecuteModel = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxScnName = new System.Windows.Forms.TextBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.labelISFlinks = new System.Windows.Forms.Label();
            this.labelRiparian = new System.Windows.Forms.Label();
            this.labelCostBlock = new System.Windows.Forms.Label();
            this.buttonNodeDW = new System.Windows.Forms.Button();
            this.buttonNodeUP = new System.Windows.Forms.Button();
            this.treeViewPsdoCost = new System.Windows.Forms.TreeView();
            this.checkBoxOthersON = new System.Windows.Forms.CheckBox();
            this.checkBoxAgON = new System.Windows.Forms.CheckBox();
            this.checkBoxIndoorDomON = new System.Windows.Forms.CheckBox();
            this.checkBoxOutdoorDomON = new System.Windows.Forms.CheckBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.comboBox5 = new System.Windows.Forms.ComboBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.groupBox7 = new System.Windows.Forms.GroupBox();
            this.dataGridViewISF = new System.Windows.Forms.DataGridView();
            this.groupBox4 = new System.Windows.Forms.GroupBox();
            this.txtFactor = new System.Windows.Forms.TextBox();
            this.checkFactor = new System.Windows.Forms.CheckBox();
            this.groupBox9 = new System.Windows.Forms.GroupBox();
            this.groupBoxRunInfo = new System.Windows.Forms.GroupBox();
            this.checkBoxUseInName = new System.Windows.Forms.CheckBox();
            this.label2 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.richTextBoxRunNotes = new System.Windows.Forms.RichTextBox();
            this.comboBoxKeyword = new System.Windows.Forms.ComboBox();
            this.radioButtonMMSRun = new System.Windows.Forms.RadioButton();
            this.radioButtonRunActive = new System.Windows.Forms.RadioButton();
            this.checkBoxRiparianLogic = new System.Windows.Forms.CheckBox();
            this.groupBox6 = new System.Windows.Forms.GroupBox();
            this.radioButtonAgPckge = new System.Windows.Forms.RadioButton();
            this.radioButtonWRIMS = new System.Windows.Forms.RadioButton();
            this.groupBox5 = new System.Windows.Forms.GroupBox();
            this.radioButtonMS_GS = new System.Windows.Forms.RadioButton();
            this.radioButtonMODSIMOnly = new System.Windows.Forms.RadioButton();
            this.comboBoxMODSIMFile = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.pictureBoxStatus = new System.Windows.Forms.PictureBox();
            this.labelActFile = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.groupBox7.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewISF)).BeginInit();
            this.groupBox4.SuspendLayout();
            this.groupBox9.SuspendLayout();
            this.groupBoxRunInfo.SuspendLayout();
            this.groupBox6.SuspendLayout();
            this.groupBox5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStatus)).BeginInit();
            this.SuspendLayout();
            // 
            // buttonExecuteModel
            // 
            this.buttonExecuteModel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonExecuteModel.Enabled = false;
            this.buttonExecuteModel.Location = new System.Drawing.Point(490, 169);
            this.buttonExecuteModel.Name = "buttonExecuteModel";
            this.buttonExecuteModel.Size = new System.Drawing.Size(98, 23);
            this.buttonExecuteModel.TabIndex = 6;
            this.buttonExecuteModel.Text = "Run Simulation";
            this.buttonExecuteModel.UseVisualStyleBackColor = true;
            this.buttonExecuteModel.Click += new System.EventHandler(this.buttonImportTS_Click);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(15, 16);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(80, 13);
            this.label1.TabIndex = 7;
            this.label1.Text = "Scenario Name";
            // 
            // textBoxScnName
            // 
            this.textBoxScnName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxScnName.Location = new System.Drawing.Point(101, 10);
            this.textBoxScnName.Name = "textBoxScnName";
            this.textBoxScnName.Size = new System.Drawing.Size(204, 20);
            this.textBoxScnName.TabIndex = 8;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.labelISFlinks);
            this.groupBox1.Controls.Add(this.labelRiparian);
            this.groupBox1.Controls.Add(this.labelCostBlock);
            this.groupBox1.Controls.Add(this.buttonNodeDW);
            this.groupBox1.Controls.Add(this.buttonNodeUP);
            this.groupBox1.Controls.Add(this.treeViewPsdoCost);
            this.groupBox1.Controls.Add(this.checkBoxOthersON);
            this.groupBox1.Controls.Add(this.checkBoxAgON);
            this.groupBox1.Controls.Add(this.checkBoxIndoorDomON);
            this.groupBox1.Controls.Add(this.checkBoxOutdoorDomON);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.ForeColor = System.Drawing.SystemColors.ControlText;
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(378, 184);
            this.groupBox1.TabIndex = 9;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Allocation Pseudo-Priorities";
            this.groupBox1.Enter += new System.EventHandler(this.groupBox1_Enter);
            // 
            // labelISFlinks
            // 
            this.labelISFlinks.AutoSize = true;
            this.labelISFlinks.Location = new System.Drawing.Point(30, 42);
            this.labelISFlinks.Name = "labelISFlinks";
            this.labelISFlinks.Size = new System.Drawing.Size(104, 13);
            this.labelISFlinks.TabIndex = 19;
            this.labelISFlinks.Text = "Instream flow targets";
            // 
            // labelRiparian
            // 
            this.labelRiparian.AutoSize = true;
            this.labelRiparian.Location = new System.Drawing.Point(30, 12);
            this.labelRiparian.Name = "labelRiparian";
            this.labelRiparian.Size = new System.Drawing.Size(100, 13);
            this.labelRiparian.TabIndex = 18;
            this.labelRiparian.Text = "Riparian rights cost:";
            // 
            // labelCostBlock
            // 
            this.labelCostBlock.AutoSize = true;
            this.labelCostBlock.Location = new System.Drawing.Point(30, 27);
            this.labelCostBlock.Name = "labelCostBlock";
            this.labelCostBlock.Size = new System.Drawing.Size(147, 13);
            this.labelCostBlock.TabIndex = 17;
            this.labelCostBlock.Text = "Normal water right cost block:";
            // 
            // buttonNodeDW
            // 
            this.buttonNodeDW.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonNodeDW.Location = new System.Drawing.Point(295, 84);
            this.buttonNodeDW.Name = "buttonNodeDW";
            this.buttonNodeDW.Size = new System.Drawing.Size(75, 23);
            this.buttonNodeDW.TabIndex = 16;
            this.buttonNodeDW.Text = "Move Down";
            this.buttonNodeDW.UseVisualStyleBackColor = true;
            this.buttonNodeDW.Click += new System.EventHandler(this.buttonNodeDW_Click);
            // 
            // buttonNodeUP
            // 
            this.buttonNodeUP.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.buttonNodeUP.Location = new System.Drawing.Point(295, 57);
            this.buttonNodeUP.Name = "buttonNodeUP";
            this.buttonNodeUP.Size = new System.Drawing.Size(75, 23);
            this.buttonNodeUP.TabIndex = 15;
            this.buttonNodeUP.Text = "Move Up";
            this.buttonNodeUP.UseVisualStyleBackColor = true;
            this.buttonNodeUP.Click += new System.EventHandler(this.buttonNodeUP_Click);
            // 
            // treeViewPsdoCost
            // 
            this.treeViewPsdoCost.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.treeViewPsdoCost.Location = new System.Drawing.Point(87, 57);
            this.treeViewPsdoCost.Name = "treeViewPsdoCost";
            this.treeViewPsdoCost.Size = new System.Drawing.Size(202, 124);
            this.treeViewPsdoCost.TabIndex = 14;
            this.treeViewPsdoCost.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeViewPsdoCost_AfterSelect);
            // 
            // checkBoxOthersON
            // 
            this.checkBoxOthersON.AutoSize = true;
            this.checkBoxOthersON.Location = new System.Drawing.Point(9, 144);
            this.checkBoxOthersON.Name = "checkBoxOthersON";
            this.checkBoxOthersON.Size = new System.Drawing.Size(52, 17);
            this.checkBoxOthersON.TabIndex = 11;
            this.checkBoxOthersON.Text = "Other";
            this.checkBoxOthersON.UseVisualStyleBackColor = true;
            this.checkBoxOthersON.CheckedChanged += new System.EventHandler(this.checkBoxOthersON_CheckedChanged);
            // 
            // checkBoxAgON
            // 
            this.checkBoxAgON.AutoSize = true;
            this.checkBoxAgON.Location = new System.Drawing.Point(8, 120);
            this.checkBoxAgON.Name = "checkBoxAgON";
            this.checkBoxAgON.Size = new System.Drawing.Size(76, 17);
            this.checkBoxAgON.TabIndex = 10;
            this.checkBoxAgON.Text = "Agriculture";
            this.checkBoxAgON.UseVisualStyleBackColor = true;
            this.checkBoxAgON.CheckedChanged += new System.EventHandler(this.checkBoxAgON_CheckedChanged);
            // 
            // checkBoxIndoorDomON
            // 
            this.checkBoxIndoorDomON.Location = new System.Drawing.Point(8, 60);
            this.checkBoxIndoorDomON.Name = "checkBoxIndoorDomON";
            this.checkBoxIndoorDomON.Size = new System.Drawing.Size(76, 32);
            this.checkBoxIndoorDomON.TabIndex = 8;
            this.checkBoxIndoorDomON.Text = "Indoor Domestic";
            this.checkBoxIndoorDomON.UseVisualStyleBackColor = true;
            this.checkBoxIndoorDomON.CheckedChanged += new System.EventHandler(this.checkBoxIndoorDomON_CheckedChanged);
            // 
            // checkBoxOutdoorDomON
            // 
            this.checkBoxOutdoorDomON.Location = new System.Drawing.Point(8, 88);
            this.checkBoxOutdoorDomON.Name = "checkBoxOutdoorDomON";
            this.checkBoxOutdoorDomON.Size = new System.Drawing.Size(76, 32);
            this.checkBoxOutdoorDomON.TabIndex = 9;
            this.checkBoxOutdoorDomON.Text = "Outdoor Domestic";
            this.checkBoxOutdoorDomON.UseVisualStyleBackColor = true;
            this.checkBoxOutdoorDomON.CheckedChanged += new System.EventHandler(this.checkBoxOutdoorDomON_CheckedChanged);
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Controls.Add(this.txtFactor);
            this.groupBox2.Controls.Add(this.comboBox5);
            this.groupBox2.Controls.Add(this.checkFactor);
            this.groupBox2.Location = new System.Drawing.Point(9, 213);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(633, 44);
            this.groupBox2.TabIndex = 10;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "GSFLOW Pumping Scenario";
            // 
            // comboBox5
            // 
            this.comboBox5.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox5.Enabled = false;
            this.comboBox5.FormattingEnabled = true;
            this.comboBox5.Items.AddRange(new object[] {
            "1.  20% reduction in agricultural groundwater pumping.",
            "2.  20% reduction in municipal and industrial groundwater pumping.",
            "3.  20% reduction in all groundwater pumping.",
            "4.  20% reduction in residential groundwater pumping.",
            "5.  20% reduction in outdoor residential groundwater pumping.",
            "6.  20% reduction in indoor residential groundwater pumping.",
            "7.  20% transfer of agricultural groundwater pumping to residential groundwater p" +
                "umping"});
            this.comboBox5.Location = new System.Drawing.Point(19, 17);
            this.comboBox5.Name = "comboBox5";
            this.comboBox5.Size = new System.Drawing.Size(268, 21);
            this.comboBox5.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox3.Controls.Add(this.splitContainer1);
            this.groupBox3.Controls.Add(this.groupBox2);
            this.groupBox3.ForeColor = System.Drawing.SystemColors.ControlText;
            this.groupBox3.Location = new System.Drawing.Point(3, 61);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(648, 265);
            this.groupBox3.TabIndex = 11;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Management Preferences";
            // 
            // splitContainer1
            // 
            this.splitContainer1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitContainer1.Location = new System.Drawing.Point(7, 21);
            this.splitContainer1.Name = "splitContainer1";
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.groupBox1);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBox7);
            this.splitContainer1.Size = new System.Drawing.Size(635, 188);
            this.splitContainer1.SplitterDistance = 378;
            this.splitContainer1.TabIndex = 15;
            // 
            // groupBox7
            // 
            this.groupBox7.Controls.Add(this.dataGridViewISF);
            this.groupBox7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupBox7.Location = new System.Drawing.Point(0, 0);
            this.groupBox7.Name = "groupBox7";
            this.groupBox7.Size = new System.Drawing.Size(253, 188);
            this.groupBox7.TabIndex = 0;
            this.groupBox7.TabStop = false;
            this.groupBox7.Text = "Instream Flow Targets";
            // 
            // dataGridViewISF
            // 
            this.dataGridViewISF.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridViewISF.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridViewISF.Location = new System.Drawing.Point(3, 16);
            this.dataGridViewISF.Name = "dataGridViewISF";
            this.dataGridViewISF.Size = new System.Drawing.Size(247, 169);
            this.dataGridViewISF.TabIndex = 0;
            // 
            // groupBox4
            // 
            this.groupBox4.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox4.Controls.Add(this.groupBox9);
            this.groupBox4.Controls.Add(this.checkBoxRiparianLogic);
            this.groupBox4.Controls.Add(this.groupBox6);
            this.groupBox4.Controls.Add(this.buttonExecuteModel);
            this.groupBox4.Controls.Add(this.groupBox5);
            this.groupBox4.ForeColor = System.Drawing.SystemColors.ControlText;
            this.groupBox4.Location = new System.Drawing.Point(14, 332);
            this.groupBox4.Name = "groupBox4";
            this.groupBox4.Size = new System.Drawing.Size(631, 339);
            this.groupBox4.TabIndex = 12;
            this.groupBox4.TabStop = false;
            this.groupBox4.Text = "Simulation Preferences";
            this.groupBox4.Enter += new System.EventHandler(this.groupBox4_Enter);
            // 
            // txtFactor
            // 
            this.txtFactor.Location = new System.Drawing.Point(552, 15);
            this.txtFactor.Name = "txtFactor";
            this.txtFactor.Size = new System.Drawing.Size(71, 20);
            this.txtFactor.TabIndex = 0;
            this.txtFactor.Text = "1.0";
            this.txtFactor.TextAlign = System.Windows.Forms.HorizontalAlignment.Right;
            this.txtFactor.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBox1_KeyPress);
            // 
            // checkFactor
            // 
            this.checkFactor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkFactor.AutoSize = true;
            this.checkFactor.Checked = true;
            this.checkFactor.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkFactor.Location = new System.Drawing.Point(351, 17);
            this.checkFactor.Name = "checkFactor";
            this.checkFactor.Size = new System.Drawing.Size(191, 17);
            this.checkFactor.TabIndex = 7;
            this.checkFactor.Text = "Enable Pumping Adjustment Factor";
            this.checkFactor.UseVisualStyleBackColor = true;
            // 
            // groupBox9
            // 
            this.groupBox9.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox9.Controls.Add(this.groupBoxRunInfo);
            this.groupBox9.Controls.Add(this.radioButtonMMSRun);
            this.groupBox9.Controls.Add(this.radioButtonRunActive);
            this.groupBox9.Location = new System.Drawing.Point(6, 15);
            this.groupBox9.Name = "groupBox9";
            this.groupBox9.Size = new System.Drawing.Size(414, 318);
            this.groupBox9.TabIndex = 4;
            this.groupBox9.TabStop = false;
            this.groupBox9.Text = "Run Type";
            // 
            // groupBoxRunInfo
            // 
            this.groupBoxRunInfo.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBoxRunInfo.Controls.Add(this.checkBoxUseInName);
            this.groupBoxRunInfo.Controls.Add(this.label1);
            this.groupBoxRunInfo.Controls.Add(this.textBoxScnName);
            this.groupBoxRunInfo.Controls.Add(this.label2);
            this.groupBoxRunInfo.Controls.Add(this.label4);
            this.groupBoxRunInfo.Controls.Add(this.richTextBoxRunNotes);
            this.groupBoxRunInfo.Controls.Add(this.comboBoxKeyword);
            this.groupBoxRunInfo.Location = new System.Drawing.Point(6, 38);
            this.groupBoxRunInfo.Name = "groupBoxRunInfo";
            this.groupBoxRunInfo.Size = new System.Drawing.Size(402, 274);
            this.groupBoxRunInfo.TabIndex = 21;
            this.groupBoxRunInfo.TabStop = false;
            this.groupBoxRunInfo.Text = "Run Info";
            this.groupBoxRunInfo.Visible = false;
            // 
            // checkBoxUseInName
            // 
            this.checkBoxUseInName.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxUseInName.AutoSize = true;
            this.checkBoxUseInName.Location = new System.Drawing.Point(311, 12);
            this.checkBoxUseInName.Name = "checkBoxUseInName";
            this.checkBoxUseInName.Size = new System.Drawing.Size(85, 17);
            this.checkBoxUseInName.TabIndex = 20;
            this.checkBoxUseInName.Text = "Use in name";
            this.checkBoxUseInName.UseVisualStyleBackColor = true;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(57, 36);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(38, 13);
            this.label2.TabIndex = 16;
            this.label2.Text = "Notes:";
            // 
            // label4
            // 
            this.label4.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(1, 246);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(94, 13);
            this.label4.TabIndex = 19;
            this.label4.Text = "Grouping Keyword";
            // 
            // richTextBoxRunNotes
            // 
            this.richTextBoxRunNotes.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.richTextBoxRunNotes.Location = new System.Drawing.Point(101, 33);
            this.richTextBoxRunNotes.Name = "richTextBoxRunNotes";
            this.richTextBoxRunNotes.Size = new System.Drawing.Size(295, 204);
            this.richTextBoxRunNotes.TabIndex = 17;
            this.richTextBoxRunNotes.Text = "";
            // 
            // comboBoxKeyword
            // 
            this.comboBoxKeyword.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBoxKeyword.FormattingEnabled = true;
            this.comboBoxKeyword.Location = new System.Drawing.Point(101, 243);
            this.comboBoxKeyword.Name = "comboBoxKeyword";
            this.comboBoxKeyword.Size = new System.Drawing.Size(297, 21);
            this.comboBoxKeyword.TabIndex = 18;
            // 
            // radioButtonMMSRun
            // 
            this.radioButtonMMSRun.AutoSize = true;
            this.radioButtonMMSRun.Location = new System.Drawing.Point(148, 15);
            this.radioButtonMMSRun.Name = "radioButtonMMSRun";
            this.radioButtonMMSRun.Size = new System.Drawing.Size(123, 17);
            this.radioButtonMMSRun.TabIndex = 1;
            this.radioButtonMMSRun.Text = "Log Run in the MMS";
            this.radioButtonMMSRun.UseVisualStyleBackColor = true;
            this.radioButtonMMSRun.CheckedChanged += new System.EventHandler(this.radioButtonMMSRun_CheckedChanged);
            // 
            // radioButtonRunActive
            // 
            this.radioButtonRunActive.AutoSize = true;
            this.radioButtonRunActive.Checked = true;
            this.radioButtonRunActive.Location = new System.Drawing.Point(21, 15);
            this.radioButtonRunActive.Name = "radioButtonRunActive";
            this.radioButtonRunActive.Size = new System.Drawing.Size(121, 17);
            this.radioButtonRunActive.TabIndex = 0;
            this.radioButtonRunActive.TabStop = true;
            this.radioButtonRunActive.Text = "Run Active Network";
            this.radioButtonRunActive.UseVisualStyleBackColor = true;
            this.radioButtonRunActive.CheckedChanged += new System.EventHandler(this.radioButtonRunActive_CheckedChanged);
            // 
            // checkBoxRiparianLogic
            // 
            this.checkBoxRiparianLogic.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxRiparianLogic.AutoSize = true;
            this.checkBoxRiparianLogic.Checked = true;
            this.checkBoxRiparianLogic.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkBoxRiparianLogic.Location = new System.Drawing.Point(442, 19);
            this.checkBoxRiparianLogic.Name = "checkBoxRiparianLogic";
            this.checkBoxRiparianLogic.Size = new System.Drawing.Size(179, 17);
            this.checkBoxRiparianLogic.TabIndex = 3;
            this.checkBoxRiparianLogic.Text = "Enable Riparian Allocation Logic";
            this.checkBoxRiparianLogic.UseVisualStyleBackColor = true;
            // 
            // groupBox6
            // 
            this.groupBox6.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox6.Controls.Add(this.radioButtonAgPckge);
            this.groupBox6.Controls.Add(this.radioButtonWRIMS);
            this.groupBox6.Location = new System.Drawing.Point(443, 103);
            this.groupBox6.Name = "groupBox6";
            this.groupBox6.Size = new System.Drawing.Size(183, 55);
            this.groupBox6.TabIndex = 2;
            this.groupBox6.TabStop = false;
            this.groupBox6.Text = "Agricultural Demand";
            // 
            // radioButtonAgPckge
            // 
            this.radioButtonAgPckge.AutoSize = true;
            this.radioButtonAgPckge.Enabled = false;
            this.radioButtonAgPckge.Location = new System.Drawing.Point(12, 32);
            this.radioButtonAgPckge.Name = "radioButtonAgPckge";
            this.radioButtonAgPckge.Size = new System.Drawing.Size(135, 17);
            this.radioButtonAgPckge.TabIndex = 1;
            this.radioButtonAgPckge.Text = "Ag. Package (dynamic)";
            this.radioButtonAgPckge.UseVisualStyleBackColor = true;
            // 
            // radioButtonWRIMS
            // 
            this.radioButtonWRIMS.AutoSize = true;
            this.radioButtonWRIMS.Checked = true;
            this.radioButtonWRIMS.Location = new System.Drawing.Point(13, 14);
            this.radioButtonWRIMS.Name = "radioButtonWRIMS";
            this.radioButtonWRIMS.Size = new System.Drawing.Size(69, 17);
            this.radioButtonWRIMS.TabIndex = 0;
            this.radioButtonWRIMS.TabStop = true;
            this.radioButtonWRIMS.Text = "eWRIMS";
            this.radioButtonWRIMS.UseVisualStyleBackColor = true;
            // 
            // groupBox5
            // 
            this.groupBox5.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox5.Controls.Add(this.radioButtonMS_GS);
            this.groupBox5.Controls.Add(this.radioButtonMODSIMOnly);
            this.groupBox5.Location = new System.Drawing.Point(442, 42);
            this.groupBox5.Name = "groupBox5";
            this.groupBox5.Size = new System.Drawing.Size(184, 55);
            this.groupBox5.TabIndex = 1;
            this.groupBox5.TabStop = false;
            this.groupBox5.Text = "Simulation Mode";
            // 
            // radioButtonMS_GS
            // 
            this.radioButtonMS_GS.AutoSize = true;
            this.radioButtonMS_GS.Location = new System.Drawing.Point(13, 32);
            this.radioButtonMS_GS.Name = "radioButtonMS_GS";
            this.radioButtonMS_GS.Size = new System.Drawing.Size(160, 17);
            this.radioButtonMS_GS.TabIndex = 1;
            this.radioButtonMS_GS.Text = "Coupled MODSIM-GSFLOW";
            this.radioButtonMS_GS.UseVisualStyleBackColor = true;
            // 
            // radioButtonMODSIMOnly
            // 
            this.radioButtonMODSIMOnly.AutoSize = true;
            this.radioButtonMODSIMOnly.Checked = true;
            this.radioButtonMODSIMOnly.Location = new System.Drawing.Point(13, 14);
            this.radioButtonMODSIMOnly.Name = "radioButtonMODSIMOnly";
            this.radioButtonMODSIMOnly.Size = new System.Drawing.Size(93, 17);
            this.radioButtonMODSIMOnly.TabIndex = 0;
            this.radioButtonMODSIMOnly.TabStop = true;
            this.radioButtonMODSIMOnly.Text = "MODSIM Only";
            this.radioButtonMODSIMOnly.UseVisualStyleBackColor = true;
            this.radioButtonMODSIMOnly.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // comboBoxMODSIMFile
            // 
            this.comboBoxMODSIMFile.FormattingEnabled = true;
            this.comboBoxMODSIMFile.Items.AddRange(new object[] {
            "Base Network(*.xy)",
            "Diversion Network (*_DIV.xy)",
            "Water Rights Network (*_DIV_WR.xy)",
            "Time Series (*_DIV_WRTS.xy)"});
            this.comboBoxMODSIMFile.Location = new System.Drawing.Point(98, 4);
            this.comboBoxMODSIMFile.Name = "comboBoxMODSIMFile";
            this.comboBoxMODSIMFile.Size = new System.Drawing.Size(256, 21);
            this.comboBoxMODSIMFile.TabIndex = 13;
            this.comboBoxMODSIMFile.SelectedIndexChanged += new System.EventHandler(this.comboBoxMODSIMFile_SelectedIndexChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(11, 7);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(80, 13);
            this.label3.TabIndex = 14;
            this.label3.Text = "Active Network";
            // 
            // pictureBoxStatus
            // 
            this.pictureBoxStatus.Image = global::RRModelingSystem.Properties.Resources.icons8_error_64;
            this.pictureBoxStatus.InitialImage = global::RRModelingSystem.Properties.Resources.icons8_ok_40;
            this.pictureBoxStatus.Location = new System.Drawing.Point(360, 4);
            this.pictureBoxStatus.Name = "pictureBoxStatus";
            this.pictureBoxStatus.Size = new System.Drawing.Size(21, 21);
            this.pictureBoxStatus.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pictureBoxStatus.TabIndex = 15;
            this.pictureBoxStatus.TabStop = false;
            // 
            // labelActFile
            // 
            this.labelActFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelActFile.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelActFile.Location = new System.Drawing.Point(100, 29);
            this.labelActFile.Name = "labelActFile";
            this.labelActFile.Size = new System.Drawing.Size(551, 29);
            this.labelActFile.TabIndex = 20;
            this.labelActFile.Text = "No active file";
            // 
            // Simulation
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.labelActFile);
            this.Controls.Add(this.pictureBoxStatus);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.comboBoxMODSIMFile);
            this.Controls.Add(this.groupBox4);
            this.Name = "Simulation";
            this.Size = new System.Drawing.Size(654, 686);
            this.Load += new System.EventHandler(this.Simulation_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.groupBox7.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridViewISF)).EndInit();
            this.groupBox4.ResumeLayout(false);
            this.groupBox4.PerformLayout();
            this.groupBox9.ResumeLayout(false);
            this.groupBox9.PerformLayout();
            this.groupBoxRunInfo.ResumeLayout(false);
            this.groupBoxRunInfo.PerformLayout();
            this.groupBox6.ResumeLayout(false);
            this.groupBox6.PerformLayout();
            this.groupBox5.ResumeLayout(false);
            this.groupBox5.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBoxStatus)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.Button buttonExecuteModel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxScnName;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.ComboBox comboBox5;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.GroupBox groupBox4;
        private System.Windows.Forms.GroupBox groupBox6;
        private System.Windows.Forms.RadioButton radioButtonAgPckge;
        private System.Windows.Forms.RadioButton radioButtonWRIMS;
        private System.Windows.Forms.GroupBox groupBox5;
        private System.Windows.Forms.RadioButton radioButtonMS_GS;
        private System.Windows.Forms.RadioButton radioButtonMODSIMOnly;
        private System.Windows.Forms.CheckBox checkBoxRiparianLogic;
        private System.Windows.Forms.CheckBox checkBoxAgON;
        private System.Windows.Forms.CheckBox checkBoxOutdoorDomON;
        private System.Windows.Forms.CheckBox checkBoxIndoorDomON;
        private System.Windows.Forms.CheckBox checkBoxOthersON;
        private System.Windows.Forms.ComboBox comboBoxMODSIMFile;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label labelCostBlock;
        private System.Windows.Forms.Button buttonNodeDW;
        private System.Windows.Forms.Button buttonNodeUP;
        private System.Windows.Forms.TreeView treeViewPsdoCost;
        private System.Windows.Forms.Label labelRiparian;
        private System.Windows.Forms.Label labelISFlinks;
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.GroupBox groupBox7;
        private System.Windows.Forms.DataGridView dataGridViewISF;
        private System.Windows.Forms.PictureBox pictureBoxStatus;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.RichTextBox richTextBoxRunNotes;
        private System.Windows.Forms.ComboBox comboBoxKeyword;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label labelActFile;
        private System.Windows.Forms.GroupBox groupBox9;
        private System.Windows.Forms.GroupBox groupBoxRunInfo;
        private System.Windows.Forms.RadioButton radioButtonMMSRun;
        private System.Windows.Forms.RadioButton radioButtonRunActive;
        private System.Windows.Forms.CheckBox checkBoxUseInName;
        private System.Windows.Forms.TextBox txtFactor;
        private System.Windows.Forms.CheckBox checkFactor;
    }
}
