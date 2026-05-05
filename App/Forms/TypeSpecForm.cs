using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace StaadSteelApp
{
    internal sealed class TypeSpecForm : Form
    {
        private readonly ComboBox _cboType;
        // private readonly Label _lblHelp; // Removed
        private readonly Label _lblSpec1;
        private readonly Label _lblSpec2;
        private readonly Label _lblSpec3;
        private readonly Label _lblSpec4;
        private readonly TextBox _txtSpec1;
        private readonly TextBox _txtSpec2;
        private readonly TextBox _txtSpec3;
        private readonly TextBox _txtSpec4;
        private readonly GroupBox _grpParams;
        private readonly TableLayoutPanel _table;
        private List<LegacyTypeSpecItem> _items;

        public event EventHandler ValueChanged;

        public LegacyTypeSpecItem SelectedItem => _cboType.SelectedItem as LegacyTypeSpecItem;
        public string Spec1Text => _txtSpec1.Text;
        public string Spec2Text => _txtSpec2.Text;
        public string Spec3Text => _txtSpec3.Text;
        public string Spec4Text => _txtSpec4.Text;

        public TypeSpecForm()
        {
            _items = new List<LegacyTypeSpecItem>();

            Text = "Type Specification";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            ShowInTaskbar = false;
            BackColor = SystemColors.Control;
            ClientSize = new Size(300, 195); // Make room for 4 rows at common DPI

            var lblType = new Label
            {
                Text = "Type",
                Location = new Point(12, 15),
                AutoSize = true
            };

            _cboType = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(60, 12),
                Size = new Size(228, 21) // Adjusted for narrower width
            };
            _cboType.SelectedIndexChanged += (s, e) => { UpdateSpecFields(); OnValueChanged(); };

            _grpParams = new GroupBox
            {
                Text = "Parameters",
                Location = new Point(12, 45),
                Size = new Size(276, 135)
            };

            _table = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 4,
                Location = new Point(10, 20),
                Size = new Size(256, 100)
            };
            _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 70f));
            _table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            _table.RowStyles.Clear();
            _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));
            _table.RowStyles.Add(new RowStyle(SizeType.Absolute, 24f));

            _lblSpec1 = new Label { AutoSize = true, Anchor = AnchorStyles.Left };
            _lblSpec2 = new Label { AutoSize = true, Anchor = AnchorStyles.Left };
            _lblSpec3 = new Label { AutoSize = true, Anchor = AnchorStyles.Left };
            _lblSpec4 = new Label { AutoSize = true, Anchor = AnchorStyles.Left };

            _txtSpec1 = new TextBox { Anchor = AnchorStyles.Left, Width = 80 };
            _txtSpec2 = new TextBox { Anchor = AnchorStyles.Left, Width = 80 };
            _txtSpec3 = new TextBox { Anchor = AnchorStyles.Left, Width = 80 };
            _txtSpec4 = new TextBox { Anchor = AnchorStyles.Left, Width = 80 };

            _txtSpec1.TextChanged += (s, e) => OnValueChanged();
            _txtSpec2.TextChanged += (s, e) => OnValueChanged();
            _txtSpec3.TextChanged += (s, e) => OnValueChanged();
            _txtSpec4.TextChanged += (s, e) => OnValueChanged();

            SetupNumericBox(_txtSpec1);
            SetupNumericBox(_txtSpec2);
            SetupNumericBox(_txtSpec3);
            SetupNumericBox(_txtSpec4);

            _table.Controls.Add(_lblSpec1, 0, 0);
            _table.Controls.Add(_txtSpec1, 1, 0);
            _table.Controls.Add(_lblSpec2, 0, 1);
            _table.Controls.Add(_txtSpec2, 1, 1);
            _table.Controls.Add(_lblSpec3, 0, 2);
            _table.Controls.Add(_txtSpec3, 1, 2);
            _table.Controls.Add(_lblSpec4, 0, 3);
            _table.Controls.Add(_txtSpec4, 1, 3);

            _grpParams.Controls.Add(_table);



            Controls.Add(lblType);
            Controls.Add(_cboType);
            Controls.Add(_grpParams);
            // Controls.Add(grpHelp); // Removed
            
            // Apply Rounded Corners
            this.Load += (s, e) => ApplyRounding();
        }

        [DllImport("Gdi32.dll", EntryPoint = "CreateRoundRectRgn")]
        private static extern IntPtr CreateRoundRectRgn(
            int nLeftRect,
            int nTopRect,
            int nRightRect,
            int nBottomRect,
            int nWidthEllipse,
            int nHeightEllipse
        );

        private void ApplyRounding()
        {
            Region = System.Drawing.Region.FromHrgn(CreateRoundRectRgn(0, 0, Width, Height, 8, 8));
        }

        public void Initialize(
            List<LegacyTypeSpecItem> items,
            LegacyTypeSpecItem selected,
            string spec1,
            string spec2,
            string spec3,
            string spec4)
        {
            _items = items ?? new List<LegacyTypeSpecItem>();
            _cboType.Items.Clear();
            _cboType.Items.AddRange(_items.Cast<object>().ToArray());

            if (selected != null)
            {
                var match = _items.FirstOrDefault(i => i.Value == selected.Value);
                _cboType.SelectedItem = match ?? selected;
            }
            if (_cboType.SelectedIndex < 0 && _cboType.Items.Count > 0)
                _cboType.SelectedIndex = 0;

            _txtSpec1.Text = string.IsNullOrWhiteSpace(spec1) ? "0" : spec1;
            _txtSpec2.Text = string.IsNullOrWhiteSpace(spec2) ? "0" : spec2;
            _txtSpec3.Text = string.IsNullOrWhiteSpace(spec3) ? "0" : spec3;
            _txtSpec4.Text = string.IsNullOrWhiteSpace(spec4) ? "0" : spec4;
            
            // _lblHelp.Text = helpText ?? ""; // Removed

            UpdateSpecFields();
        }

        private void OnValueChanged()
        {
            ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        private void UpdateSpecFields()
        {
            var item = _cboType.SelectedItem as LegacyTypeSpecItem;
            if (item == null)
            {
                SetSpecRow(_lblSpec1, _txtSpec1, false, "");
                SetSpecRow(_lblSpec2, _txtSpec2, false, "");
                SetSpecRow(_lblSpec3, _txtSpec3, false, "");
                SetSpecRow(_lblSpec4, _txtSpec4, false, "");
                return;
            }

            switch (item.Mode)
            {
                case LegacySpecMode.AngleSpacing:
                case LegacySpecMode.ChannelSpacing:
                case LegacySpecMode.DoubleProfile:
                    SetSpecRow(_lblSpec1, _txtSpec1, true, "SP (Clear Spacing, mm)");
                    SetSpecRow(_lblSpec2, _txtSpec2, false, "");
                    SetSpecRow(_lblSpec3, _txtSpec3, false, "");
                    SetSpecRow(_lblSpec4, _txtSpec4, false, "");
                    break;
                case LegacySpecMode.Composite:
                    SetSpecRow(_lblSpec1, _txtSpec1, true, "CT (Concrete Thickness, mm)");
                    SetSpecRow(_lblSpec2, _txtSpec2, true, "FC (Concrete Grade, kN/m2)");
                    SetSpecRow(_lblSpec3, _txtSpec3, true, "CW (Concrete Width, mm)");
                    SetSpecRow(_lblSpec4, _txtSpec4, true, "CD (Density, kN/m3)");
                    break;
                case LegacySpecMode.TopCover:
                    SetSpecRow(_lblSpec1, _txtSpec1, true, "WP (Cover Plate Width, mm)");
                    SetSpecRow(_lblSpec2, _txtSpec2, true, "TH (Cover Plate Thickness, mm)");
                    SetSpecRow(_lblSpec3, _txtSpec3, false, "");
                    SetSpecRow(_lblSpec4, _txtSpec4, false, "");
                    break;
                case LegacySpecMode.BottomCover:
                    SetSpecRow(_lblSpec1, _txtSpec1, true, "BW (Cover Plate Width, mm)");
                    SetSpecRow(_lblSpec2, _txtSpec2, true, "BT (Cover Plate Thickness, mm)");
                    SetSpecRow(_lblSpec3, _txtSpec3, false, "");
                    SetSpecRow(_lblSpec4, _txtSpec4, false, "");
                    break;
                case LegacySpecMode.TopBottomCover:
                    SetSpecRow(_lblSpec1, _txtSpec1, true, "WP (Cover Plate Width, mm)");
                    SetSpecRow(_lblSpec2, _txtSpec2, true, "TH (Cover Plate Thickness, mm)");
                    SetSpecRow(_lblSpec3, _txtSpec3, true, "BW (Cover Plate Width, mm)");
                    SetSpecRow(_lblSpec4, _txtSpec4, true, "BT (Cover Plate Thickness, mm)");
                    break;
                default:
                    SetSpecRow(_lblSpec1, _txtSpec1, false, "");
                    SetSpecRow(_lblSpec2, _txtSpec2, false, "");
                    SetSpecRow(_lblSpec3, _txtSpec3, false, "");
                    SetSpecRow(_lblSpec4, _txtSpec4, false, "");
                    break;
            }
        }

        private static void SetSpecRow(Label label, TextBox textBox, bool visible, string labelText)
        {
            label.Visible = visible;
            textBox.Visible = visible;
            label.Text = labelText;

            if (!visible)
            {
               // Keep value, don't reset to 0 unless needed (avoid wiping data if user switches type temporarily)
               // However, if we want to mimic previous behavior:
               // textBox.Text = "0"; 
               // The previous behavior reset it. Let's keep it safe.
               return; 
            }
            if (string.IsNullOrWhiteSpace(textBox.Text)) textBox.Text = "0";
        }


        private void SetupNumericBox(TextBox box)
        {
            box.Enter += Box_Enter;
            box.KeyPress += Box_KeyPress;
        }

        private void Box_Enter(object sender, EventArgs e)
        {
            var box = sender as TextBox;
            if (box == null) return;
            BeginInvoke((Action)delegate { box.SelectAll(); });
        }

        private void Box_KeyPress(object sender, KeyPressEventArgs e)
        {
             if (char.IsControl(e.KeyChar)) return;
             if (char.IsDigit(e.KeyChar)) return;
             if (e.KeyChar == '.' && !((TextBox)sender).Text.Contains('.')) return;
             e.Handled = true;
        }
    }
}
