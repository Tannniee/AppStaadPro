namespace StaadSteelApp
{
    partial class AssignLoadForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblLoadCase;
        private System.Windows.Forms.ComboBox cboLoadCases;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.GroupBox grpApplyTo;
        private System.Windows.Forms.RadioButton rbSelectedSource;
        private System.Windows.Forms.RadioButton rbGroupSource;
        private System.Windows.Forms.Label lblGroup;
        private System.Windows.Forms.ComboBox cboGroups;
        private System.Windows.Forms.GroupBox grpAssignType;
        private System.Windows.Forms.RadioButton rbMemberLoad;
        private System.Windows.Forms.RadioButton rbNodalLoad;
        private System.Windows.Forms.GroupBox grpMemberLoad;
        private System.Windows.Forms.GroupBox grpDirection;
        private System.Windows.Forms.RadioButton rbLocalX;
        private System.Windows.Forms.RadioButton rbGlobalX;
        private System.Windows.Forms.RadioButton rbProjectedX;
        private System.Windows.Forms.RadioButton rbLocalY;
        private System.Windows.Forms.RadioButton rbGlobalY;
        private System.Windows.Forms.RadioButton rbProjectedY;
        private System.Windows.Forms.RadioButton rbLocalZ;
        private System.Windows.Forms.RadioButton rbGlobalZ;
        private System.Windows.Forms.RadioButton rbProjectedZ;
        private System.Windows.Forms.Label lblMemberForce;
        private System.Windows.Forms.TextBox txtMemberForce;
        private System.Windows.Forms.Label lblMemberUnit;
        private System.Windows.Forms.GroupBox grpNodalLoad;
        private System.Windows.Forms.Label lblFx;
        private System.Windows.Forms.Label lblFy;
        private System.Windows.Forms.Label lblFz;
        private System.Windows.Forms.Label lblMx;
        private System.Windows.Forms.Label lblMy;
        private System.Windows.Forms.Label lblMz;
        private System.Windows.Forms.TextBox txtFx;
        private System.Windows.Forms.TextBox txtFy;
        private System.Windows.Forms.TextBox txtFz;
        private System.Windows.Forms.TextBox txtMx;
        private System.Windows.Forms.TextBox txtMy;
        private System.Windows.Forms.TextBox txtMz;
        private System.Windows.Forms.Label lblFxUnit;
        private System.Windows.Forms.Label lblFyUnit;
        private System.Windows.Forms.Label lblFzUnit;
        private System.Windows.Forms.Label lblMxUnit;
        private System.Windows.Forms.Label lblMyUnit;
        private System.Windows.Forms.Label lblMzUnit;
        private System.Windows.Forms.Button btnAssign;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnLoadTree;
        private System.Windows.Forms.Panel pnlLoadTreeHost;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }

            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.lblLoadCase = new System.Windows.Forms.Label();
            this.cboLoadCases = new System.Windows.Forms.ComboBox();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.grpApplyTo = new System.Windows.Forms.GroupBox();
            this.cboGroups = new System.Windows.Forms.ComboBox();
            this.lblGroup = new System.Windows.Forms.Label();
            this.rbGroupSource = new System.Windows.Forms.RadioButton();
            this.rbSelectedSource = new System.Windows.Forms.RadioButton();
            this.grpAssignType = new System.Windows.Forms.GroupBox();
            this.rbNodalLoad = new System.Windows.Forms.RadioButton();
            this.rbMemberLoad = new System.Windows.Forms.RadioButton();
            this.grpMemberLoad = new System.Windows.Forms.GroupBox();
            this.lblMemberUnit = new System.Windows.Forms.Label();
            this.txtMemberForce = new System.Windows.Forms.TextBox();
            this.lblMemberForce = new System.Windows.Forms.Label();
            this.grpDirection = new System.Windows.Forms.GroupBox();
            this.rbProjectedZ = new System.Windows.Forms.RadioButton();
            this.rbGlobalZ = new System.Windows.Forms.RadioButton();
            this.rbLocalZ = new System.Windows.Forms.RadioButton();
            this.rbProjectedY = new System.Windows.Forms.RadioButton();
            this.rbGlobalY = new System.Windows.Forms.RadioButton();
            this.rbLocalY = new System.Windows.Forms.RadioButton();
            this.rbProjectedX = new System.Windows.Forms.RadioButton();
            this.rbGlobalX = new System.Windows.Forms.RadioButton();
            this.rbLocalX = new System.Windows.Forms.RadioButton();
            this.grpNodalLoad = new System.Windows.Forms.GroupBox();
            this.lblMzUnit = new System.Windows.Forms.Label();
            this.lblMyUnit = new System.Windows.Forms.Label();
            this.lblMxUnit = new System.Windows.Forms.Label();
            this.lblFzUnit = new System.Windows.Forms.Label();
            this.lblFyUnit = new System.Windows.Forms.Label();
            this.lblFxUnit = new System.Windows.Forms.Label();
            this.txtMz = new System.Windows.Forms.TextBox();
            this.txtMy = new System.Windows.Forms.TextBox();
            this.txtMx = new System.Windows.Forms.TextBox();
            this.txtFz = new System.Windows.Forms.TextBox();
            this.txtFy = new System.Windows.Forms.TextBox();
            this.txtFx = new System.Windows.Forms.TextBox();
            this.lblMz = new System.Windows.Forms.Label();
            this.lblMy = new System.Windows.Forms.Label();
            this.lblMx = new System.Windows.Forms.Label();
            this.lblFz = new System.Windows.Forms.Label();
            this.lblFy = new System.Windows.Forms.Label();
            this.lblFx = new System.Windows.Forms.Label();
            this.btnAssign = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.btnLoadTree = new System.Windows.Forms.Button();
            this.pnlLoadTreeHost = new System.Windows.Forms.Panel();
            this.grpApplyTo.SuspendLayout();
            this.grpAssignType.SuspendLayout();
            this.grpMemberLoad.SuspendLayout();
            this.grpDirection.SuspendLayout();
            this.grpNodalLoad.SuspendLayout();
            this.SuspendLayout();
            // 
            // lblLoadCase
            // 
            this.lblLoadCase.AutoSize = true;
            this.lblLoadCase.Location = new System.Drawing.Point(12, 15);
            this.lblLoadCase.Name = "lblLoadCase";
            this.lblLoadCase.Size = new System.Drawing.Size(61, 13);
            this.lblLoadCase.TabIndex = 0;
            this.lblLoadCase.Text = "Load Case";
            // 
            // cboLoadCases
            // 
            this.cboLoadCases.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboLoadCases.FormattingEnabled = true;
            this.cboLoadCases.Location = new System.Drawing.Point(79, 12);
            this.cboLoadCases.Name = "cboLoadCases";
            this.cboLoadCases.Size = new System.Drawing.Size(204, 21);
            this.cboLoadCases.TabIndex = 1;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(289, 10);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(69, 24);
            this.btnRefresh.TabIndex = 2;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // grpApplyTo
            // 
            this.grpApplyTo.Controls.Add(this.cboGroups);
            this.grpApplyTo.Controls.Add(this.lblGroup);
            this.grpApplyTo.Controls.Add(this.rbGroupSource);
            this.grpApplyTo.Controls.Add(this.rbSelectedSource);
            this.grpApplyTo.Location = new System.Drawing.Point(15, 45);
            this.grpApplyTo.Name = "grpApplyTo";
            this.grpApplyTo.Size = new System.Drawing.Size(343, 76);
            this.grpApplyTo.TabIndex = 3;
            this.grpApplyTo.TabStop = false;
            this.grpApplyTo.Text = "Apply To";
            // 
            // cboGroups
            // 
            this.cboGroups.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboGroups.FormattingEnabled = true;
            this.cboGroups.Location = new System.Drawing.Point(81, 45);
            this.cboGroups.Name = "cboGroups";
            this.cboGroups.Size = new System.Drawing.Size(244, 21);
            this.cboGroups.TabIndex = 3;
            // 
            // lblGroup
            // 
            this.lblGroup.AutoSize = true;
            this.lblGroup.Location = new System.Drawing.Point(15, 48);
            this.lblGroup.Name = "lblGroup";
            this.lblGroup.Size = new System.Drawing.Size(36, 13);
            this.lblGroup.TabIndex = 2;
            this.lblGroup.Text = "Group";
            // 
            // rbGroupSource
            // 
            this.rbGroupSource.AutoSize = true;
            this.rbGroupSource.Location = new System.Drawing.Point(105, 20);
            this.rbGroupSource.Name = "rbGroupSource";
            this.rbGroupSource.Size = new System.Drawing.Size(54, 17);
            this.rbGroupSource.TabIndex = 1;
            this.rbGroupSource.Text = "Group";
            this.rbGroupSource.UseVisualStyleBackColor = true;
            this.rbGroupSource.CheckedChanged += new System.EventHandler(this.rbSource_CheckedChanged);
            // 
            // rbSelectedSource
            // 
            this.rbSelectedSource.AutoSize = true;
            this.rbSelectedSource.Location = new System.Drawing.Point(18, 20);
            this.rbSelectedSource.Name = "rbSelectedSource";
            this.rbSelectedSource.Size = new System.Drawing.Size(67, 17);
            this.rbSelectedSource.TabIndex = 0;
            this.rbSelectedSource.Text = "Selected";
            this.rbSelectedSource.UseVisualStyleBackColor = true;
            this.rbSelectedSource.CheckedChanged += new System.EventHandler(this.rbSource_CheckedChanged);
            // 
            // grpAssignType
            // 
            this.grpAssignType.Controls.Add(this.rbNodalLoad);
            this.grpAssignType.Controls.Add(this.rbMemberLoad);
            this.grpAssignType.Location = new System.Drawing.Point(15, 127);
            this.grpAssignType.Name = "grpAssignType";
            this.grpAssignType.Size = new System.Drawing.Size(343, 49);
            this.grpAssignType.TabIndex = 4;
            this.grpAssignType.TabStop = false;
            this.grpAssignType.Text = "Assign Type";
            // 
            // rbNodalLoad
            // 
            this.rbNodalLoad.AutoSize = true;
            this.rbNodalLoad.Location = new System.Drawing.Point(132, 20);
            this.rbNodalLoad.Name = "rbNodalLoad";
            this.rbNodalLoad.Size = new System.Drawing.Size(79, 17);
            this.rbNodalLoad.TabIndex = 1;
            this.rbNodalLoad.Text = "Nodal Load";
            this.rbNodalLoad.UseVisualStyleBackColor = true;
            this.rbNodalLoad.CheckedChanged += new System.EventHandler(this.rbAssignType_CheckedChanged);
            // 
            // rbMemberLoad
            // 
            this.rbMemberLoad.AutoSize = true;
            this.rbMemberLoad.Location = new System.Drawing.Point(18, 20);
            this.rbMemberLoad.Name = "rbMemberLoad";
            this.rbMemberLoad.Size = new System.Drawing.Size(86, 17);
            this.rbMemberLoad.TabIndex = 0;
            this.rbMemberLoad.Text = "Member Load";
            this.rbMemberLoad.UseVisualStyleBackColor = true;
            this.rbMemberLoad.CheckedChanged += new System.EventHandler(this.rbAssignType_CheckedChanged);
            // 
            // grpMemberLoad
            // 
            this.grpMemberLoad.Controls.Add(this.lblMemberUnit);
            this.grpMemberLoad.Controls.Add(this.txtMemberForce);
            this.grpMemberLoad.Controls.Add(this.lblMemberForce);
            this.grpMemberLoad.Controls.Add(this.grpDirection);
            this.grpMemberLoad.Location = new System.Drawing.Point(15, 182);
            this.grpMemberLoad.Name = "grpMemberLoad";
            this.grpMemberLoad.Size = new System.Drawing.Size(343, 165);
            this.grpMemberLoad.TabIndex = 5;
            this.grpMemberLoad.TabStop = false;
            this.grpMemberLoad.Text = "Member Load";
            // 
            // lblMemberUnit
            // 
            this.lblMemberUnit.AutoSize = true;
            this.lblMemberUnit.Location = new System.Drawing.Point(169, 135);
            this.lblMemberUnit.Name = "lblMemberUnit";
            this.lblMemberUnit.Size = new System.Drawing.Size(31, 13);
            this.lblMemberUnit.TabIndex = 3;
            this.lblMemberUnit.Text = "kN/m";
            // 
            // txtMemberForce
            // 
            this.txtMemberForce.Location = new System.Drawing.Point(103, 132);
            this.txtMemberForce.Name = "txtMemberForce";
            this.txtMemberForce.Size = new System.Drawing.Size(60, 20);
            this.txtMemberForce.TabIndex = 2;
            this.txtMemberForce.Text = "0";
            // 
            // lblMemberForce
            // 
            this.lblMemberForce.AutoSize = true;
            this.lblMemberForce.Location = new System.Drawing.Point(15, 135);
            this.lblMemberForce.Name = "lblMemberForce";
            this.lblMemberForce.Size = new System.Drawing.Size(68, 13);
            this.lblMemberForce.TabIndex = 1;
            this.lblMemberForce.Text = "Uniform Load";
            // 
            // grpDirection
            // 
            this.grpDirection.Controls.Add(this.rbProjectedZ);
            this.grpDirection.Controls.Add(this.rbGlobalZ);
            this.grpDirection.Controls.Add(this.rbLocalZ);
            this.grpDirection.Controls.Add(this.rbProjectedY);
            this.grpDirection.Controls.Add(this.rbGlobalY);
            this.grpDirection.Controls.Add(this.rbLocalY);
            this.grpDirection.Controls.Add(this.rbProjectedX);
            this.grpDirection.Controls.Add(this.rbGlobalX);
            this.grpDirection.Controls.Add(this.rbLocalX);
            this.grpDirection.Location = new System.Drawing.Point(18, 25);
            this.grpDirection.Name = "grpDirection";
            this.grpDirection.Size = new System.Drawing.Size(307, 92);
            this.grpDirection.TabIndex = 0;
            this.grpDirection.TabStop = false;
            this.grpDirection.Text = "Direction";
            // 
            // rbProjectedZ
            // 
            this.rbProjectedZ.AutoSize = true;
            this.rbProjectedZ.Location = new System.Drawing.Point(230, 63);
            this.rbProjectedZ.Name = "rbProjectedZ";
            this.rbProjectedZ.Size = new System.Drawing.Size(38, 17);
            this.rbProjectedZ.TabIndex = 8;
            this.rbProjectedZ.Text = "PZ";
            this.rbProjectedZ.UseVisualStyleBackColor = true;
            // 
            // rbGlobalZ
            // 
            this.rbGlobalZ.AutoSize = true;
            this.rbGlobalZ.Location = new System.Drawing.Point(128, 63);
            this.rbGlobalZ.Name = "rbGlobalZ";
            this.rbGlobalZ.Size = new System.Drawing.Size(40, 17);
            this.rbGlobalZ.TabIndex = 7;
            this.rbGlobalZ.Text = "GZ";
            this.rbGlobalZ.UseVisualStyleBackColor = true;
            // 
            // rbLocalZ
            // 
            this.rbLocalZ.AutoSize = true;
            this.rbLocalZ.Location = new System.Drawing.Point(17, 63);
            this.rbLocalZ.Name = "rbLocalZ";
            this.rbLocalZ.Size = new System.Drawing.Size(64, 17);
            this.rbLocalZ.TabIndex = 6;
            this.rbLocalZ.Text = "Z (Local)";
            this.rbLocalZ.UseVisualStyleBackColor = true;
            // 
            // rbProjectedY
            // 
            this.rbProjectedY.AutoSize = true;
            this.rbProjectedY.Location = new System.Drawing.Point(230, 40);
            this.rbProjectedY.Name = "rbProjectedY";
            this.rbProjectedY.Size = new System.Drawing.Size(38, 17);
            this.rbProjectedY.TabIndex = 5;
            this.rbProjectedY.Text = "PY";
            this.rbProjectedY.UseVisualStyleBackColor = true;
            // 
            // rbGlobalY
            // 
            this.rbGlobalY.AutoSize = true;
            this.rbGlobalY.Location = new System.Drawing.Point(128, 40);
            this.rbGlobalY.Name = "rbGlobalY";
            this.rbGlobalY.Size = new System.Drawing.Size(40, 17);
            this.rbGlobalY.TabIndex = 4;
            this.rbGlobalY.Text = "GY";
            this.rbGlobalY.UseVisualStyleBackColor = true;
            // 
            // rbLocalY
            // 
            this.rbLocalY.AutoSize = true;
            this.rbLocalY.Location = new System.Drawing.Point(17, 40);
            this.rbLocalY.Name = "rbLocalY";
            this.rbLocalY.Size = new System.Drawing.Size(64, 17);
            this.rbLocalY.TabIndex = 3;
            this.rbLocalY.Text = "Y (Local)";
            this.rbLocalY.UseVisualStyleBackColor = true;
            // 
            // rbProjectedX
            // 
            this.rbProjectedX.AutoSize = true;
            this.rbProjectedX.Location = new System.Drawing.Point(230, 17);
            this.rbProjectedX.Name = "rbProjectedX";
            this.rbProjectedX.Size = new System.Drawing.Size(38, 17);
            this.rbProjectedX.TabIndex = 2;
            this.rbProjectedX.Text = "PX";
            this.rbProjectedX.UseVisualStyleBackColor = true;
            // 
            // rbGlobalX
            // 
            this.rbGlobalX.AutoSize = true;
            this.rbGlobalX.Location = new System.Drawing.Point(128, 17);
            this.rbGlobalX.Name = "rbGlobalX";
            this.rbGlobalX.Size = new System.Drawing.Size(40, 17);
            this.rbGlobalX.TabIndex = 1;
            this.rbGlobalX.Text = "GX";
            this.rbGlobalX.UseVisualStyleBackColor = true;
            // 
            // rbLocalX
            // 
            this.rbLocalX.AutoSize = true;
            this.rbLocalX.Location = new System.Drawing.Point(17, 17);
            this.rbLocalX.Name = "rbLocalX";
            this.rbLocalX.Size = new System.Drawing.Size(64, 17);
            this.rbLocalX.TabIndex = 0;
            this.rbLocalX.Text = "X (Local)";
            this.rbLocalX.UseVisualStyleBackColor = true;
            // 
            // grpNodalLoad
            // 
            this.grpNodalLoad.Controls.Add(this.lblMzUnit);
            this.grpNodalLoad.Controls.Add(this.lblMyUnit);
            this.grpNodalLoad.Controls.Add(this.lblMxUnit);
            this.grpNodalLoad.Controls.Add(this.lblFzUnit);
            this.grpNodalLoad.Controls.Add(this.lblFyUnit);
            this.grpNodalLoad.Controls.Add(this.lblFxUnit);
            this.grpNodalLoad.Controls.Add(this.txtMz);
            this.grpNodalLoad.Controls.Add(this.txtMy);
            this.grpNodalLoad.Controls.Add(this.txtMx);
            this.grpNodalLoad.Controls.Add(this.txtFz);
            this.grpNodalLoad.Controls.Add(this.txtFy);
            this.grpNodalLoad.Controls.Add(this.txtFx);
            this.grpNodalLoad.Controls.Add(this.lblMz);
            this.grpNodalLoad.Controls.Add(this.lblMy);
            this.grpNodalLoad.Controls.Add(this.lblMx);
            this.grpNodalLoad.Controls.Add(this.lblFz);
            this.grpNodalLoad.Controls.Add(this.lblFy);
            this.grpNodalLoad.Controls.Add(this.lblFx);
            this.grpNodalLoad.Location = new System.Drawing.Point(15, 353);
            this.grpNodalLoad.Name = "grpNodalLoad";
            this.grpNodalLoad.Size = new System.Drawing.Size(343, 124);
            this.grpNodalLoad.TabIndex = 6;
            this.grpNodalLoad.TabStop = false;
            this.grpNodalLoad.Text = "Nodal Load";
            // 
            // lblMzUnit
            // 
            this.lblMzUnit.AutoSize = true;
            this.lblMzUnit.Location = new System.Drawing.Point(289, 91);
            this.lblMzUnit.Name = "lblMzUnit";
            this.lblMzUnit.Size = new System.Drawing.Size(29, 13);
            this.lblMzUnit.TabIndex = 17;
            this.lblMzUnit.Text = "kNm";
            // 
            // lblMyUnit
            // 
            this.lblMyUnit.AutoSize = true;
            this.lblMyUnit.Location = new System.Drawing.Point(289, 62);
            this.lblMyUnit.Name = "lblMyUnit";
            this.lblMyUnit.Size = new System.Drawing.Size(29, 13);
            this.lblMyUnit.TabIndex = 16;
            this.lblMyUnit.Text = "kNm";
            // 
            // lblMxUnit
            // 
            this.lblMxUnit.AutoSize = true;
            this.lblMxUnit.Location = new System.Drawing.Point(289, 33);
            this.lblMxUnit.Name = "lblMxUnit";
            this.lblMxUnit.Size = new System.Drawing.Size(29, 13);
            this.lblMxUnit.TabIndex = 15;
            this.lblMxUnit.Text = "kNm";
            // 
            // lblFzUnit
            // 
            this.lblFzUnit.AutoSize = true;
            this.lblFzUnit.Location = new System.Drawing.Point(119, 91);
            this.lblFzUnit.Name = "lblFzUnit";
            this.lblFzUnit.Size = new System.Drawing.Size(20, 13);
            this.lblFzUnit.TabIndex = 14;
            this.lblFzUnit.Text = "kN";
            // 
            // lblFyUnit
            // 
            this.lblFyUnit.AutoSize = true;
            this.lblFyUnit.Location = new System.Drawing.Point(119, 62);
            this.lblFyUnit.Name = "lblFyUnit";
            this.lblFyUnit.Size = new System.Drawing.Size(20, 13);
            this.lblFyUnit.TabIndex = 13;
            this.lblFyUnit.Text = "kN";
            // 
            // lblFxUnit
            // 
            this.lblFxUnit.AutoSize = true;
            this.lblFxUnit.Location = new System.Drawing.Point(119, 33);
            this.lblFxUnit.Name = "lblFxUnit";
            this.lblFxUnit.Size = new System.Drawing.Size(20, 13);
            this.lblFxUnit.TabIndex = 12;
            this.lblFxUnit.Text = "kN";
            // 
            // txtMz
            // 
            this.txtMz.Location = new System.Drawing.Point(223, 88);
            this.txtMz.Name = "txtMz";
            this.txtMz.Size = new System.Drawing.Size(60, 20);
            this.txtMz.TabIndex = 11;
            this.txtMz.Text = "0";
            // 
            // txtMy
            // 
            this.txtMy.Location = new System.Drawing.Point(223, 59);
            this.txtMy.Name = "txtMy";
            this.txtMy.Size = new System.Drawing.Size(60, 20);
            this.txtMy.TabIndex = 10;
            this.txtMy.Text = "0";
            // 
            // txtMx
            // 
            this.txtMx.Location = new System.Drawing.Point(223, 30);
            this.txtMx.Name = "txtMx";
            this.txtMx.Size = new System.Drawing.Size(60, 20);
            this.txtMx.TabIndex = 9;
            this.txtMx.Text = "0";
            // 
            // txtFz
            // 
            this.txtFz.Location = new System.Drawing.Point(53, 88);
            this.txtFz.Name = "txtFz";
            this.txtFz.Size = new System.Drawing.Size(60, 20);
            this.txtFz.TabIndex = 8;
            this.txtFz.Text = "0";
            // 
            // txtFy
            // 
            this.txtFy.Location = new System.Drawing.Point(53, 59);
            this.txtFy.Name = "txtFy";
            this.txtFy.Size = new System.Drawing.Size(60, 20);
            this.txtFy.TabIndex = 7;
            this.txtFy.Text = "0";
            // 
            // txtFx
            // 
            this.txtFx.Location = new System.Drawing.Point(53, 30);
            this.txtFx.Name = "txtFx";
            this.txtFx.Size = new System.Drawing.Size(60, 20);
            this.txtFx.TabIndex = 6;
            this.txtFx.Text = "0";
            // 
            // lblMz
            // 
            this.lblMz.AutoSize = true;
            this.lblMz.Location = new System.Drawing.Point(188, 91);
            this.lblMz.Name = "lblMz";
            this.lblMz.Size = new System.Drawing.Size(20, 13);
            this.lblMz.TabIndex = 5;
            this.lblMz.Text = "Mz";
            // 
            // lblMy
            // 
            this.lblMy.AutoSize = true;
            this.lblMy.Location = new System.Drawing.Point(188, 62);
            this.lblMy.Name = "lblMy";
            this.lblMy.Size = new System.Drawing.Size(20, 13);
            this.lblMy.TabIndex = 4;
            this.lblMy.Text = "My";
            // 
            // lblMx
            // 
            this.lblMx.AutoSize = true;
            this.lblMx.Location = new System.Drawing.Point(188, 33);
            this.lblMx.Name = "lblMx";
            this.lblMx.Size = new System.Drawing.Size(20, 13);
            this.lblMx.TabIndex = 3;
            this.lblMx.Text = "Mx";
            // 
            // lblFz
            // 
            this.lblFz.AutoSize = true;
            this.lblFz.Location = new System.Drawing.Point(18, 91);
            this.lblFz.Name = "lblFz";
            this.lblFz.Size = new System.Drawing.Size(17, 13);
            this.lblFz.TabIndex = 2;
            this.lblFz.Text = "Fz";
            // 
            // lblFy
            // 
            this.lblFy.AutoSize = true;
            this.lblFy.Location = new System.Drawing.Point(18, 62);
            this.lblFy.Name = "lblFy";
            this.lblFy.Size = new System.Drawing.Size(17, 13);
            this.lblFy.TabIndex = 1;
            this.lblFy.Text = "Fy";
            // 
            // lblFx
            // 
            this.lblFx.AutoSize = true;
            this.lblFx.Location = new System.Drawing.Point(18, 33);
            this.lblFx.Name = "lblFx";
            this.lblFx.Size = new System.Drawing.Size(17, 13);
            this.lblFx.TabIndex = 0;
            this.lblFx.Text = "Fx";
            // 
            // btnAssign
            // 
            this.btnAssign.Location = new System.Drawing.Point(202, 489);
            this.btnAssign.Name = "btnAssign";
            this.btnAssign.Size = new System.Drawing.Size(75, 28);
            this.btnAssign.TabIndex = 7;
            this.btnAssign.Text = "Assign";
            this.btnAssign.UseVisualStyleBackColor = true;
            this.btnAssign.Click += new System.EventHandler(this.btnAssign_Click);
            // 
            // btnClose
            // 
            this.btnClose.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnClose.Location = new System.Drawing.Point(283, 489);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(75, 28);
            this.btnClose.TabIndex = 8;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = true;
            this.btnClose.Click += new System.EventHandler(this.btnClose_Click);
            // 
            // btnLoadTree
            // 
            this.btnLoadTree.Location = new System.Drawing.Point(15, 489);
            this.btnLoadTree.Name = "btnLoadTree";
            this.btnLoadTree.Size = new System.Drawing.Size(85, 28);
            this.btnLoadTree.TabIndex = 9;
            this.btnLoadTree.Text = "Load Tree";
            this.btnLoadTree.UseVisualStyleBackColor = true;
            this.btnLoadTree.Click += new System.EventHandler(this.btnLoadTree_Click);
            // 
            // pnlLoadTreeHost
            // 
            this.pnlLoadTreeHost.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pnlLoadTreeHost.Location = new System.Drawing.Point(12, 12);
            this.pnlLoadTreeHost.Name = "pnlLoadTreeHost";
            this.pnlLoadTreeHost.Size = new System.Drawing.Size(335, 505);
            this.pnlLoadTreeHost.TabIndex = 10;
            this.pnlLoadTreeHost.Visible = false;
            // 
            // AssignLoadForm
            // 
            this.AcceptButton = this.btnAssign;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnClose;
            this.ClientSize = new System.Drawing.Size(373, 530);
            this.Controls.Add(this.pnlLoadTreeHost);
            this.Controls.Add(this.btnLoadTree);
            this.Controls.Add(this.btnClose);
            this.Controls.Add(this.btnAssign);
            this.Controls.Add(this.grpNodalLoad);
            this.Controls.Add(this.grpMemberLoad);
            this.Controls.Add(this.grpAssignType);
            this.Controls.Add(this.grpApplyTo);
            this.Controls.Add(this.btnRefresh);
            this.Controls.Add(this.cboLoadCases);
            this.Controls.Add(this.lblLoadCase);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AssignLoadForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Assign Load";
            this.grpApplyTo.ResumeLayout(false);
            this.grpApplyTo.PerformLayout();
            this.grpAssignType.ResumeLayout(false);
            this.grpAssignType.PerformLayout();
            this.grpMemberLoad.ResumeLayout(false);
            this.grpMemberLoad.PerformLayout();
            this.grpDirection.ResumeLayout(false);
            this.grpDirection.PerformLayout();
            this.grpNodalLoad.ResumeLayout(false);
            this.grpNodalLoad.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
