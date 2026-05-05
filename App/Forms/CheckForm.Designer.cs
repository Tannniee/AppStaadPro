namespace StaadSteelApp
{
    partial class CheckForm
    {
        private System.ComponentModel.IContainer components = null;

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
            this.panelTop = new System.Windows.Forms.Panel();
            this.txtTargetRatio = new System.Windows.Forms.TextBox();
            this.lblTargetRatio = new System.Windows.Forms.Label();
            this.btnDetect = new System.Windows.Forms.Button();
            this.chkIsBuiltUp = new System.Windows.Forms.CheckBox();
            this.lblCombo = new System.Windows.Forms.Label();
            this.lblMaterial = new System.Windows.Forms.Label();
            this.lblSection = new System.Windows.Forms.Label();
            this.cboFrame = new System.Windows.Forms.ComboBox();
            this.lblFrame = new System.Windows.Forms.Label();
            this.panelForces = new System.Windows.Forms.Panel();
            this.lblForces = new System.Windows.Forms.Label();
            this.dgvSummary = new System.Windows.Forms.DataGridView();
            this.colCheck = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colSection = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colCapacity = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colDemand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colRatio = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.colStatus = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.splitContainer = new System.Windows.Forms.SplitContainer();
            this.txtDetails = new System.Windows.Forms.RichTextBox();
            this.panelBottom = new System.Windows.Forms.Panel();
            this.lblCompareExtra = new System.Windows.Forms.Label();
            this.lblCompareSub = new System.Windows.Forms.Label();
            this.lblCompare = new System.Windows.Forms.Label();
            this.btnCopyDetail = new System.Windows.Forms.Button();
            this.btnCopy = new System.Windows.Forms.Button();
            this.btnClose = new System.Windows.Forms.Button();
            this.panelTop.SuspendLayout();
            this.panelForces.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).BeginInit();
            this.splitContainer.Panel1.SuspendLayout();
            this.splitContainer.Panel2.SuspendLayout();
            this.splitContainer.SuspendLayout();
            this.panelBottom.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelTop
            // 
            this.panelTop.BackColor = System.Drawing.SystemColors.Control;
            this.panelTop.Controls.Add(this.txtTargetRatio);
            this.panelTop.Controls.Add(this.lblTargetRatio);
            this.panelTop.Controls.Add(this.btnDetect);
            this.panelTop.Controls.Add(this.chkIsBuiltUp);
            this.panelTop.Controls.Add(this.lblCombo);
            this.panelTop.Controls.Add(this.lblMaterial);
            this.panelTop.Controls.Add(this.lblSection);
            this.panelTop.Controls.Add(this.cboFrame);
            this.panelTop.Controls.Add(this.lblFrame);
            this.panelTop.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelTop.Location = new System.Drawing.Point(0, 0);
            this.panelTop.Name = "panelTop";
            this.panelTop.Size = new System.Drawing.Size(620, 60);
            this.panelTop.TabIndex = 0;
            // 
            // txtTargetRatio
            // 
            this.txtTargetRatio.Location = new System.Drawing.Point(280, 10);
            this.txtTargetRatio.Name = "txtTargetRatio";
            this.txtTargetRatio.Size = new System.Drawing.Size(44, 20);
            this.txtTargetRatio.TabIndex = 6;
            this.txtTargetRatio.Text = "0.95";
            // 
            // lblTargetRatio
            // 
            this.lblTargetRatio.AutoSize = true;
            this.lblTargetRatio.Location = new System.Drawing.Point(220, 13);
            this.lblTargetRatio.Name = "lblTargetRatio";
            this.lblTargetRatio.Size = new System.Drawing.Size(61, 13);
            this.lblTargetRatio.TabIndex = 5;
            this.lblTargetRatio.Text = "Target D/C";
            // 
            // btnDetect
            // 
            this.btnDetect.BackColor = System.Drawing.SystemColors.Control;
            this.btnDetect.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.btnDetect.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnDetect.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnDetect.Location = new System.Drawing.Point(330, 8);
            this.btnDetect.Name = "btnDetect";
            this.btnDetect.Size = new System.Drawing.Size(46, 25);
            this.btnDetect.TabIndex = 7;
            this.btnDetect.Text = "Size";
            this.btnDetect.UseVisualStyleBackColor = false;
            // 
            // chkIsBuiltUp
            // 
            this.chkIsBuiltUp.AutoSize = true;
            this.chkIsBuiltUp.Location = new System.Drawing.Point(377, 13);
            this.chkIsBuiltUp.Name = "chkIsBuiltUp";
            this.chkIsBuiltUp.Size = new System.Drawing.Size(109, 17);
            this.chkIsBuiltUp.TabIndex = 7;
            this.chkIsBuiltUp.Text = "Built-up / Welded";
            this.chkIsBuiltUp.UseVisualStyleBackColor = true;
            // 
            // lblCombo
            // 
            this.lblCombo.AutoEllipsis = true;
            this.lblCombo.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblCombo.Location = new System.Drawing.Point(374, 35);
            this.lblCombo.Name = "lblCombo";
            this.lblCombo.Size = new System.Drawing.Size(230, 13);
            this.lblCombo.TabIndex = 4;
            this.lblCombo.Text = "Combo: --";
            // 
            // lblMaterial
            // 
            this.lblMaterial.AutoSize = true;
            this.lblMaterial.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblMaterial.Location = new System.Drawing.Point(217, 35);
            this.lblMaterial.Name = "lblMaterial";
            this.lblMaterial.Size = new System.Drawing.Size(56, 13);
            this.lblMaterial.TabIndex = 3;
            this.lblMaterial.Text = "Material: --";
            // 
            // lblSection
            // 
            this.lblSection.AutoSize = true;
            this.lblSection.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblSection.Location = new System.Drawing.Point(12, 35);
            this.lblSection.Name = "lblSection";
            this.lblSection.Size = new System.Drawing.Size(55, 13);
            this.lblSection.TabIndex = 2;
            this.lblSection.Text = "Section: --";
            // 
            // cboFrame
            // 
            this.cboFrame.BackColor = System.Drawing.SystemColors.Window;
            this.cboFrame.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cboFrame.ForeColor = System.Drawing.SystemColors.ControlText;
            this.cboFrame.FormattingEnabled = true;
            this.cboFrame.Location = new System.Drawing.Point(60, 10);
            this.cboFrame.Name = "cboFrame";
            this.cboFrame.Size = new System.Drawing.Size(150, 21);
            this.cboFrame.TabIndex = 1;
            // 
            // lblFrame
            // 
            this.lblFrame.AutoSize = true;
            this.lblFrame.ForeColor = System.Drawing.SystemColors.ControlText;
            this.lblFrame.Location = new System.Drawing.Point(12, 13);
            this.lblFrame.Name = "lblFrame";
            this.lblFrame.Size = new System.Drawing.Size(39, 13);
            this.lblFrame.TabIndex = 0;
            this.lblFrame.Text = "Frame:";
            // 
            // panelForces
            // 
            this.panelForces.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(240)))), ((int)(((byte)(245)))));
            this.panelForces.Controls.Add(this.lblForces);
            this.panelForces.Dock = System.Windows.Forms.DockStyle.Top;
            this.panelForces.Location = new System.Drawing.Point(0, 60);
            this.panelForces.Name = "panelForces";
            this.panelForces.Size = new System.Drawing.Size(620, 35);
            this.panelForces.TabIndex = 1;
            // 
            // lblForces
            // 
            this.lblForces.AutoSize = true;
            this.lblForces.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblForces.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(102)))), ((int)(((byte)(180)))));
            this.lblForces.Location = new System.Drawing.Point(12, 10);
            this.lblForces.Name = "lblForces";
            this.lblForces.Size = new System.Drawing.Size(392, 14);
            this.lblForces.TabIndex = 0;
            this.lblForces.Text = "P=-- kN   M3=-- kN-m   M2=-- kN-m   V2=-- kN   V3=-- kN";
            // 
            // dgvSummary
            // 
            this.dgvSummary.AllowUserToAddRows = false;
            this.dgvSummary.AllowUserToDeleteRows = false;
            this.dgvSummary.AllowUserToResizeRows = false;
            this.dgvSummary.BackgroundColor = System.Drawing.SystemColors.Window;
            this.dgvSummary.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.dgvSummary.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.SingleHorizontal;
            this.dgvSummary.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvSummary.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.colCheck,
            this.colSection,
            this.colCapacity,
            this.colDemand,
            this.colRatio,
            this.colStatus});
            this.dgvSummary.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvSummary.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(200)))), ((int)(((byte)(200)))));
            this.dgvSummary.Location = new System.Drawing.Point(0, 0);
            this.dgvSummary.MultiSelect = false;
            this.dgvSummary.Name = "dgvSummary";
            this.dgvSummary.ReadOnly = true;
            this.dgvSummary.RowHeadersVisible = false;
            this.dgvSummary.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvSummary.Size = new System.Drawing.Size(620, 161);
            this.dgvSummary.TabIndex = 2;
            // 
            // colCheck
            // 
            this.colCheck.HeaderText = "Check";
            this.colCheck.Name = "colCheck";
            this.colCheck.ReadOnly = true;
            this.colCheck.Width = 180;
            // 
            // colSection
            // 
            this.colSection.HeaderText = "Section";
            this.colSection.Name = "colSection";
            this.colSection.ReadOnly = true;
            this.colSection.Width = 80;
            // 
            // colCapacity
            // 
            this.colCapacity.HeaderText = "Capacity";
            this.colCapacity.Name = "colCapacity";
            this.colCapacity.ReadOnly = true;
            // 
            // colDemand
            // 
            this.colDemand.HeaderText = "Demand";
            this.colDemand.Name = "colDemand";
            this.colDemand.ReadOnly = true;
            // 
            // colRatio
            // 
            this.colRatio.HeaderText = "D/C Ratio";
            this.colRatio.Name = "colRatio";
            this.colRatio.ReadOnly = true;
            this.colRatio.Width = 80;
            // 
            // colStatus
            // 
            this.colStatus.HeaderText = "Status";
            this.colStatus.Name = "colStatus";
            this.colStatus.ReadOnly = true;
            this.colStatus.Width = 80;
            // 
            // splitContainer
            // 
            this.splitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer.Location = new System.Drawing.Point(0, 95);
            this.splitContainer.Name = "splitContainer";
            this.splitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            this.splitContainer.Panel1.Controls.Add(this.dgvSummary);
            // 
            // splitContainer.Panel2
            // 
            this.splitContainer.Panel2.Controls.Add(this.txtDetails);
            this.splitContainer.Size = new System.Drawing.Size(620, 427);
            this.splitContainer.SplitterDistance = 161;
            this.splitContainer.TabIndex = 3;
            // 
            // txtDetails
            // 
            this.txtDetails.BackColor = System.Drawing.SystemColors.Window;
            this.txtDetails.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.txtDetails.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtDetails.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtDetails.ForeColor = System.Drawing.SystemColors.ControlText;
            this.txtDetails.Location = new System.Drawing.Point(0, 0);
            this.txtDetails.Name = "txtDetails";
            this.txtDetails.ReadOnly = true;
            this.txtDetails.Size = new System.Drawing.Size(620, 262);
            this.txtDetails.TabIndex = 0;
            this.txtDetails.Text = "";
            // 
            // panelBottom
            // 
            this.panelBottom.BackColor = System.Drawing.SystemColors.Control;
            this.panelBottom.Controls.Add(this.lblCompareExtra);
            this.panelBottom.Controls.Add(this.lblCompareSub);
            this.panelBottom.Controls.Add(this.lblCompare);
            this.panelBottom.Controls.Add(this.btnCopyDetail);
            this.panelBottom.Controls.Add(this.btnCopy);
            this.panelBottom.Controls.Add(this.btnClose);
            this.panelBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.panelBottom.Location = new System.Drawing.Point(0, 522);
            this.panelBottom.Name = "panelBottom";
            this.panelBottom.Size = new System.Drawing.Size(620, 97);
            this.panelBottom.TabIndex = 4;
            // 
            // lblCompareExtra
            // 
            this.lblCompareExtra.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCompareExtra.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(120)))), ((int)(((byte)(90)))), ((int)(((byte)(20)))));
            this.lblCompareExtra.Location = new System.Drawing.Point(12, 73);
            this.lblCompareExtra.Name = "lblCompareExtra";
            this.lblCompareExtra.Size = new System.Drawing.Size(403, 18);
            this.lblCompareExtra.TabIndex = 5;
            this.lblCompareExtra.Text = "Slenderness: --";
            // 
            // lblCompareSub
            // 
            this.lblCompareSub.Font = new System.Drawing.Font("Segoe UI", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCompareSub.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(102)))), ((int)(((byte)(180)))));
            this.lblCompareSub.Location = new System.Drawing.Point(217, 7);
            this.lblCompareSub.Name = "lblCompareSub";
            this.lblCompareSub.Size = new System.Drawing.Size(198, 58);
            this.lblCompareSub.TabIndex = 4;
            this.lblCompareSub.Text = "@ Overall";
            // 
            // lblCompare
            // 
            this.lblCompare.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblCompare.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(150)))), ((int)(((byte)(100)))));
            this.lblCompare.Location = new System.Drawing.Point(12, 6);
            this.lblCompare.Name = "lblCompare";
            this.lblCompare.Size = new System.Drawing.Size(198, 60);
            this.lblCompare.TabIndex = 2;
            this.lblCompare.Text = "@ LC --";
            // 
            // btnCopyDetail
            // 
            this.btnCopyDetail.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopyDetail.BackColor = System.Drawing.SystemColors.Control;
            this.btnCopyDetail.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.btnCopyDetail.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopyDetail.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnCopyDetail.Location = new System.Drawing.Point(422, 47);
            this.btnCopyDetail.Name = "btnCopyDetail";
            this.btnCopyDetail.Size = new System.Drawing.Size(90, 25);
            this.btnCopyDetail.TabIndex = 3;
            this.btnCopyDetail.Text = "Copy Detail";
            this.btnCopyDetail.UseVisualStyleBackColor = false;
            // 
            // btnCopy
            // 
            this.btnCopy.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCopy.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(122)))), ((int)(((byte)(204)))));
            this.btnCopy.FlatAppearance.BorderSize = 0;
            this.btnCopy.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCopy.ForeColor = System.Drawing.Color.White;
            this.btnCopy.Location = new System.Drawing.Point(518, 47);
            this.btnCopy.Name = "btnCopy";
            this.btnCopy.Size = new System.Drawing.Size(90, 25);
            this.btnCopy.TabIndex = 1;
            this.btnCopy.Text = "Copy Report";
            this.btnCopy.UseVisualStyleBackColor = false;
            // 
            // btnClose
            // 
            this.btnClose.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnClose.BackColor = System.Drawing.SystemColors.Control;
            this.btnClose.FlatAppearance.BorderColor = System.Drawing.Color.Gray;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnClose.Location = new System.Drawing.Point(518, 16);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(90, 25);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "Close";
            this.btnClose.UseVisualStyleBackColor = false;
            // 
            // CheckForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.ClientSize = new System.Drawing.Size(620, 619);
            this.Controls.Add(this.splitContainer);
            this.Controls.Add(this.panelBottom);
            this.Controls.Add(this.panelForces);
            this.Controls.Add(this.panelTop);
            this.MaximumSize = new System.Drawing.Size(636, 2000);
            this.MinimumSize = new System.Drawing.Size(636, 400);
            this.Name = "CheckForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "AISC 360-16 Design Check";
            this.TopMost = true;
            this.panelTop.ResumeLayout(false);
            this.panelTop.PerformLayout();
            this.panelForces.ResumeLayout(false);
            this.panelForces.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvSummary)).EndInit();
            this.splitContainer.Panel1.ResumeLayout(false);
            this.splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer)).EndInit();
            this.splitContainer.ResumeLayout(false);
            this.panelBottom.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panelTop;
        private System.Windows.Forms.Label lblFrame;
        private System.Windows.Forms.ComboBox cboFrame;
        private System.Windows.Forms.Label lblSection;
        private System.Windows.Forms.Label lblMaterial;
        private System.Windows.Forms.Label lblCombo;
        private System.Windows.Forms.Panel panelForces;
        private System.Windows.Forms.Label lblForces;
        private System.Windows.Forms.DataGridView dgvSummary;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCheck;
        private System.Windows.Forms.DataGridViewTextBoxColumn colSection;
        private System.Windows.Forms.DataGridViewTextBoxColumn colCapacity;
        private System.Windows.Forms.DataGridViewTextBoxColumn colDemand;
        private System.Windows.Forms.DataGridViewTextBoxColumn colRatio;
        private System.Windows.Forms.DataGridViewTextBoxColumn colStatus;
        private System.Windows.Forms.SplitContainer splitContainer;
        private System.Windows.Forms.RichTextBox txtDetails;
        private System.Windows.Forms.Panel panelBottom;
        private System.Windows.Forms.Button btnClose;
        private System.Windows.Forms.Button btnCopy;
        private System.Windows.Forms.Button btnCopyDetail;
        private System.Windows.Forms.Label lblCompare;
        private System.Windows.Forms.Label lblCompareSub;
        private System.Windows.Forms.Label lblCompareExtra;
        private System.Windows.Forms.Button btnDetect;
        private System.Windows.Forms.CheckBox chkIsBuiltUp;
        private System.Windows.Forms.TextBox txtTargetRatio;
        private System.Windows.Forms.Label lblTargetRatio;
    }
}
