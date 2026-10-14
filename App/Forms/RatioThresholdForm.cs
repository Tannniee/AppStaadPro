using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace StaadSteelApp
{
    internal sealed class RatioThresholdForm : Form
    {
        private readonly TextBox _txtThreshold;
        private readonly Button _btnOk;
        private readonly Button _btnCancel;

        internal double Threshold { get; private set; }

        internal RatioThresholdForm(double defaultThreshold)
        {
            Threshold = defaultThreshold;

            Text = "Ratio";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(210, 86);
            MaximizeBox = false;
            MinimizeBox = false;
            TopMost = true;

            var lblThreshold = new Label
            {
                AutoSize = true,
                Location = new Point(12, 15),
                Text = "Threshold"
            };

            _txtThreshold = new TextBox
            {
                Location = new Point(82, 12),
                Size = new Size(110, 20),
                Text = defaultThreshold.ToString("0.###", CultureInfo.InvariantCulture)
            };

            _btnOk = new Button
            {
                DialogResult = DialogResult.None,
                Location = new Point(36, 46),
                Size = new Size(75, 28),
                Text = "OK"
            };
            _btnOk.Click += BtnOk_Click;

            _btnCancel = new Button
            {
                DialogResult = DialogResult.Cancel,
                Location = new Point(117, 46),
                Size = new Size(75, 28),
                Text = "Cancel"
            };

            Controls.Add(lblThreshold);
            Controls.Add(_txtThreshold);
            Controls.Add(_btnOk);
            Controls.Add(_btnCancel);

            AcceptButton = _btnOk;
            CancelButton = _btnCancel;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            _txtThreshold.SelectAll();
            _txtThreshold.Focus();
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (!TryParseThreshold(_txtThreshold.Text, out double threshold))
            {
                TopMostMessageBox.ShowWarning("Please enter a valid ratio threshold.", "Ratio");
                _txtThreshold.Focus();
                _txtThreshold.SelectAll();
                return;
            }

            Threshold = threshold;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static bool TryParseThreshold(string text, out double threshold)
        {
            string raw = (text ?? string.Empty).Trim();
            return (double.TryParse(raw, NumberStyles.Float, CultureInfo.CurrentCulture, out threshold) ||
                    double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out threshold)) &&
                   threshold > 0.0 &&
                   threshold <= 10.0;
        }
    }
}
