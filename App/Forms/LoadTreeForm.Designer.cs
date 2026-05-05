namespace StaadSteelApp
{
    partial class LoadTreeForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Button btnRefresh;
        private System.Windows.Forms.Button btnExpandAll;
        private System.Windows.Forms.Button btnCollapseAll;
        private System.Windows.Forms.TreeView treeLoads;

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
            this.btnRefresh = new System.Windows.Forms.Button();
            this.btnExpandAll = new System.Windows.Forms.Button();
            this.btnCollapseAll = new System.Windows.Forms.Button();
            this.treeLoads = new System.Windows.Forms.TreeView();
            this.SuspendLayout();
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(12, 12);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 27);
            this.btnRefresh.TabIndex = 0;
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // btnExpandAll
            // 
            this.btnExpandAll.Location = new System.Drawing.Point(93, 12);
            this.btnExpandAll.Name = "btnExpandAll";
            this.btnExpandAll.Size = new System.Drawing.Size(82, 27);
            this.btnExpandAll.TabIndex = 1;
            this.btnExpandAll.Text = "Expand All";
            this.btnExpandAll.UseVisualStyleBackColor = true;
            this.btnExpandAll.Click += new System.EventHandler(this.btnExpandAll_Click);
            // 
            // btnCollapseAll
            // 
            this.btnCollapseAll.Location = new System.Drawing.Point(181, 12);
            this.btnCollapseAll.Name = "btnCollapseAll";
            this.btnCollapseAll.Size = new System.Drawing.Size(88, 27);
            this.btnCollapseAll.TabIndex = 2;
            this.btnCollapseAll.Text = "Collapse All";
            this.btnCollapseAll.UseVisualStyleBackColor = true;
            this.btnCollapseAll.Click += new System.EventHandler(this.btnCollapseAll_Click);
            // 
            // treeLoads
            // 
            this.treeLoads.HideSelection = false;
            this.treeLoads.Location = new System.Drawing.Point(12, 49);
            this.treeLoads.Name = "treeLoads";
            this.treeLoads.Size = new System.Drawing.Size(516, 530);
            this.treeLoads.TabIndex = 3;
            this.treeLoads.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.treeLoads_AfterSelect);
            // 
            // LoadTreeForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(540, 591);
            this.Controls.Add(this.treeLoads);
            this.Controls.Add(this.btnCollapseAll);
            this.Controls.Add(this.btnExpandAll);
            this.Controls.Add(this.btnRefresh);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.SizableToolWindow;
            this.MinimizeBox = false;
            this.Name = "LoadTreeForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Load Tree";
            this.ResumeLayout(false);
            this.PerformLayout();
        }
    }
}
