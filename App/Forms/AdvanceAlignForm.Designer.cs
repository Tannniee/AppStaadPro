namespace StaadSteelApp
{
    partial class AdvanceAlignForm
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Label _lblTitle;
        private System.Windows.Forms.Label _lblAction;
        private System.Windows.Forms.ComboBox _cboAction;
        private System.Windows.Forms.GroupBox _grpSelection;
        private System.Windows.Forms.Label _lblSelectionInfo;
        private System.Windows.Forms.Label _lblSelectionIds;
        private System.Windows.Forms.Button _btnReloadSelection;
        private System.Windows.Forms.GroupBox _grpAction;
        private System.Windows.Forms.Panel _panelLine;
        private System.Windows.Forms.Label _lblRootNode;
        private System.Windows.Forms.Panel _panelRootModeGroup;
        private System.Windows.Forms.RadioButton _rdoRootFromSelection;
        private System.Windows.Forms.RadioButton _rdoRootManual;
        private System.Windows.Forms.ComboBox _cboRootNode;
        private System.Windows.Forms.TextBox _txtRootNodeManual;
        private System.Windows.Forms.Button _btnPickRootHighlight;
        private System.Windows.Forms.Label _lblKeepAxis;
        private System.Windows.Forms.Panel _panelKeepAxisGroup;
        private System.Windows.Forms.RadioButton _rdoAxisX;
        private System.Windows.Forms.RadioButton _rdoAxisY;
        private System.Windows.Forms.RadioButton _rdoAxisZ;
        private System.Windows.Forms.Label _lblLineNodesTitle;
        private System.Windows.Forms.TextBox _txtLineNodes;
        private System.Windows.Forms.Panel _panelTemplate;
        private System.Windows.Forms.Label _lblTemplateSource;
        private System.Windows.Forms.Panel _panelTemplateSourceGroup;
        private System.Windows.Forms.RadioButton _rdoTemplateFromNode;
        private System.Windows.Forms.RadioButton _rdoTemplateFromCoordinates;
        private System.Windows.Forms.Label _lblTemplateNode;
        private System.Windows.Forms.TextBox _txtTemplateNode;
        private System.Windows.Forms.Label _lblTargetCoords;
        private System.Windows.Forms.Label _lblTargetX;
        private System.Windows.Forms.TextBox _txtTargetX;
        private System.Windows.Forms.Label _lblTargetY;
        private System.Windows.Forms.TextBox _txtTargetY;
        private System.Windows.Forms.Label _lblTargetZ;
        private System.Windows.Forms.TextBox _txtTargetZ;
        private System.Windows.Forms.Label _lblTemplateAxes;
        private System.Windows.Forms.CheckBox _chkAlignX;
        private System.Windows.Forms.CheckBox _chkAlignY;
        private System.Windows.Forms.CheckBox _chkAlignZ;
        private System.Windows.Forms.Panel _panelConnect;
        private System.Windows.Forms.Label _lblConnectMode;
        private System.Windows.Forms.Panel _panelConnectModeGroup;
        private System.Windows.Forms.RadioButton _rdoConnectSingleChain;
        private System.Windows.Forms.RadioButton _rdoConnectSplitByGap;
        private System.Windows.Forms.Label _lblConnectAxes;
        private System.Windows.Forms.Panel _panelConnectAxisGroup;
        private System.Windows.Forms.RadioButton _rdoConnectAxisX;
        private System.Windows.Forms.RadioButton _rdoConnectAxisY;
        private System.Windows.Forms.RadioButton _rdoConnectAxisZ;
        private System.Windows.Forms.Label _lblGapAxis;
        private System.Windows.Forms.Panel _panelGapAxisGroup;
        private System.Windows.Forms.RadioButton _rdoGapAxisX;
        private System.Windows.Forms.RadioButton _rdoGapAxisY;
        private System.Windows.Forms.RadioButton _rdoGapAxisZ;
        private System.Windows.Forms.Label _lblGapLimit;
        private System.Windows.Forms.TextBox _txtGapLimit;
        private System.Windows.Forms.Label _lblGapLimitUnit;
        private System.Windows.Forms.Label _lblHint;
        private System.Windows.Forms.Button _btnApply;
        private System.Windows.Forms.Button _btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this._lblTitle = new System.Windows.Forms.Label();
            this._lblAction = new System.Windows.Forms.Label();
            this._cboAction = new System.Windows.Forms.ComboBox();
            this._grpSelection = new System.Windows.Forms.GroupBox();
            this._lblSelectionInfo = new System.Windows.Forms.Label();
            this._lblSelectionIds = new System.Windows.Forms.Label();
            this._btnReloadSelection = new System.Windows.Forms.Button();
            this._grpAction = new System.Windows.Forms.GroupBox();
            this._panelLine = new System.Windows.Forms.Panel();
            this._lblRootNode = new System.Windows.Forms.Label();
            this._panelRootModeGroup = new System.Windows.Forms.Panel();
            this._rdoRootFromSelection = new System.Windows.Forms.RadioButton();
            this._rdoRootManual = new System.Windows.Forms.RadioButton();
            this._cboRootNode = new System.Windows.Forms.ComboBox();
            this._txtRootNodeManual = new System.Windows.Forms.TextBox();
            this._btnPickRootHighlight = new System.Windows.Forms.Button();
            this._lblKeepAxis = new System.Windows.Forms.Label();
            this._panelKeepAxisGroup = new System.Windows.Forms.Panel();
            this._rdoAxisX = new System.Windows.Forms.RadioButton();
            this._rdoAxisY = new System.Windows.Forms.RadioButton();
            this._rdoAxisZ = new System.Windows.Forms.RadioButton();
            this._lblLineNodesTitle = new System.Windows.Forms.Label();
            this._txtLineNodes = new System.Windows.Forms.TextBox();
            this._panelTemplate = new System.Windows.Forms.Panel();
            this._lblTemplateSource = new System.Windows.Forms.Label();
            this._panelTemplateSourceGroup = new System.Windows.Forms.Panel();
            this._rdoTemplateFromNode = new System.Windows.Forms.RadioButton();
            this._rdoTemplateFromCoordinates = new System.Windows.Forms.RadioButton();
            this._lblTemplateNode = new System.Windows.Forms.Label();
            this._txtTemplateNode = new System.Windows.Forms.TextBox();
            this._lblTargetCoords = new System.Windows.Forms.Label();
            this._lblTargetX = new System.Windows.Forms.Label();
            this._txtTargetX = new System.Windows.Forms.TextBox();
            this._lblTargetY = new System.Windows.Forms.Label();
            this._txtTargetY = new System.Windows.Forms.TextBox();
            this._lblTargetZ = new System.Windows.Forms.Label();
            this._txtTargetZ = new System.Windows.Forms.TextBox();
            this._lblTemplateAxes = new System.Windows.Forms.Label();
            this._chkAlignX = new System.Windows.Forms.CheckBox();
            this._chkAlignY = new System.Windows.Forms.CheckBox();
            this._chkAlignZ = new System.Windows.Forms.CheckBox();
            this._panelConnect = new System.Windows.Forms.Panel();
            this._lblConnectMode = new System.Windows.Forms.Label();
            this._panelConnectModeGroup = new System.Windows.Forms.Panel();
            this._rdoConnectSingleChain = new System.Windows.Forms.RadioButton();
            this._rdoConnectSplitByGap = new System.Windows.Forms.RadioButton();
            this._lblConnectAxes = new System.Windows.Forms.Label();
            this._panelConnectAxisGroup = new System.Windows.Forms.Panel();
            this._rdoConnectAxisX = new System.Windows.Forms.RadioButton();
            this._rdoConnectAxisY = new System.Windows.Forms.RadioButton();
            this._rdoConnectAxisZ = new System.Windows.Forms.RadioButton();
            this._lblGapAxis = new System.Windows.Forms.Label();
            this._panelGapAxisGroup = new System.Windows.Forms.Panel();
            this._rdoGapAxisX = new System.Windows.Forms.RadioButton();
            this._rdoGapAxisY = new System.Windows.Forms.RadioButton();
            this._rdoGapAxisZ = new System.Windows.Forms.RadioButton();
            this._lblGapLimit = new System.Windows.Forms.Label();
            this._txtGapLimit = new System.Windows.Forms.TextBox();
            this._lblGapLimitUnit = new System.Windows.Forms.Label();
            this._lblHint = new System.Windows.Forms.Label();
            this._btnApply = new System.Windows.Forms.Button();
            this._btnCancel = new System.Windows.Forms.Button();
            this._grpSelection.SuspendLayout();
            this._grpAction.SuspendLayout();
            this._panelLine.SuspendLayout();
            this._panelRootModeGroup.SuspendLayout();
            this._panelKeepAxisGroup.SuspendLayout();
            this._panelTemplate.SuspendLayout();
            this._panelTemplateSourceGroup.SuspendLayout();
            this._panelConnect.SuspendLayout();
            this._panelConnectModeGroup.SuspendLayout();
            this._panelConnectAxisGroup.SuspendLayout();
            this._panelGapAxisGroup.SuspendLayout();
            this.SuspendLayout();
            // 
            // _lblTitle
            // 
            this._lblTitle.AutoSize = true;
            this._lblTitle.Font = new System.Drawing.Font("Segoe UI Semibold", 10.5F, System.Drawing.FontStyle.Bold);
            this._lblTitle.Location = new System.Drawing.Point(11, 9);
            this._lblTitle.Name = "_lblTitle";
            this._lblTitle.Size = new System.Drawing.Size(99, 19);
            this._lblTitle.TabIndex = 0;
            this._lblTitle.Text = "Advance Tools";
            this._lblTitle.Click += new System.EventHandler(this._lblTitle_Click);
            // 
            // _lblAction
            // 
            this._lblAction.AutoSize = true;
            this._lblAction.Location = new System.Drawing.Point(13, 37);
            this._lblAction.Name = "_lblAction";
            this._lblAction.Size = new System.Drawing.Size(42, 15);
            this._lblAction.TabIndex = 1;
            this._lblAction.Text = "Action";
            // 
            // _cboAction
            // 
            this._cboAction.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboAction.FormattingEnabled = true;
            this._cboAction.Items.AddRange(new object[] {
            "Align node on line (3 points)",
            "Align selected nodes (template/manual)",
            "Connect selected nodes with beams"});
            this._cboAction.Location = new System.Drawing.Point(73, 33);
            this._cboAction.Name = "_cboAction";
            this._cboAction.Size = new System.Drawing.Size(286, 23);
            this._cboAction.TabIndex = 2;
            this._cboAction.SelectedIndexChanged += new System.EventHandler(this.CboAction_SelectedIndexChanged);
            // 
            // _grpSelection
            // 
            this._grpSelection.Controls.Add(this._lblSelectionInfo);
            this._grpSelection.Controls.Add(this._lblSelectionIds);
            this._grpSelection.Controls.Add(this._btnReloadSelection);
            this._grpSelection.Location = new System.Drawing.Point(13, 62);
            this._grpSelection.Name = "_grpSelection";
            this._grpSelection.Size = new System.Drawing.Size(348, 94);
            this._grpSelection.TabIndex = 3;
            this._grpSelection.TabStop = false;
            this._grpSelection.Text = "Selection";
            // 
            // _lblSelectionInfo
            // 
            this._lblSelectionInfo.AutoSize = true;
            this._lblSelectionInfo.Location = new System.Drawing.Point(14, 28);
            this._lblSelectionInfo.Name = "_lblSelectionInfo";
            this._lblSelectionInfo.Size = new System.Drawing.Size(98, 15);
            this._lblSelectionInfo.TabIndex = 0;
            this._lblSelectionInfo.Text = "Nodes selected: -";
            // 
            // _lblSelectionIds
            // 
            this._lblSelectionIds.AutoSize = true;
            this._lblSelectionIds.Location = new System.Drawing.Point(14, 56);
            this._lblSelectionIds.Name = "_lblSelectionIds";
            this._lblSelectionIds.Size = new System.Drawing.Size(34, 15);
            this._lblSelectionIds.TabIndex = 1;
            this._lblSelectionIds.Text = "IDs: -";
            // 
            // _btnReloadSelection
            // 
            this._btnReloadSelection.Location = new System.Drawing.Point(268, 22);
            this._btnReloadSelection.Name = "_btnReloadSelection";
            this._btnReloadSelection.Size = new System.Drawing.Size(70, 28);
            this._btnReloadSelection.TabIndex = 2;
            this._btnReloadSelection.Text = "Reload";
            this._btnReloadSelection.UseVisualStyleBackColor = true;
            this._btnReloadSelection.Click += new System.EventHandler(this.BtnReloadSelection_Click);
            // 
            // _grpAction
            // 
            this._grpAction.Controls.Add(this._panelLine);
            this._grpAction.Controls.Add(this._panelTemplate);
            this._grpAction.Controls.Add(this._panelConnect);
            this._grpAction.Location = new System.Drawing.Point(13, 162);
            this._grpAction.Name = "_grpAction";
            this._grpAction.Size = new System.Drawing.Size(348, 198);
            this._grpAction.TabIndex = 4;
            this._grpAction.TabStop = false;
            this._grpAction.Text = "Action Settings";
            // 
            // _panelLine
            // 
            this._panelLine.Controls.Add(this._lblRootNode);
            this._panelLine.Controls.Add(this._panelRootModeGroup);
            this._panelLine.Controls.Add(this._cboRootNode);
            this._panelLine.Controls.Add(this._txtRootNodeManual);
            this._panelLine.Controls.Add(this._btnPickRootHighlight);
            this._panelLine.Controls.Add(this._lblKeepAxis);
            this._panelLine.Controls.Add(this._panelKeepAxisGroup);
            this._panelLine.Controls.Add(this._lblLineNodesTitle);
            this._panelLine.Controls.Add(this._txtLineNodes);
            this._panelLine.Location = new System.Drawing.Point(10, 24);
            this._panelLine.Name = "_panelLine";
            this._panelLine.Size = new System.Drawing.Size(328, 119);
            this._panelLine.TabIndex = 0;
            // 
            // _lblRootNode
            // 
            this._lblRootNode.AutoSize = true;
            this._lblRootNode.Location = new System.Drawing.Point(2, 8);
            this._lblRootNode.Name = "_lblRootNode";
            this._lblRootNode.Size = new System.Drawing.Size(62, 15);
            this._lblRootNode.TabIndex = 0;
            this._lblRootNode.Text = "Root node";
            // 
            // _panelRootModeGroup
            // 
            this._panelRootModeGroup.Controls.Add(this._rdoRootFromSelection);
            this._panelRootModeGroup.Controls.Add(this._rdoRootManual);
            this._panelRootModeGroup.Location = new System.Drawing.Point(90, 4);
            this._panelRootModeGroup.Name = "_panelRootModeGroup";
            this._panelRootModeGroup.Size = new System.Drawing.Size(232, 23);
            this._panelRootModeGroup.TabIndex = 1;
            // 
            // _rdoRootFromSelection
            // 
            this._rdoRootFromSelection.AutoSize = true;
            this._rdoRootFromSelection.Checked = true;
            this._rdoRootFromSelection.Location = new System.Drawing.Point(0, 2);
            this._rdoRootFromSelection.Name = "_rdoRootFromSelection";
            this._rdoRootFromSelection.Size = new System.Drawing.Size(99, 19);
            this._rdoRootFromSelection.TabIndex = 0;
            this._rdoRootFromSelection.TabStop = true;
            this._rdoRootFromSelection.Text = "From selected";
            this._rdoRootFromSelection.UseVisualStyleBackColor = true;
            this._rdoRootFromSelection.CheckedChanged += new System.EventHandler(this.RootMode_CheckedChanged);
            // 
            // _rdoRootManual
            // 
            this._rdoRootManual.AutoSize = true;
            this._rdoRootManual.Location = new System.Drawing.Point(104, 2);
            this._rdoRootManual.Name = "_rdoRootManual";
            this._rdoRootManual.Size = new System.Drawing.Size(120, 19);
            this._rdoRootManual.TabIndex = 1;
            this._rdoRootManual.Text = "Manual/Highlight";
            this._rdoRootManual.UseVisualStyleBackColor = true;
            this._rdoRootManual.CheckedChanged += new System.EventHandler(this.RootMode_CheckedChanged);
            // 
            // _cboRootNode
            // 
            this._cboRootNode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this._cboRootNode.FormattingEnabled = true;
            this._cboRootNode.Location = new System.Drawing.Point(90, 28);
            this._cboRootNode.Name = "_cboRootNode";
            this._cboRootNode.Size = new System.Drawing.Size(120, 23);
            this._cboRootNode.TabIndex = 3;
            this._cboRootNode.SelectedIndexChanged += new System.EventHandler(this.CboRootNode_SelectedIndexChanged);
            // 
            // _txtRootNodeManual
            // 
            this._txtRootNodeManual.Enabled = false;
            this._txtRootNodeManual.Location = new System.Drawing.Point(216, 28);
            this._txtRootNodeManual.Name = "_txtRootNodeManual";
            this._txtRootNodeManual.Size = new System.Drawing.Size(50, 23);
            this._txtRootNodeManual.TabIndex = 4;
            this._txtRootNodeManual.TextChanged += new System.EventHandler(this.TxtRootNodeManual_TextChanged);
            // 
            // _btnPickRootHighlight
            // 
            this._btnPickRootHighlight.Enabled = false;
            this._btnPickRootHighlight.Location = new System.Drawing.Point(272, 27);
            this._btnPickRootHighlight.Name = "_btnPickRootHighlight";
            this._btnPickRootHighlight.Size = new System.Drawing.Size(50, 25);
            this._btnPickRootHighlight.TabIndex = 5;
            this._btnPickRootHighlight.Text = "Pick";
            this._btnPickRootHighlight.UseVisualStyleBackColor = true;
            this._btnPickRootHighlight.Click += new System.EventHandler(this.BtnPickRootHighlight_Click);
            // 
            // _lblKeepAxis
            // 
            this._lblKeepAxis.AutoSize = true;
            this._lblKeepAxis.Location = new System.Drawing.Point(2, 58);
            this._lblKeepAxis.Name = "_lblKeepAxis";
            this._lblKeepAxis.Size = new System.Drawing.Size(55, 15);
            this._lblKeepAxis.TabIndex = 6;
            this._lblKeepAxis.Text = "Keep axis";
            // 
            // _panelKeepAxisGroup
            // 
            this._panelKeepAxisGroup.Controls.Add(this._rdoAxisX);
            this._panelKeepAxisGroup.Controls.Add(this._rdoAxisY);
            this._panelKeepAxisGroup.Controls.Add(this._rdoAxisZ);
            this._panelKeepAxisGroup.Location = new System.Drawing.Point(90, 52);
            this._panelKeepAxisGroup.Name = "_panelKeepAxisGroup";
            this._panelKeepAxisGroup.Size = new System.Drawing.Size(140, 23);
            this._panelKeepAxisGroup.TabIndex = 7;
            // 
            // _rdoAxisX
            // 
            this._rdoAxisX.AutoSize = true;
            this._rdoAxisX.Location = new System.Drawing.Point(0, 2);
            this._rdoAxisX.Name = "_rdoAxisX";
            this._rdoAxisX.Size = new System.Drawing.Size(32, 19);
            this._rdoAxisX.TabIndex = 0;
            this._rdoAxisX.Text = "X";
            this._rdoAxisX.UseVisualStyleBackColor = true;
            // 
            // _rdoAxisY
            // 
            this._rdoAxisY.AutoSize = true;
            this._rdoAxisY.Checked = true;
            this._rdoAxisY.Location = new System.Drawing.Point(50, 2);
            this._rdoAxisY.Name = "_rdoAxisY";
            this._rdoAxisY.Size = new System.Drawing.Size(32, 19);
            this._rdoAxisY.TabIndex = 1;
            this._rdoAxisY.TabStop = true;
            this._rdoAxisY.Text = "Y";
            this._rdoAxisY.UseVisualStyleBackColor = true;
            // 
            // _rdoAxisZ
            // 
            this._rdoAxisZ.AutoSize = true;
            this._rdoAxisZ.Location = new System.Drawing.Point(100, 2);
            this._rdoAxisZ.Name = "_rdoAxisZ";
            this._rdoAxisZ.Size = new System.Drawing.Size(32, 19);
            this._rdoAxisZ.TabIndex = 2;
            this._rdoAxisZ.Text = "Z";
            this._rdoAxisZ.UseVisualStyleBackColor = true;
            // 
            // _lblLineNodesTitle
            // 
            this._lblLineNodesTitle.AutoSize = true;
            this._lblLineNodesTitle.Location = new System.Drawing.Point(2, 89);
            this._lblLineNodesTitle.Name = "_lblLineNodesTitle";
            this._lblLineNodesTitle.Size = new System.Drawing.Size(64, 15);
            this._lblLineNodesTitle.TabIndex = 10;
            this._lblLineNodesTitle.Text = "Line nodes";
            // 
            // _txtLineNodes
            // 
            this._txtLineNodes.BackColor = System.Drawing.Color.WhiteSmoke;
            this._txtLineNodes.Location = new System.Drawing.Point(90, 85);
            this._txtLineNodes.Name = "_txtLineNodes";
            this._txtLineNodes.ReadOnly = true;
            this._txtLineNodes.Size = new System.Drawing.Size(140, 23);
            this._txtLineNodes.TabIndex = 11;
            // 
            // _panelTemplate
            // 
            this._panelTemplate.Controls.Add(this._lblTemplateSource);
            this._panelTemplate.Controls.Add(this._panelTemplateSourceGroup);
            this._panelTemplate.Controls.Add(this._lblTemplateNode);
            this._panelTemplate.Controls.Add(this._txtTemplateNode);
            this._panelTemplate.Controls.Add(this._lblTargetCoords);
            this._panelTemplate.Controls.Add(this._lblTargetX);
            this._panelTemplate.Controls.Add(this._txtTargetX);
            this._panelTemplate.Controls.Add(this._lblTargetY);
            this._panelTemplate.Controls.Add(this._txtTargetY);
            this._panelTemplate.Controls.Add(this._lblTargetZ);
            this._panelTemplate.Controls.Add(this._txtTargetZ);
            this._panelTemplate.Controls.Add(this._lblTemplateAxes);
            this._panelTemplate.Controls.Add(this._chkAlignX);
            this._panelTemplate.Controls.Add(this._chkAlignY);
            this._panelTemplate.Controls.Add(this._chkAlignZ);
            this._panelTemplate.Location = new System.Drawing.Point(10, 24);
            this._panelTemplate.Name = "_panelTemplate";
            this._panelTemplate.Size = new System.Drawing.Size(328, 124);
            this._panelTemplate.TabIndex = 1;
            // 
            // _lblTemplateSource
            // 
            this._lblTemplateSource.AutoSize = true;
            this._lblTemplateSource.Location = new System.Drawing.Point(2, 8);
            this._lblTemplateSource.Name = "_lblTemplateSource";
            this._lblTemplateSource.Size = new System.Drawing.Size(43, 15);
            this._lblTemplateSource.TabIndex = 0;
            this._lblTemplateSource.Text = "Source";
            // 
            // _panelTemplateSourceGroup
            // 
            this._panelTemplateSourceGroup.Controls.Add(this._rdoTemplateFromNode);
            this._panelTemplateSourceGroup.Controls.Add(this._rdoTemplateFromCoordinates);
            this._panelTemplateSourceGroup.Location = new System.Drawing.Point(90, 2);
            this._panelTemplateSourceGroup.Name = "_panelTemplateSourceGroup";
            this._panelTemplateSourceGroup.Size = new System.Drawing.Size(220, 23);
            this._panelTemplateSourceGroup.TabIndex = 1;
            // 
            // _rdoTemplateFromNode
            // 
            this._rdoTemplateFromNode.AutoSize = true;
            this._rdoTemplateFromNode.Checked = true;
            this._rdoTemplateFromNode.Location = new System.Drawing.Point(0, 2);
            this._rdoTemplateFromNode.Name = "_rdoTemplateFromNode";
            this._rdoTemplateFromNode.Size = new System.Drawing.Size(104, 19);
            this._rdoTemplateFromNode.TabIndex = 0;
            this._rdoTemplateFromNode.TabStop = true;
            this._rdoTemplateFromNode.Text = "Template node";
            this._rdoTemplateFromNode.UseVisualStyleBackColor = true;
            this._rdoTemplateFromNode.CheckedChanged += new System.EventHandler(this.TemplateSource_CheckedChanged);
            // 
            // _rdoTemplateFromCoordinates
            // 
            this._rdoTemplateFromCoordinates.AutoSize = true;
            this._rdoTemplateFromCoordinates.Location = new System.Drawing.Point(116, 2);
            this._rdoTemplateFromCoordinates.Name = "_rdoTemplateFromCoordinates";
            this._rdoTemplateFromCoordinates.Size = new System.Drawing.Size(89, 19);
            this._rdoTemplateFromCoordinates.TabIndex = 1;
            this._rdoTemplateFromCoordinates.Text = "Manual XYZ";
            this._rdoTemplateFromCoordinates.UseVisualStyleBackColor = true;
            this._rdoTemplateFromCoordinates.CheckedChanged += new System.EventHandler(this.TemplateSource_CheckedChanged);
            // 
            // _lblTemplateNode
            // 
            this._lblTemplateNode.AutoSize = true;
            this._lblTemplateNode.Location = new System.Drawing.Point(2, 39);
            this._lblTemplateNode.Name = "_lblTemplateNode";
            this._lblTemplateNode.Size = new System.Drawing.Size(86, 15);
            this._lblTemplateNode.TabIndex = 2;
            this._lblTemplateNode.Text = "Template node";
            // 
            // _txtTemplateNode
            // 
            this._txtTemplateNode.Location = new System.Drawing.Point(90, 35);
            this._txtTemplateNode.Name = "_txtTemplateNode";
            this._txtTemplateNode.Size = new System.Drawing.Size(232, 23);
            this._txtTemplateNode.TabIndex = 3;
            // 
            // _lblTargetCoords
            // 
            this._lblTargetCoords.AutoSize = true;
            this._lblTargetCoords.Enabled = false;
            this._lblTargetCoords.Location = new System.Drawing.Point(2, 72);
            this._lblTargetCoords.Name = "_lblTargetCoords";
            this._lblTargetCoords.Size = new System.Drawing.Size(70, 15);
            this._lblTargetCoords.TabIndex = 4;
            this._lblTargetCoords.Text = "New coords";
            // 
            // _lblTargetX
            // 
            this._lblTargetX.AutoSize = true;
            this._lblTargetX.Enabled = false;
            this._lblTargetX.Location = new System.Drawing.Point(90, 72);
            this._lblTargetX.Name = "_lblTargetX";
            this._lblTargetX.Size = new System.Drawing.Size(14, 15);
            this._lblTargetX.TabIndex = 5;
            this._lblTargetX.Text = "X";
            // 
            // _txtTargetX
            // 
            this._txtTargetX.Enabled = false;
            this._txtTargetX.Location = new System.Drawing.Point(106, 68);
            this._txtTargetX.Name = "_txtTargetX";
            this._txtTargetX.Size = new System.Drawing.Size(56, 23);
            this._txtTargetX.TabIndex = 6;
            // 
            // _lblTargetY
            // 
            this._lblTargetY.AutoSize = true;
            this._lblTargetY.Enabled = false;
            this._lblTargetY.Location = new System.Drawing.Point(164, 72);
            this._lblTargetY.Name = "_lblTargetY";
            this._lblTargetY.Size = new System.Drawing.Size(14, 15);
            this._lblTargetY.TabIndex = 7;
            this._lblTargetY.Text = "Y";
            // 
            // _txtTargetY
            // 
            this._txtTargetY.Enabled = false;
            this._txtTargetY.Location = new System.Drawing.Point(180, 68);
            this._txtTargetY.Name = "_txtTargetY";
            this._txtTargetY.Size = new System.Drawing.Size(56, 23);
            this._txtTargetY.TabIndex = 8;
            // 
            // _lblTargetZ
            // 
            this._lblTargetZ.AutoSize = true;
            this._lblTargetZ.Enabled = false;
            this._lblTargetZ.Location = new System.Drawing.Point(238, 72);
            this._lblTargetZ.Name = "_lblTargetZ";
            this._lblTargetZ.Size = new System.Drawing.Size(14, 15);
            this._lblTargetZ.TabIndex = 9;
            this._lblTargetZ.Text = "Z";
            // 
            // _txtTargetZ
            // 
            this._txtTargetZ.Enabled = false;
            this._txtTargetZ.Location = new System.Drawing.Point(254, 68);
            this._txtTargetZ.Name = "_txtTargetZ";
            this._txtTargetZ.Size = new System.Drawing.Size(46, 23);
            this._txtTargetZ.TabIndex = 10;
            // 
            // _lblTemplateAxes
            // 
            this._lblTemplateAxes.AutoSize = true;
            this._lblTemplateAxes.Location = new System.Drawing.Point(2, 104);
            this._lblTemplateAxes.Name = "_lblTemplateAxes";
            this._lblTemplateAxes.Size = new System.Drawing.Size(60, 15);
            this._lblTemplateAxes.TabIndex = 11;
            this._lblTemplateAxes.Text = "Align axes";
            // 
            // _chkAlignX
            // 
            this._chkAlignX.AutoSize = true;
            this._chkAlignX.Checked = true;
            this._chkAlignX.CheckState = System.Windows.Forms.CheckState.Checked;
            this._chkAlignX.Location = new System.Drawing.Point(90, 102);
            this._chkAlignX.Name = "_chkAlignX";
            this._chkAlignX.Size = new System.Drawing.Size(33, 19);
            this._chkAlignX.TabIndex = 12;
            this._chkAlignX.Text = "X";
            this._chkAlignX.UseVisualStyleBackColor = true;
            // 
            // _chkAlignY
            // 
            this._chkAlignY.AutoSize = true;
            this._chkAlignY.Location = new System.Drawing.Point(140, 102);
            this._chkAlignY.Name = "_chkAlignY";
            this._chkAlignY.Size = new System.Drawing.Size(33, 19);
            this._chkAlignY.TabIndex = 13;
            this._chkAlignY.Text = "Y";
            this._chkAlignY.UseVisualStyleBackColor = true;
            // 
            // _chkAlignZ
            // 
            this._chkAlignZ.AutoSize = true;
            this._chkAlignZ.Location = new System.Drawing.Point(190, 102);
            this._chkAlignZ.Name = "_chkAlignZ";
            this._chkAlignZ.Size = new System.Drawing.Size(33, 19);
            this._chkAlignZ.TabIndex = 14;
            this._chkAlignZ.Text = "Z";
            this._chkAlignZ.UseVisualStyleBackColor = true;
            // 
            // _panelConnect
            // 
            this._panelConnect.Controls.Add(this._lblConnectMode);
            this._panelConnect.Controls.Add(this._panelConnectModeGroup);
            this._panelConnect.Controls.Add(this._lblConnectAxes);
            this._panelConnect.Controls.Add(this._panelConnectAxisGroup);
            this._panelConnect.Controls.Add(this._lblGapAxis);
            this._panelConnect.Controls.Add(this._panelGapAxisGroup);
            this._panelConnect.Controls.Add(this._lblGapLimit);
            this._panelConnect.Controls.Add(this._txtGapLimit);
            this._panelConnect.Controls.Add(this._lblGapLimitUnit);
            this._panelConnect.Location = new System.Drawing.Point(10, 24);
            this._panelConnect.Name = "_panelConnect";
            this._panelConnect.Size = new System.Drawing.Size(328, 124);
            this._panelConnect.TabIndex = 2;
            // 
            // _lblConnectMode
            // 
            this._lblConnectMode.AutoSize = true;
            this._lblConnectMode.Location = new System.Drawing.Point(2, 8);
            this._lblConnectMode.Name = "_lblConnectMode";
            this._lblConnectMode.Size = new System.Drawing.Size(38, 15);
            this._lblConnectMode.TabIndex = 0;
            this._lblConnectMode.Text = "Mode";
            // 
            // _panelConnectModeGroup
            // 
            this._panelConnectModeGroup.Controls.Add(this._rdoConnectSingleChain);
            this._panelConnectModeGroup.Controls.Add(this._rdoConnectSplitByGap);
            this._panelConnectModeGroup.Location = new System.Drawing.Point(90, 2);
            this._panelConnectModeGroup.Name = "_panelConnectModeGroup";
            this._panelConnectModeGroup.Size = new System.Drawing.Size(206, 23);
            this._panelConnectModeGroup.TabIndex = 1;
            // 
            // _rdoConnectSingleChain
            // 
            this._rdoConnectSingleChain.AutoSize = true;
            this._rdoConnectSingleChain.Checked = true;
            this._rdoConnectSingleChain.Location = new System.Drawing.Point(0, 2);
            this._rdoConnectSingleChain.Name = "_rdoConnectSingleChain";
            this._rdoConnectSingleChain.Size = new System.Drawing.Size(91, 19);
            this._rdoConnectSingleChain.TabIndex = 0;
            this._rdoConnectSingleChain.TabStop = true;
            this._rdoConnectSingleChain.Text = "Single Chain";
            this._rdoConnectSingleChain.UseVisualStyleBackColor = true;
            this._rdoConnectSingleChain.CheckedChanged += new System.EventHandler(this.ConnectMode_CheckedChanged);
            // 
            // _rdoConnectSplitByGap
            // 
            this._rdoConnectSplitByGap.AutoSize = true;
            this._rdoConnectSplitByGap.Location = new System.Drawing.Point(105, 2);
            this._rdoConnectSplitByGap.Name = "_rdoConnectSplitByGap";
            this._rdoConnectSplitByGap.Size = new System.Drawing.Size(88, 19);
            this._rdoConnectSplitByGap.TabIndex = 1;
            this._rdoConnectSplitByGap.Text = "Split by Gap";
            this._rdoConnectSplitByGap.UseVisualStyleBackColor = true;
            this._rdoConnectSplitByGap.CheckedChanged += new System.EventHandler(this.ConnectMode_CheckedChanged);
            // 
            // _lblConnectAxes
            // 
            this._lblConnectAxes.AutoSize = true;
            this._lblConnectAxes.Location = new System.Drawing.Point(2, 37);
            this._lblConnectAxes.Name = "_lblConnectAxes";
            this._lblConnectAxes.Size = new System.Drawing.Size(50, 15);
            this._lblConnectAxes.TabIndex = 2;
            this._lblConnectAxes.Text = "Sort axis";
            // 
            // _panelConnectAxisGroup
            // 
            this._panelConnectAxisGroup.Controls.Add(this._rdoConnectAxisX);
            this._panelConnectAxisGroup.Controls.Add(this._rdoConnectAxisY);
            this._panelConnectAxisGroup.Controls.Add(this._rdoConnectAxisZ);
            this._panelConnectAxisGroup.Location = new System.Drawing.Point(90, 31);
            this._panelConnectAxisGroup.Name = "_panelConnectAxisGroup";
            this._panelConnectAxisGroup.Size = new System.Drawing.Size(140, 23);
            this._panelConnectAxisGroup.TabIndex = 3;
            // 
            // _rdoConnectAxisX
            // 
            this._rdoConnectAxisX.AutoSize = true;
            this._rdoConnectAxisX.Checked = true;
            this._rdoConnectAxisX.Location = new System.Drawing.Point(0, 2);
            this._rdoConnectAxisX.Name = "_rdoConnectAxisX";
            this._rdoConnectAxisX.Size = new System.Drawing.Size(32, 19);
            this._rdoConnectAxisX.TabIndex = 0;
            this._rdoConnectAxisX.TabStop = true;
            this._rdoConnectAxisX.Text = "X";
            this._rdoConnectAxisX.UseVisualStyleBackColor = true;
            // 
            // _rdoConnectAxisY
            // 
            this._rdoConnectAxisY.AutoSize = true;
            this._rdoConnectAxisY.Location = new System.Drawing.Point(50, 2);
            this._rdoConnectAxisY.Name = "_rdoConnectAxisY";
            this._rdoConnectAxisY.Size = new System.Drawing.Size(32, 19);
            this._rdoConnectAxisY.TabIndex = 1;
            this._rdoConnectAxisY.Text = "Y";
            this._rdoConnectAxisY.UseVisualStyleBackColor = true;
            // 
            // _rdoConnectAxisZ
            // 
            this._rdoConnectAxisZ.AutoSize = true;
            this._rdoConnectAxisZ.Location = new System.Drawing.Point(100, 2);
            this._rdoConnectAxisZ.Name = "_rdoConnectAxisZ";
            this._rdoConnectAxisZ.Size = new System.Drawing.Size(32, 19);
            this._rdoConnectAxisZ.TabIndex = 2;
            this._rdoConnectAxisZ.Text = "Z";
            this._rdoConnectAxisZ.UseVisualStyleBackColor = true;
            // 
            // _lblGapAxis
            // 
            this._lblGapAxis.AutoSize = true;
            this._lblGapAxis.Location = new System.Drawing.Point(2, 66);
            this._lblGapAxis.Name = "_lblGapAxis";
            this._lblGapAxis.Size = new System.Drawing.Size(50, 15);
            this._lblGapAxis.TabIndex = 4;
            this._lblGapAxis.Text = "Gap axis";
            // 
            // _panelGapAxisGroup
            // 
            this._panelGapAxisGroup.Controls.Add(this._rdoGapAxisX);
            this._panelGapAxisGroup.Controls.Add(this._rdoGapAxisY);
            this._panelGapAxisGroup.Controls.Add(this._rdoGapAxisZ);
            this._panelGapAxisGroup.Location = new System.Drawing.Point(90, 60);
            this._panelGapAxisGroup.Name = "_panelGapAxisGroup";
            this._panelGapAxisGroup.Size = new System.Drawing.Size(140, 23);
            this._panelGapAxisGroup.TabIndex = 5;
            // 
            // _rdoGapAxisX
            // 
            this._rdoGapAxisX.AutoSize = true;
            this._rdoGapAxisX.Checked = true;
            this._rdoGapAxisX.Location = new System.Drawing.Point(0, 2);
            this._rdoGapAxisX.Name = "_rdoGapAxisX";
            this._rdoGapAxisX.Size = new System.Drawing.Size(32, 19);
            this._rdoGapAxisX.TabIndex = 0;
            this._rdoGapAxisX.TabStop = true;
            this._rdoGapAxisX.Text = "X";
            this._rdoGapAxisX.UseVisualStyleBackColor = true;
            // 
            // _rdoGapAxisY
            // 
            this._rdoGapAxisY.AutoSize = true;
            this._rdoGapAxisY.Location = new System.Drawing.Point(50, 2);
            this._rdoGapAxisY.Name = "_rdoGapAxisY";
            this._rdoGapAxisY.Size = new System.Drawing.Size(32, 19);
            this._rdoGapAxisY.TabIndex = 1;
            this._rdoGapAxisY.Text = "Y";
            this._rdoGapAxisY.UseVisualStyleBackColor = true;
            // 
            // _rdoGapAxisZ
            // 
            this._rdoGapAxisZ.AutoSize = true;
            this._rdoGapAxisZ.Location = new System.Drawing.Point(100, 2);
            this._rdoGapAxisZ.Name = "_rdoGapAxisZ";
            this._rdoGapAxisZ.Size = new System.Drawing.Size(32, 19);
            this._rdoGapAxisZ.TabIndex = 2;
            this._rdoGapAxisZ.Text = "Z";
            this._rdoGapAxisZ.UseVisualStyleBackColor = true;
            // 
            // _lblGapLimit
            // 
            this._lblGapLimit.AutoSize = true;
            this._lblGapLimit.Location = new System.Drawing.Point(2, 95);
            this._lblGapLimit.Name = "_lblGapLimit";
            this._lblGapLimit.Size = new System.Drawing.Size(55, 15);
            this._lblGapLimit.TabIndex = 6;
            this._lblGapLimit.Text = "Gap limit";
            // 
            // _txtGapLimit
            // 
            this._txtGapLimit.Location = new System.Drawing.Point(90, 91);
            this._txtGapLimit.Name = "_txtGapLimit";
            this._txtGapLimit.Size = new System.Drawing.Size(74, 23);
            this._txtGapLimit.TabIndex = 7;
            this._txtGapLimit.Text = "8";
            // 
            // _lblGapLimitUnit
            // 
            this._lblGapLimitUnit.AutoSize = true;
            this._lblGapLimitUnit.Location = new System.Drawing.Point(170, 95);
            this._lblGapLimitUnit.Name = "_lblGapLimitUnit";
            this._lblGapLimitUnit.Size = new System.Drawing.Size(18, 15);
            this._lblGapLimitUnit.TabIndex = 8;
            this._lblGapLimitUnit.Text = "m";
            // 
            // _lblHint
            // 
            this._lblHint.ForeColor = System.Drawing.Color.DimGray;
            this._lblHint.Location = new System.Drawing.Point(15, 337);
            this._lblHint.Name = "_lblHint";
            this._lblHint.Size = new System.Drawing.Size(344, 30);
            this._lblHint.TabIndex = 5;
            this._lblHint.Text = "-";
            this._lblHint.Click += new System.EventHandler(this._lblHint_Click);
            // 
            // _btnApply
            // 
            this._btnApply.Location = new System.Drawing.Point(198, 362);
            this._btnApply.Name = "_btnApply";
            this._btnApply.Size = new System.Drawing.Size(80, 28);
            this._btnApply.TabIndex = 6;
            this._btnApply.Text = "Apply";
            this._btnApply.UseVisualStyleBackColor = true;
            this._btnApply.Click += new System.EventHandler(this.BtnApply_Click);
            // 
            // _btnCancel
            // 
            this._btnCancel.Location = new System.Drawing.Point(284, 362);
            this._btnCancel.Name = "_btnCancel";
            this._btnCancel.Size = new System.Drawing.Size(78, 28);
            this._btnCancel.TabIndex = 7;
            this._btnCancel.Text = "Cancel";
            this._btnCancel.UseVisualStyleBackColor = true;
            this._btnCancel.Click += new System.EventHandler(this.BtnCancel_Click);
            // 
            // AdvanceAlignForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(248)))), ((int)(((byte)(249)))), ((int)(((byte)(251)))));
            this.ClientSize = new System.Drawing.Size(372, 404);
            this.Controls.Add(this._btnCancel);
            this.Controls.Add(this._btnApply);
            this.Controls.Add(this._lblHint);
            this.Controls.Add(this._grpAction);
            this.Controls.Add(this._grpSelection);
            this.Controls.Add(this._cboAction);
            this.Controls.Add(this._lblAction);
            this.Controls.Add(this._lblTitle);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AdvanceAlignForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Advance";
            this.TopMost = true;
            this._grpSelection.ResumeLayout(false);
            this._grpSelection.PerformLayout();
            this._grpAction.ResumeLayout(false);
            this._panelLine.ResumeLayout(false);
            this._panelLine.PerformLayout();
            this._panelRootModeGroup.ResumeLayout(false);
            this._panelRootModeGroup.PerformLayout();
            this._panelKeepAxisGroup.ResumeLayout(false);
            this._panelKeepAxisGroup.PerformLayout();
            this._panelTemplate.ResumeLayout(false);
            this._panelTemplate.PerformLayout();
            this._panelTemplateSourceGroup.ResumeLayout(false);
            this._panelTemplateSourceGroup.PerformLayout();
            this._panelConnect.ResumeLayout(false);
            this._panelConnect.PerformLayout();
            this._panelConnectModeGroup.ResumeLayout(false);
            this._panelConnectModeGroup.PerformLayout();
            this._panelConnectAxisGroup.ResumeLayout(false);
            this._panelConnectAxisGroup.PerformLayout();
            this._panelGapAxisGroup.ResumeLayout(false);
            this._panelGapAxisGroup.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
    }
}
