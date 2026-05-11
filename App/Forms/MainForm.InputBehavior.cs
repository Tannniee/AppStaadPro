using System;
using System.Windows.Forms;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void grpDimensions_Enter(object sender, EventArgs e)
        {
        }

        private void SetupInputFields()
        {
            var boxes = new[] { txtH1, txtH2, txtTw, txtBf, txtTf, txtBfb, txtTfb, txtDis };
            foreach (var box in boxes)
            {
                SetupNumericBox(box);
            }
        }

        private void SetupNumericBox(TextBox box)
        {
            box.Enter += Box_Enter;
            box.KeyPress += Box_KeyPress;
            box.Click += Box_Click;
        }

        private void Box_Enter(object sender, EventArgs e)
        {
            var box = sender as TextBox;
            if (box == null)
                return;

            BeginInvoke((Action)delegate { box.SelectAll(); });
        }

        private void Box_Click(object sender, EventArgs e)
        {
        }

        private void Box_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar))
                return;

            if (char.IsDigit(e.KeyChar))
                return;

            if (e.KeyChar == '.' && !((TextBox)sender).Text.Contains("."))
                return;

            e.Handled = true;
        }
    }
}
