namespace StaadSteelApp
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.btnDetect = new System.Windows.Forms.Button();
            this.btnDetectAll = new System.Windows.Forms.Button();
            this.lblCode = new System.Windows.Forms.Label();
            this.txtCode = new System.Windows.Forms.TextBox();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblName = new System.Windows.Forms.Label();
            this.cboMaterial = new System.Windows.Forms.ComboBox();
            this.lblMat = new System.Windows.Forms.Label();
            this.btnApplySel = new System.Windows.Forms.Button();
            this.grpCopyPaste = new System.Windows.Forms.GroupBox();
            this.txtCopied = new System.Windows.Forms.TextBox();
            this.btnCopy = new System.Windows.Forms.Button();
            this.btnPaste = new System.Windows.Forms.Button();
            this.cboType = new System.Windows.Forms.ComboBox();
            this.btnApplyAll = new System.Windows.Forms.Button();
            this.btnClean = new System.Windows.Forms.Button();
            this.btnMisc = new System.Windows.Forms.Button();
            this.btnRun = new System.Windows.Forms.Button();
            this.btnAdvance = new System.Windows.Forms.Button();
            this.btnAssignLoad = new System.Windows.Forms.Button();
            this.btnHideSelected = new System.Windows.Forms.Button();
            this.btnViewSelected = new System.Windows.Forms.Button();
            this.btnGroups = new System.Windows.Forms.Button();
            this.btnRatio = new System.Windows.Forms.Button();
            this.chkUPT = new System.Windows.Forms.CheckBox();
            this.chkUsedSections = new System.Windows.Forms.CheckBox();
            this.cboSeries = new System.Windows.Forms.ComboBox();
            this.lblSeries = new System.Windows.Forms.Label();
            this.cboSection = new System.Windows.Forms.ComboBox();
            this.lblSection = new System.Windows.Forms.Label();
            this.cboShape = new System.Windows.Forms.ComboBox();
            this.lblShape = new System.Windows.Forms.Label();
            this.cboZone = new System.Windows.Forms.ComboBox();
            this.lblZone = new System.Windows.Forms.Label();
            this.lblH1 = new System.Windows.Forms.Label();
            this.txtH1 = new System.Windows.Forms.TextBox();
            this.lblH2 = new System.Windows.Forms.Label();
            this.txtH2 = new System.Windows.Forms.TextBox();
            this.lblTw = new System.Windows.Forms.Label();
            this.txtTw = new System.Windows.Forms.TextBox();
            this.lblBf = new System.Windows.Forms.Label();
            this.txtBf = new System.Windows.Forms.TextBox();
            this.lblTf = new System.Windows.Forms.Label();
            this.txtTf = new System.Windows.Forms.TextBox();
            this.lblBfb = new System.Windows.Forms.Label();
            this.txtBfb = new System.Windows.Forms.TextBox();
            this.lblTfb = new System.Windows.Forms.Label();
            this.txtTfb = new System.Windows.Forms.TextBox();
            this.lblDis = new System.Windows.Forms.Label();
            this.txtDis = new System.Windows.Forms.TextBox();
            this.grpDimensions = new System.Windows.Forms.GroupBox();
            this.grpUsedSections = new System.Windows.Forms.GroupBox();
            this.lstUsedSections = new System.Windows.Forms.ListBox();
            this.grpCopyPaste.SuspendLayout();
            this.grpDimensions.SuspendLayout();
            this.grpUsedSections.SuspendLayout();
            this.SuspendLayout();
            //
            // btnDetect
            //
            this.btnDetect.BackColor = System.Drawing.Color.LightBlue;
            this.btnDetect.Location = new System.Drawing.Point(15, 12);
            this.btnDetect.Name = "btnDetect";
            this.btnDetect.Size = new System.Drawing.Size(85, 40);
            this.btnDetect.TabIndex = 0;
            this.btnDetect.Text = "Detect Active";
            this.btnDetect.UseVisualStyleBackColor = false;
            this.btnDetect.Click += new System.EventHandler(this.btnDetect_Click);
            //
            // btnDetectAll
            //
            this.btnDetectAll.BackColor = System.Drawing.Color.LightPink;
            this.btnDetectAll.Location = new System.Drawing.Point(106, 12);
            this.btnDetectAll.Name = "btnDetectAll";
            this.btnDetectAll.Size = new System.Drawing.Size(85, 40);
            this.btnDetectAll.TabIndex = 15;
            this.btnDetectAll.Text = "Detect All";
            this.btnDetectAll.UseVisualStyleBackColor = false;
            this.btnDetectAll.Click += new System.EventHandler(this.btnDetectAll_Click);
            //
            // lblCode
            //
            this.lblCode.AutoSize = true;
            this.lblCode.Location = new System.Drawing.Point(18, 61);
            this.lblCode.Name = "lblCode";
            this.lblCode.Size = new System.Drawing.Size(32, 13);
            this.lblCode.TabIndex = 16;
            this.lblCode.Text = "Code";
            //
            // txtCode
            //
            this.txtCode.Location = new System.Drawing.Point(63, 58);
            this.txtCode.Name = "txtCode";
            this.txtCode.Size = new System.Drawing.Size(128, 20);
            this.txtCode.TabIndex = 1;
            //
            // txtName
            //
            this.txtName.Location = new System.Drawing.Point(63, 84);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(128, 20);
            this.txtName.TabIndex = 2;
            //
            // lblName
            //
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(18, 87);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(35, 13);
            this.lblName.TabIndex = 15;
            this.lblName.Text = "Name";
            //
            // cboMaterial
            //
            this.cboMaterial.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboMaterial.FormattingEnabled = true;
            this.cboMaterial.Location = new System.Drawing.Point(63, 367);
            this.cboMaterial.Name = "cboMaterial";
            this.cboMaterial.Size = new System.Drawing.Size(128, 21);
            this.cboMaterial.TabIndex = 7;
            //
            // lblMat
            //
            this.lblMat.AutoSize = true;
            this.lblMat.Location = new System.Drawing.Point(18, 370);
            this.lblMat.Name = "lblMat";
            this.lblMat.Size = new System.Drawing.Size(44, 13);
            this.lblMat.TabIndex = 14;
            this.lblMat.Text = "Material";
            //
            // btnApplySel
            //
            this.btnApplySel.BackColor = System.Drawing.Color.White;
            this.btnApplySel.Location = new System.Drawing.Point(106, 394);
            this.btnApplySel.Name = "btnApplySel";
            this.btnApplySel.Size = new System.Drawing.Size(85, 30);
            this.btnApplySel.TabIndex = 13;
            this.btnApplySel.Text = "Selected";
            this.btnApplySel.UseVisualStyleBackColor = false;
            this.btnApplySel.Click += new System.EventHandler(this.btnApplySel_Click);
            //
            // grpCopyPaste
            //
            this.grpCopyPaste.Controls.Add(this.txtCopied);
            this.grpCopyPaste.Controls.Add(this.btnCopy);
            this.grpCopyPaste.Controls.Add(this.btnPaste);
            this.grpCopyPaste.Location = new System.Drawing.Point(15, 430);
            this.grpCopyPaste.Name = "grpCopyPaste";
            this.grpCopyPaste.Size = new System.Drawing.Size(176, 90);
            this.grpCopyPaste.TabIndex = 10;
            this.grpCopyPaste.TabStop = false;
            this.grpCopyPaste.Text = "Copy / Paste Section";
            //
            // txtCopied
            //
            this.txtCopied.BackColor = System.Drawing.Color.WhiteSmoke;
            this.txtCopied.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCopied.ForeColor = System.Drawing.Color.Red;
            this.txtCopied.Location = new System.Drawing.Point(10, 20);
            this.txtCopied.Name = "txtCopied";
            this.txtCopied.ReadOnly = true;
            this.txtCopied.Size = new System.Drawing.Size(156, 20);
            this.txtCopied.TabIndex = 0;
            this.txtCopied.Text = "(No section copied)";
            this.txtCopied.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            //
            // btnCopy
            //
            this.btnCopy.Location = new System.Drawing.Point(10, 50);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(75, 30);
            this.btnCopy.TabIndex = 1;
            this.btnCopy.Text = "Copy";
            this.btnCopy.Click += new System.EventHandler(this.btnCopy_Click);
            //
            // btnPaste
            //
            this.btnPaste.Location = new System.Drawing.Point(91, 50);
            this.btnPaste.Name = "btnPaste";
            this.btnPaste.Size = new System.Drawing.Size(75, 30);
            this.btnPaste.TabIndex = 2;
            this.btnPaste.Text = "Paste";
            this.btnPaste.Click += new System.EventHandler(this.btnPaste_Click);
            //
            // cboType
            //
            this.cboType.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboType.FormattingEnabled = true;
            this.cboType.Items.AddRange(new object[] {
            "Tapered I",
            "Circle",
            "Rectangle",
            "Tee",
            "Trapezoidal",
            "General",
            "Tapered Pipe",
            "Tapered Tube",
            "Legacy"});
            this.cboType.Location = new System.Drawing.Point(63, 110);
            this.cboType.Name = "cboType";
            this.cboType.Size = new System.Drawing.Size(128, 21);
            this.cboType.TabIndex = 3;
            //
            // btnApplyAll
            //
            this.btnApplyAll.BackColor = System.Drawing.Color.White;
            this.btnApplyAll.Location = new System.Drawing.Point(15, 394);
            this.btnApplyAll.Name = "btnApplyAll";
            this.btnApplyAll.Size = new System.Drawing.Size(85, 30);
            this.btnApplyAll.TabIndex = 9;
            this.btnApplyAll.Text = "All";
            this.btnApplyAll.UseVisualStyleBackColor = false;
            this.btnApplyAll.Click += new System.EventHandler(this.btnApplyAll_Click);
            //
            // btnClean
            //
            this.btnClean.Location = new System.Drawing.Point(106, 630);
            this.btnClean.Name = "btnClean";
            this.btnClean.Size = new System.Drawing.Size(85, 30);
            this.btnClean.TabIndex = 11;
            this.btnClean.Text = "Export";
            this.btnClean.UseVisualStyleBackColor = true;
            this.btnClean.Click += new System.EventHandler(this.btnClean_Click);
            //
            // btnMisc
            //
            this.btnMisc.Location = new System.Drawing.Point(15, 560);
            this.btnMisc.Name = "btnMisc";
            this.btnMisc.Size = new System.Drawing.Size(85, 30);
            this.btnMisc.TabIndex = 12;
            this.btnMisc.Text = "Misc Tools";
            this.btnMisc.UseVisualStyleBackColor = true;
            this.btnMisc.Click += new System.EventHandler(this.btnMisc_Click);
            //
            // btnRun
            //
            this.btnRun.Location = new System.Drawing.Point(15, 596);
            this.btnRun.Name = "btnRun";
            this.btnRun.Size = new System.Drawing.Size(85, 30);
            this.btnRun.TabIndex = 15;
            this.btnRun.Text = "RUN";
            this.btnRun.UseVisualStyleBackColor = true;
            this.btnRun.Click += new System.EventHandler(this.btnRun_Click);
            //
            // btnAdvance
            //
            this.btnAdvance.Location = new System.Drawing.Point(106, 560);
            this.btnAdvance.Name = "btnAdvance";
            this.btnAdvance.Size = new System.Drawing.Size(85, 30);
            this.btnAdvance.TabIndex = 16;
            this.btnAdvance.Text = "Advance";
            this.btnAdvance.UseVisualStyleBackColor = true;
            this.btnAdvance.Click += new System.EventHandler(this.btnAdvance_Click);
            //
            // btnAssignLoad
            //
            this.btnAssignLoad.Location = new System.Drawing.Point(15, 630);
            this.btnAssignLoad.Name = "btnAssignLoad";
            this.btnAssignLoad.Size = new System.Drawing.Size(85, 30);
            this.btnAssignLoad.TabIndex = 17;
            this.btnAssignLoad.Text = "Assign Load";
            this.btnAssignLoad.UseVisualStyleBackColor = true;
            this.btnAssignLoad.Click += new System.EventHandler(this.btnAssignLoad_Click);
            //
            // btnHideSelected
            //
            this.btnHideSelected.Location = new System.Drawing.Point(15, 526);
            this.btnHideSelected.Name = "btnHideSelected";
            this.btnHideSelected.Size = new System.Drawing.Size(85, 30);
            this.btnHideSelected.TabIndex = 21;
            this.btnHideSelected.Text = "Hide Selected";
            this.btnHideSelected.UseVisualStyleBackColor = true;
            this.btnHideSelected.Click += new System.EventHandler(this.btnHideSelected_Click);
            //
            // btnViewSelected
            //
            this.btnViewSelected.Location = new System.Drawing.Point(106, 526);
            this.btnViewSelected.Name = "btnViewSelected";
            this.btnViewSelected.Size = new System.Drawing.Size(85, 30);
            this.btnViewSelected.TabIndex = 22;
            this.btnViewSelected.Text = "View Selected";
            this.btnViewSelected.UseVisualStyleBackColor = true;
            this.btnViewSelected.Click += new System.EventHandler(this.btnViewSelected_Click);
            //
            // btnGroups
            //
            this.btnGroups.Location = new System.Drawing.Point(15, 664);
            this.btnGroups.Name = "btnGroups";
            this.btnGroups.Size = new System.Drawing.Size(85, 30);
            this.btnGroups.TabIndex = 20;
            this.btnGroups.Text = "Group";
            this.btnGroups.UseVisualStyleBackColor = true;
            this.btnGroups.Click += new System.EventHandler(this.btnGroups_Click);
            //
            // btnRatio
            //
            this.btnRatio.Location = new System.Drawing.Point(106, 664);
            this.btnRatio.Name = "btnRatio";
            this.btnRatio.Size = new System.Drawing.Size(85, 30);
            this.btnRatio.TabIndex = 23;
            this.btnRatio.Text = "Ratio";
            this.btnRatio.UseVisualStyleBackColor = true;
            this.btnRatio.Click += new System.EventHandler(this.btnRatio_Click);
            //
            // chkUPT
            //
            this.chkUPT.AutoSize = true;
            this.chkUPT.Location = new System.Drawing.Point(21, 137);
            this.chkUPT.Name = "chkUPT";
            this.chkUPT.Size = new System.Drawing.Size(96, 17);
            this.chkUPT.TabIndex = 8;
            this.chkUPT.Text = "Create as UPT";
            this.chkUPT.UseVisualStyleBackColor = true;
            this.chkUPT.CheckedChanged += new System.EventHandler(this.chkUPT_CheckedChanged);
            //
            // chkUsedSections
            //
            this.chkUsedSections.AutoSize = true;
            this.chkUsedSections.Location = new System.Drawing.Point(123, 137);
            this.chkUsedSections.Name = "chkUsedSections";
            this.chkUsedSections.Size = new System.Drawing.Size(51, 17);
            this.chkUsedSections.TabIndex = 18;
            this.chkUsedSections.Text = "Used";
            this.chkUsedSections.UseVisualStyleBackColor = true;
            this.chkUsedSections.CheckedChanged += new System.EventHandler(this.chkUsedSections_CheckedChanged);
            //
            // cboSeries
            //
            this.cboSeries.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSeries.FormattingEnabled = true;
            this.cboSeries.Location = new System.Drawing.Point(55, 80);
            this.cboSeries.Name = "cboSeries";
            this.cboSeries.Size = new System.Drawing.Size(110, 21);
            this.cboSeries.TabIndex = 27;
            this.cboSeries.Visible = false;
            //
            // lblSeries
            //
            this.lblSeries.AutoSize = true;
            this.lblSeries.Location = new System.Drawing.Point(10, 83);
            this.lblSeries.Name = "lblSeries";
            this.lblSeries.Size = new System.Drawing.Size(36, 13);
            this.lblSeries.TabIndex = 26;
            this.lblSeries.Text = "Series";
            this.lblSeries.Visible = false;
            //
            // cboSection
            //
            this.cboSection.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboSection.FormattingEnabled = true;
            this.cboSection.Location = new System.Drawing.Point(55, 110);
            this.cboSection.Name = "cboSection";
            this.cboSection.Size = new System.Drawing.Size(110, 21);
            this.cboSection.TabIndex = 25;
            this.cboSection.Visible = false;
            //
            // lblSection
            //
            this.lblSection.AutoSize = true;
            this.lblSection.Location = new System.Drawing.Point(10, 113);
            this.lblSection.Name = "lblSection";
            this.lblSection.Size = new System.Drawing.Size(43, 13);
            this.lblSection.TabIndex = 24;
            this.lblSection.Text = "Section";
            this.lblSection.Visible = false;
            //
            // cboShape
            //
            this.cboShape.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboShape.FormattingEnabled = true;
            this.cboShape.Location = new System.Drawing.Point(55, 50);
            this.cboShape.Name = "cboShape";
            this.cboShape.Size = new System.Drawing.Size(110, 21);
            this.cboShape.TabIndex = 23;
            this.cboShape.Visible = false;
            //
            // lblShape
            //
            this.lblShape.AutoSize = true;
            this.lblShape.Location = new System.Drawing.Point(10, 53);
            this.lblShape.Name = "lblShape";
            this.lblShape.Size = new System.Drawing.Size(38, 13);
            this.lblShape.TabIndex = 22;
            this.lblShape.Text = "Shape";
            this.lblShape.Visible = false;
            //
            // cboZone
            //
            this.cboZone.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboZone.FormattingEnabled = true;
            this.cboZone.Location = new System.Drawing.Point(55, 20);
            this.cboZone.Name = "cboZone";
            this.cboZone.Size = new System.Drawing.Size(110, 21);
            this.cboZone.TabIndex = 21;
            this.cboZone.Visible = false;
            //
            // lblZone
            //
            this.lblZone.AutoSize = true;
            this.lblZone.Location = new System.Drawing.Point(10, 23);
            this.lblZone.Name = "lblZone";
            this.lblZone.Size = new System.Drawing.Size(32, 13);
            this.lblZone.TabIndex = 20;
            this.lblZone.Text = "Zone";
            this.lblZone.Visible = false;
            //
            // lblH1
            //
            this.lblH1.AutoSize = true;
            this.lblH1.Location = new System.Drawing.Point(10, 23);
            this.lblH1.Name = "lblH1";
            this.lblH1.Size = new System.Drawing.Size(21, 13);
            this.lblH1.TabIndex = 0;
            this.lblH1.Text = "H1";
            //
            // txtH1
            //
            this.txtH1.Location = new System.Drawing.Point(70, 20);
            this.txtH1.Name = "txtH1";
            this.txtH1.Size = new System.Drawing.Size(95, 20);
            this.txtH1.TabIndex = 1;
            //
            // lblH2
            //
            this.lblH2.AutoSize = true;
            this.lblH2.Location = new System.Drawing.Point(10, 49);
            this.lblH2.Name = "lblH2";
            this.lblH2.Size = new System.Drawing.Size(21, 13);
            this.lblH2.TabIndex = 2;
            this.lblH2.Text = "H2";
            //
            // txtH2
            //
            this.txtH2.Location = new System.Drawing.Point(70, 46);
            this.txtH2.Name = "txtH2";
            this.txtH2.Size = new System.Drawing.Size(95, 20);
            this.txtH2.TabIndex = 3;
            //
            // lblTw
            //
            this.lblTw.AutoSize = true;
            this.lblTw.Location = new System.Drawing.Point(10, 75);
            this.lblTw.Name = "lblTw";
            this.lblTw.Size = new System.Drawing.Size(22, 13);
            this.lblTw.TabIndex = 4;
            this.lblTw.Text = "Tw";
            //
            // txtTw
            //
            this.txtTw.Location = new System.Drawing.Point(70, 72);
            this.txtTw.Name = "txtTw";
            this.txtTw.Size = new System.Drawing.Size(95, 20);
            this.txtTw.TabIndex = 5;
            //
            // lblBf
            //
            this.lblBf.AutoSize = true;
            this.lblBf.Location = new System.Drawing.Point(10, 101);
            this.lblBf.Name = "lblBf";
            this.lblBf.Size = new System.Drawing.Size(17, 13);
            this.lblBf.TabIndex = 6;
            this.lblBf.Text = "Bf";
            //
            // txtBf
            //
            this.txtBf.Location = new System.Drawing.Point(70, 98);
            this.txtBf.Name = "txtBf";
            this.txtBf.Size = new System.Drawing.Size(95, 20);
            this.txtBf.TabIndex = 7;
            //
            // lblTf
            //
            this.lblTf.AutoSize = true;
            this.lblTf.Location = new System.Drawing.Point(10, 127);
            this.lblTf.Name = "lblTf";
            this.lblTf.Size = new System.Drawing.Size(17, 13);
            this.lblTf.TabIndex = 8;
            this.lblTf.Text = "Tf";
            //
            // txtTf
            //
            this.txtTf.Location = new System.Drawing.Point(70, 124);
            this.txtTf.Name = "txtTf";
            this.txtTf.Size = new System.Drawing.Size(95, 20);
            this.txtTf.TabIndex = 9;
            //
            // lblBfb
            //
            this.lblBfb.AutoSize = true;
            this.lblBfb.Location = new System.Drawing.Point(10, 153);
            this.lblBfb.Name = "lblBfb";
            this.lblBfb.Size = new System.Drawing.Size(23, 13);
            this.lblBfb.TabIndex = 10;
            this.lblBfb.Text = "Bfb";
            //
            // txtBfb
            //
            this.txtBfb.Location = new System.Drawing.Point(70, 150);
            this.txtBfb.Name = "txtBfb";
            this.txtBfb.Size = new System.Drawing.Size(95, 20);
            this.txtBfb.TabIndex = 11;
            //
            // lblTfb
            //
            this.lblTfb.AutoSize = true;
            this.lblTfb.Location = new System.Drawing.Point(10, 179);
            this.lblTfb.Name = "lblTfb";
            this.lblTfb.Size = new System.Drawing.Size(23, 13);
            this.lblTfb.TabIndex = 12;
            this.lblTfb.Text = "Tfb";
            //
            // txtTfb
            //
            this.txtTfb.Location = new System.Drawing.Point(70, 176);
            this.txtTfb.Name = "txtTfb";
            this.txtTfb.Size = new System.Drawing.Size(95, 20);
            this.txtTfb.TabIndex = 13;
            //
            // lblDis
            //
            this.lblDis.AutoSize = true;
            this.lblDis.Location = new System.Drawing.Point(10, 205);
            this.lblDis.Name = "lblDis";
            this.lblDis.Size = new System.Drawing.Size(47, 13);
            this.lblDis.TabIndex = 14;
            this.lblDis.Text = "Gap/Dis";
            //
            // txtDis
            //
            this.txtDis.Location = new System.Drawing.Point(70, 202);
            this.txtDis.Name = "txtDis";
            this.txtDis.Size = new System.Drawing.Size(95, 20);
            this.txtDis.TabIndex = 15;
            //
            // grpDimensions
            //
            this.grpDimensions.Controls.Add(this.txtDis);
            this.grpDimensions.Controls.Add(this.lblDis);
            this.grpDimensions.Controls.Add(this.txtTfb);
            this.grpDimensions.Controls.Add(this.lblTfb);
            this.grpDimensions.Controls.Add(this.txtBfb);
            this.grpDimensions.Controls.Add(this.lblBfb);
            this.grpDimensions.Controls.Add(this.txtTf);
            this.grpDimensions.Controls.Add(this.lblTf);
            this.grpDimensions.Controls.Add(this.txtBf);
            this.grpDimensions.Controls.Add(this.lblBf);
            this.grpDimensions.Controls.Add(this.txtTw);
            this.grpDimensions.Controls.Add(this.lblTw);
            this.grpDimensions.Controls.Add(this.txtH2);
            this.grpDimensions.Controls.Add(this.lblH2);
            this.grpDimensions.Controls.Add(this.txtH1);
            this.grpDimensions.Controls.Add(this.lblH1);
            this.grpDimensions.Controls.Add(this.lblZone);
            this.grpDimensions.Controls.Add(this.cboZone);
            this.grpDimensions.Controls.Add(this.lblShape);
            this.grpDimensions.Controls.Add(this.cboShape);
            this.grpDimensions.Controls.Add(this.lblSection);
            this.grpDimensions.Controls.Add(this.cboSection);
            this.grpDimensions.Controls.Add(this.lblSeries);
            this.grpDimensions.Controls.Add(this.cboSeries);
            this.grpDimensions.Location = new System.Drawing.Point(21, 160);
            this.grpDimensions.Name = "grpDimensions";
            this.grpDimensions.Size = new System.Drawing.Size(176, 200);
            this.grpDimensions.TabIndex = 6;
            this.grpDimensions.TabStop = false;
            this.grpDimensions.Text = "Dimensions";
            this.grpDimensions.Enter += new System.EventHandler(this.grpDimensions_Enter);
            //
            // grpUsedSections
            //
            this.grpUsedSections.Controls.Add(this.lstUsedSections);
            this.grpUsedSections.Location = new System.Drawing.Point(21, 160);
            this.grpUsedSections.Name = "grpUsedSections";
            this.grpUsedSections.Size = new System.Drawing.Size(176, 200);
            this.grpUsedSections.TabIndex = 19;
            this.grpUsedSections.TabStop = false;
            this.grpUsedSections.Text = "Used Sections";
            this.grpUsedSections.Visible = false;
            //
            // lstUsedSections
            //
            this.lstUsedSections.FormattingEnabled = true;
            this.lstUsedSections.HorizontalScrollbar = true;
            this.lstUsedSections.IntegralHeight = false;
            this.lstUsedSections.Location = new System.Drawing.Point(10, 20);
            this.lstUsedSections.Name = "lstUsedSections";
            this.lstUsedSections.Size = new System.Drawing.Size(156, 170);
            this.lstUsedSections.TabIndex = 0;
            //
            // MainForm
            //
            this.ClientSize = new System.Drawing.Size(203, 704);
            this.Controls.Add(this.grpUsedSections);
            this.Controls.Add(this.chkUsedSections);
            this.Controls.Add(this.chkUPT);
            this.Controls.Add(this.grpCopyPaste);
            this.Controls.Add(this.btnClean);
            this.Controls.Add(this.btnMisc);
            this.Controls.Add(this.btnApplyAll);
            this.Controls.Add(this.btnApplySel);
            this.Controls.Add(this.cboMaterial);
            this.Controls.Add(this.lblMat);
            this.Controls.Add(this.grpDimensions);
            this.Controls.Add(this.cboType);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.txtCode);
            this.Controls.Add(this.lblCode);
            this.Controls.Add(this.btnDetect);
            this.Controls.Add(this.btnDetectAll);
            this.Controls.Add(this.btnRun);
            this.Controls.Add(this.btnAdvance);
            this.Controls.Add(this.btnAssignLoad);
            this.Controls.Add(this.btnHideSelected);
            this.Controls.Add(this.btnViewSelected);
            this.Controls.Add(this.btnGroups);
            this.Controls.Add(this.btnRatio);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "STAAD Tools";
            this.TopMost = true;
            this.grpCopyPaste.ResumeLayout(false);
            this.grpCopyPaste.PerformLayout();
            this.grpDimensions.ResumeLayout(false);
            this.grpDimensions.PerformLayout();
            this.grpUsedSections.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private System.Windows.Forms.Button btnDetect;
        private System.Windows.Forms.Label lblCode;
        private System.Windows.Forms.TextBox txtCode;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.ComboBox cboMaterial;
        private System.Windows.Forms.Label lblMat;
        private System.Windows.Forms.Button btnApplySel;
        private System.Windows.Forms.Button btnApplyAll;
        private System.Windows.Forms.Button btnClean;
        private System.Windows.Forms.ComboBox cboType;
        private System.Windows.Forms.GroupBox grpCopyPaste;
        private System.Windows.Forms.TextBox txtCopied;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.Button btnPaste;
        private System.Windows.Forms.Button btnMisc;
        private System.Windows.Forms.Button btnRun;
        private System.Windows.Forms.Button btnAdvance;
        private System.Windows.Forms.Button btnAssignLoad;
        private System.Windows.Forms.Button btnHideSelected;
        private System.Windows.Forms.Button btnViewSelected;
        private System.Windows.Forms.Button btnGroups;
        private System.Windows.Forms.Button btnRatio;
        private System.Windows.Forms.Button btnDetectAll;
        private System.Windows.Forms.CheckBox chkUPT;
        private System.Windows.Forms.CheckBox chkUsedSections;
        private System.Windows.Forms.ComboBox cboSeries;
        private System.Windows.Forms.Label lblSeries;
        private System.Windows.Forms.ComboBox cboSection;
        private System.Windows.Forms.Label lblSection;
        private System.Windows.Forms.ComboBox cboShape;
        private System.Windows.Forms.Label lblShape;
        private System.Windows.Forms.ComboBox cboZone;
        private System.Windows.Forms.Label lblZone;
        private System.Windows.Forms.Label lblH1;
        private System.Windows.Forms.TextBox txtH1;
        private System.Windows.Forms.Label lblH2;
        private System.Windows.Forms.TextBox txtH2;
        private System.Windows.Forms.Label lblTw;
        private System.Windows.Forms.TextBox txtTw;
        private System.Windows.Forms.Label lblBf;
        private System.Windows.Forms.TextBox txtBf;
        private System.Windows.Forms.Label lblTf;
        private System.Windows.Forms.TextBox txtTf;
        private System.Windows.Forms.Label lblBfb;
        private System.Windows.Forms.TextBox txtBfb;
        private System.Windows.Forms.Label lblTfb;
        private System.Windows.Forms.TextBox txtTfb;
        private System.Windows.Forms.Label lblDis;
        private System.Windows.Forms.TextBox txtDis;
        private System.Windows.Forms.GroupBox grpDimensions;
        private System.Windows.Forms.GroupBox grpUsedSections;
        private System.Windows.Forms.ListBox lstUsedSections;
    }

}

