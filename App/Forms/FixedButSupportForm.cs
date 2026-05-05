using System;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace StaadSteelApp
{
    internal sealed class FixedButSupportForm : Form
    {
        private static readonly string[] DofLabels = { "FX", "FY", "FZ", "MX", "MY", "MZ" };
        private static readonly string[] UnitLabels = { "kN/m", "kN/m", "kN/m", "kN-m/deg.", "kN-m/deg.", "kN-m/deg." };

        private readonly CheckBox[] _releaseChecks = new CheckBox[6];
        private readonly TextBox[] _springBoxes = new TextBox[6];

        public FixedButSupportForm()
        {
            Text = "Fixed But Support";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(304, 258);
            TopMost = true;

            var grpRelease = new GroupBox
            {
                Text = "Release",
                Location = new Point(8, 8),
                Size = new Size(88, 170)
            };

            var grpSpring = new GroupBox
            {
                Text = "Define Spring",
                Location = new Point(102, 8),
                Size = new Size(194, 170)
            };

            BuildReleaseInputs(grpRelease);
            BuildSpringInputs(grpSpring);

            var btnApply = new Button
            {
                Text = "Apply",
                Location = new Point(116, 218),
                Size = new Size(80, 28)
            };
            btnApply.Click += BtnApply_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(208, 218),
                Size = new Size(80, 28),
                DialogResult = DialogResult.Cancel
            };

            Controls.Add(grpRelease);
            Controls.Add(grpSpring);
            Controls.Add(btnApply);
            Controls.Add(btnCancel);

            AcceptButton = btnApply;
            CancelButton = btnCancel;
        }

        public int[] ReleaseSpec { get; private set; } = new int[6];

        public double[] SpringSpec { get; private set; } = new double[6];

        private void BuildReleaseInputs(GroupBox parent)
        {
            for (int i = 0; i < DofLabels.Length; i++)
            {
                int top = 24 + (i * 24);

                var chk = new CheckBox
                {
                    Text = DofLabels[i],
                    Location = new Point(12, top),
                    Size = new Size(52, 18)
                };

                _releaseChecks[i] = chk;
                parent.Controls.Add(chk);
            }
        }

        private void BuildSpringInputs(GroupBox parent)
        {
            for (int i = 0; i < DofLabels.Length; i++)
            {
                int top = 24 + (i * 24);

                var lbl = new Label
                {
                    Text = DofLabels[i] + ":",
                    Location = new Point(10, top + 3),
                    Size = new Size(35, 15)
                };

                var txt = new TextBox
                {
                    Location = new Point(47, top),
                    Size = new Size(72, 20),
                    Text = "0"
                };
                txt.Enter += TextBox_EnterSelectAll;

                var unit = new Label
                {
                    Text = UnitLabels[i],
                    Location = new Point(124, top + 3),
                    Size = new Size(64, 15)
                };

                _springBoxes[i] = txt;
                parent.Controls.Add(lbl);
                parent.Controls.Add(txt);
                parent.Controls.Add(unit);
            }
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            var release = new int[6];
            var spring = new double[6];
            bool hasDefinition = false;

            for (int i = 0; i < 6; i++)
            {
                release[i] = _releaseChecks[i].Checked ? 1 : 0;
                if (release[i] != 0)
                    hasDefinition = true;

                string text = (_springBoxes[i].Text ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(text))
                    text = "0";

                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out spring[i]) &&
                    !double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out spring[i]))
                {
                    TopMostMessageBox.ShowWarning($"Please enter a valid value for {DofLabels[i]}.", "Fixed But");
                    _springBoxes[i].Focus();
                    _springBoxes[i].SelectAll();
                    return;
                }

                if (Math.Abs(spring[i]) > 1e-9)
                    hasDefinition = true;
            }

            if (!hasDefinition)
            {
                TopMostMessageBox.ShowWarning(
                    "Please define at least one release or spring stiffness.",
                    "Fixed But");
                return;
            }

            ReleaseSpec = release;
            SpringSpec = spring;
            DialogResult = DialogResult.OK;
            Close();
        }

        private static void TextBox_EnterSelectAll(object sender, EventArgs e)
        {
            if (sender is TextBox textBox)
                textBox.BeginInvoke(new Action(textBox.SelectAll));
        }
    }
}
