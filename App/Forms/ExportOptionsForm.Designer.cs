namespace StaadSteelApp
{
    partial class ExportOptionsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.RadioButton rbGalvanizing;
        private System.Windows.Forms.RadioButton rbMassBreakdown;
        private System.Windows.Forms.Button btnExport;
        private System.Windows.Forms.Button btnCancel;

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
            this.rbGalvanizing = new System.Windows.Forms.RadioButton();
            this.rbMassBreakdown = new System.Windows.Forms.RadioButton();
            this.btnExport = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // rbGalvanizing
            // 
            this.rbGalvanizing.AutoSize = true;
            this.rbGalvanizing.Checked = true;
            this.rbGalvanizing.Location = new System.Drawing.Point(20, 15);
            this.rbGalvanizing.Name = "rbGalvanizing";
            this.rbGalvanizing.Size = new System.Drawing.Size(147, 17);
            this.rbGalvanizing.TabIndex = 0;
            this.rbGalvanizing.TabStop = true;
            this.rbGalvanizing.Text = "Galvanizing (ASTM A123)";
            this.rbGalvanizing.UseVisualStyleBackColor = true;
            // 
            // rbMassBreakdown
            // 
            this.rbMassBreakdown.AutoSize = true;
            this.rbMassBreakdown.Location = new System.Drawing.Point(20, 38);
            this.rbMassBreakdown.Name = "rbMassBreakdown";
            this.rbMassBreakdown.Size = new System.Drawing.Size(120, 17);
            this.rbMassBreakdown.TabIndex = 1;
            this.rbMassBreakdown.Text = "Mass Breakdown";
            this.rbMassBreakdown.UseVisualStyleBackColor = true;
            // 
            // btnExport
            // 
            this.btnExport.Location = new System.Drawing.Point(120, 65);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(75, 28);
            this.btnExport.TabIndex = 2;
            this.btnExport.Text = "Export";
            this.btnExport.UseVisualStyleBackColor = true;
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(205, 65);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 28);
            this.btnCancel.TabIndex = 3;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // ExportOptionsForm
            // 
            this.AcceptButton = this.btnExport;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(300, 105);
            this.Controls.Add(this.rbGalvanizing);
            this.Controls.Add(this.rbMassBreakdown);
            this.Controls.Add(this.btnExport);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ExportOptionsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Export Options";
            this.ResumeLayout(false);
            this.PerformLayout();

        }
    }
}
