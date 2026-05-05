using System;
using System.Windows.Forms;
using System.Drawing;

namespace StaadSteelApp
{
    public enum ExportMode
    {
        Galvanizing,
        MassBreakdown
    }

    public partial class ExportOptionsForm : Form
    {
        public ExportMode SelectedMode { get; private set; } = ExportMode.Galvanizing;
        public bool Confirmed { get; private set; } = false;

        public ExportOptionsForm()
        {
            InitializeComponent();
            rbGalvanizing.Checked = true;
        }

        private void btnExport_Click(object sender, EventArgs e)
        {
            SelectedMode = rbGalvanizing.Checked ? ExportMode.Galvanizing : ExportMode.MassBreakdown;
            
            Confirmed = true;
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            Confirmed = false;
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
