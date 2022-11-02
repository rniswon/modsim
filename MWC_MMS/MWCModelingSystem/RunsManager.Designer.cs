
namespace RRModelingSystem
{
    partial class RunsManager
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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.splitContainer2 = new System.Windows.Forms.SplitContainer();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.textBoxHeading = new System.Windows.Forms.TextBox();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.gBoxAdaptDB = new System.Windows.Forms.GroupBox();
            this.labelScnName = new System.Windows.Forms.Label();
            this.checkBoxFileName = new System.Windows.Forms.CheckBox();
            this.checkBoxOutputDB = new System.Windows.Forms.CheckBox();
            this.txtOutputDB = new System.Windows.Forms.TextBox();
            this.btnBrowseDB = new System.Windows.Forms.Button();
            this.checkBoxScenario = new System.Windows.Forms.CheckBox();
            this.btnAdaptDB = new System.Windows.Forms.Button();
            this.checkBoxRunID = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.txtOutputRunIDDB = new System.Windows.Forms.TextBox();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).BeginInit();
            this.splitContainer2.Panel1.SuspendLayout();
            this.splitContainer2.Panel2.SuspendLayout();
            this.splitContainer2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.groupBox1.SuspendLayout();
            this.gBoxAdaptDB.SuspendLayout();
            this.SuspendLayout();
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(0, 0);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.splitContainer2);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.groupBox2);
            this.splitContainer1.Panel2.Controls.Add(this.groupBox1);
            this.splitContainer1.Size = new System.Drawing.Size(725, 488);
            this.splitContainer1.SplitterDistance = 299;
            this.splitContainer1.TabIndex = 0;
            // 
            // splitContainer2
            // 
            this.splitContainer2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer2.Location = new System.Drawing.Point(0, 0);
            this.splitContainer2.Name = "splitContainer2";
            // 
            // splitContainer2.Panel1
            // 
            this.splitContainer2.Panel1.Controls.Add(this.dataGridView1);
            // 
            // splitContainer2.Panel2
            // 
            this.splitContainer2.Panel2.Controls.Add(this.richTextBox1);
            this.splitContainer2.Panel2.Controls.Add(this.textBoxHeading);
            this.splitContainer2.Size = new System.Drawing.Size(725, 299);
            this.splitContainer2.SplitterDistance = 430;
            this.splitContainer2.TabIndex = 0;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeRows = false;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 0);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(430, 299);
            this.dataGridView1.TabIndex = 0;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            this.dataGridView1.RowEnter += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_RowEnter);
            this.dataGridView1.RowValidated += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_RowValidated);
            this.dataGridView1.SelectionChanged += new System.EventHandler(this.dataGridView1_SelectionChanged);
            this.dataGridView1.Validated += new System.EventHandler(this.dataGridView1_Validated);
            // 
            // richTextBox1
            // 
            this.richTextBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.richTextBox1.Location = new System.Drawing.Point(0, 21);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.Size = new System.Drawing.Size(291, 278);
            this.richTextBox1.TabIndex = 0;
            this.richTextBox1.Text = "";
            // 
            // textBoxHeading
            // 
            this.textBoxHeading.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.textBoxHeading.Dock = System.Windows.Forms.DockStyle.Top;
            this.textBoxHeading.Location = new System.Drawing.Point(0, 0);
            this.textBoxHeading.Multiline = true;
            this.textBoxHeading.Name = "textBoxHeading";
            this.textBoxHeading.ReadOnly = true;
            this.textBoxHeading.Size = new System.Drawing.Size(291, 21);
            this.textBoxHeading.TabIndex = 12;
            this.textBoxHeading.TabStop = false;
            this.textBoxHeading.Text = "Run Preferences";
            // 
            // groupBox2
            // 
            this.groupBox2.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox2.Location = new System.Drawing.Point(469, 0);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(253, 185);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            // 
            // groupBox1
            // 
            this.groupBox1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.groupBox1.Controls.Add(this.gBoxAdaptDB);
            this.groupBox1.Location = new System.Drawing.Point(0, 0);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(463, 185);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            // 
            // gBoxAdaptDB
            // 
            this.gBoxAdaptDB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gBoxAdaptDB.Controls.Add(this.txtOutputRunIDDB);
            this.gBoxAdaptDB.Controls.Add(this.labelScnName);
            this.gBoxAdaptDB.Controls.Add(this.checkBoxFileName);
            this.gBoxAdaptDB.Controls.Add(this.checkBoxOutputDB);
            this.gBoxAdaptDB.Controls.Add(this.txtOutputDB);
            this.gBoxAdaptDB.Controls.Add(this.btnBrowseDB);
            this.gBoxAdaptDB.Controls.Add(this.checkBoxScenario);
            this.gBoxAdaptDB.Controls.Add(this.btnAdaptDB);
            this.gBoxAdaptDB.Controls.Add(this.checkBoxRunID);
            this.gBoxAdaptDB.Controls.Add(this.label1);
            this.gBoxAdaptDB.Location = new System.Drawing.Point(6, 10);
            this.gBoxAdaptDB.Name = "gBoxAdaptDB";
            this.gBoxAdaptDB.Size = new System.Drawing.Size(451, 108);
            this.gBoxAdaptDB.TabIndex = 0;
            this.gBoxAdaptDB.TabStop = false;
            this.gBoxAdaptDB.Text = "Adapt SQLite DB Output";
            // 
            // labelScnName
            // 
            this.labelScnName.AutoSize = true;
            this.labelScnName.Font = new System.Drawing.Font("Microsoft Sans Serif", 6.75F);
            this.labelScnName.Location = new System.Drawing.Point(90, 69);
            this.labelScnName.Name = "labelScnName";
            this.labelScnName.Size = new System.Drawing.Size(71, 12);
            this.labelScnName.TabIndex = 1;
            this.labelScnName.Text = "Scenario Name:";
            // 
            // checkBoxFileName
            // 
            this.checkBoxFileName.AutoSize = true;
            this.checkBoxFileName.Location = new System.Drawing.Point(239, 84);
            this.checkBoxFileName.Name = "checkBoxFileName";
            this.checkBoxFileName.Size = new System.Drawing.Size(73, 17);
            this.checkBoxFileName.TabIndex = 6;
            this.checkBoxFileName.Text = "File Name";
            this.checkBoxFileName.UseVisualStyleBackColor = true;
            this.checkBoxFileName.CheckedChanged += new System.EventHandler(this.checkBoxFileName_CheckedChanged);
            // 
            // checkBoxOutputDB
            // 
            this.checkBoxOutputDB.AutoSize = true;
            this.checkBoxOutputDB.Location = new System.Drawing.Point(6, 21);
            this.checkBoxOutputDB.Name = "checkBoxOutputDB";
            this.checkBoxOutputDB.Size = new System.Drawing.Size(105, 17);
            this.checkBoxOutputDB.TabIndex = 2;
            this.checkBoxOutputDB.Text = "Use another DB:";
            this.checkBoxOutputDB.UseVisualStyleBackColor = true;
            this.checkBoxOutputDB.CheckedChanged += new System.EventHandler(this.checkBoxOutputDB_CheckedChanged);
            // 
            // txtOutputDB
            // 
            this.txtOutputDB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutputDB.Location = new System.Drawing.Point(111, 20);
            this.txtOutputDB.Name = "txtOutputDB";
            this.txtOutputDB.ReadOnly = true;
            this.txtOutputDB.Size = new System.Drawing.Size(254, 20);
            this.txtOutputDB.TabIndex = 3;
            // 
            // btnBrowseDB
            // 
            this.btnBrowseDB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseDB.Enabled = false;
            this.btnBrowseDB.Location = new System.Drawing.Point(370, 18);
            this.btnBrowseDB.Name = "btnBrowseDB";
            this.btnBrowseDB.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseDB.TabIndex = 2;
            this.btnBrowseDB.Text = "Browse DB";
            this.btnBrowseDB.UseVisualStyleBackColor = true;
            this.btnBrowseDB.Click += new System.EventHandler(this.btnBrowseDB_Click);
            // 
            // checkBoxScenario
            // 
            this.checkBoxScenario.AutoSize = true;
            this.checkBoxScenario.Location = new System.Drawing.Point(165, 84);
            this.checkBoxScenario.Name = "checkBoxScenario";
            this.checkBoxScenario.Size = new System.Drawing.Size(68, 17);
            this.checkBoxScenario.TabIndex = 5;
            this.checkBoxScenario.Text = "Scenario";
            this.checkBoxScenario.UseVisualStyleBackColor = true;
            this.checkBoxScenario.CheckedChanged += new System.EventHandler(this.checkBoxScenario_CheckedChanged);
            // 
            // btnAdaptDB
            // 
            this.btnAdaptDB.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAdaptDB.Location = new System.Drawing.Point(370, 45);
            this.btnAdaptDB.Name = "btnAdaptDB";
            this.btnAdaptDB.Size = new System.Drawing.Size(75, 23);
            this.btnAdaptDB.TabIndex = 1;
            this.btnAdaptDB.Text = "Adapt DB";
            this.btnAdaptDB.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.btnAdaptDB.UseVisualStyleBackColor = true;
            this.btnAdaptDB.Click += new System.EventHandler(this.btnAdaptDB_Click);
            // 
            // checkBoxRunID
            // 
            this.checkBoxRunID.AutoSize = true;
            this.checkBoxRunID.Location = new System.Drawing.Point(89, 84);
            this.checkBoxRunID.Name = "checkBoxRunID";
            this.checkBoxRunID.Size = new System.Drawing.Size(60, 17);
            this.checkBoxRunID.TabIndex = 4;
            this.checkBoxRunID.Text = "Run ID";
            this.checkBoxRunID.UseVisualStyleBackColor = true;
            this.checkBoxRunID.CheckedChanged += new System.EventHandler(this.checkBoxRunID_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 53);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(62, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Run ID DB:";
            this.label1.TextAlign = System.Drawing.ContentAlignment.TopRight;
            // 
            // txtOutputRunIDDB
            // 
            this.txtOutputRunIDDB.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutputRunIDDB.Location = new System.Drawing.Point(90, 47);
            this.txtOutputRunIDDB.Name = "txtOutputRunIDDB";
            this.txtOutputRunIDDB.Size = new System.Drawing.Size(275, 20);
            this.txtOutputRunIDDB.TabIndex = 4;
            // 
            // RunsManager
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.splitContainer1);
            this.Name = "RunsManager";
            this.Size = new System.Drawing.Size(725, 488);
            this.Load += new System.EventHandler(this.RunsManager_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.splitContainer2.Panel1.ResumeLayout(false);
            this.splitContainer2.Panel2.ResumeLayout(false);
            this.splitContainer2.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer2)).EndInit();
            this.splitContainer2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.groupBox1.ResumeLayout(false);
            this.gBoxAdaptDB.ResumeLayout(false);
            this.gBoxAdaptDB.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.SplitContainer splitContainer2;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.RichTextBox richTextBox1;
        private System.Windows.Forms.TextBox textBoxHeading;
        private System.Windows.Forms.Button btnAdaptDB;
        private System.Windows.Forms.Button btnBrowseDB;
        private System.Windows.Forms.GroupBox gBoxAdaptDB;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.CheckBox checkBoxScenario;
        private System.Windows.Forms.CheckBox checkBoxRunID;
        private System.Windows.Forms.CheckBox checkBoxFileName;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox txtOutputDB;
        private System.Windows.Forms.Label labelScnName;
        private System.Windows.Forms.CheckBox checkBoxOutputDB;
        private System.Windows.Forms.TextBox txtOutputRunIDDB;
    }
}
