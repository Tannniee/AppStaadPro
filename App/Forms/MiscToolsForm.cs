using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    internal sealed class MiscToolsForm : Form
    {
        private readonly CheckBox[] _startChecks = new CheckBox[6];
        private readonly CheckBox[] _endChecks = new CheckBox[6];
        private Button[] _memberTypeButtons;
        private bool _memberSpecBusy;
        private bool _renumberBusy;
        private TextBox _txtAngle;

        private static readonly string[] DofLabels = { "FX", "FY", "FZ", "MX", "MY", "MZ" };

        public MiscToolsForm()
        {
            Text = "Misc Tools";
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(284, 552);
            TopMost = true;

            Controls.Add(BuildReleasesGroup());
            Controls.Add(BuildLocalAxesGroup());
            Controls.Add(BuildMemberTypeGroup());
            Controls.Add(BuildSupportGroup());
            Controls.Add(BuildMemberIdsGroup());
        }

        private GroupBox BuildReleasesGroup()
        {
            var grpReleases = new GroupBox
            {
                Text = "Frame Releases",
                Location = new Point(12, 12),
                Size = new Size(260, 208)
            };

            var tblReleases = BuildReleasesTable();
            grpReleases.Controls.Add(tblReleases);

            var btnReset = new Button
            {
                Text = "Reset Default",
                Location = new Point(20, 171),
                Size = new Size(100, 30)
            };
            btnReset.Click += (s, e) =>
            {
                ServiceOperationResult result = MiscToolsService.ResetDefaultSelected();
                ServiceOperationResultPresenter.Show(result);
                if (result != null && result.Succeeded)
                    ClearReleaseChecks();
            };

            var btnApplyReleases = new Button
            {
                Text = "Apply",
                Location = new Point(140, 171),
                Size = new Size(100, 30)
            };
            btnApplyReleases.Click += (s, e) => ApplyReleases();

            grpReleases.Controls.Add(btnReset);
            grpReleases.Controls.Add(btnApplyReleases);

            return grpReleases;
        }

        private GroupBox BuildLocalAxesGroup()
        {
            var grpLocalAxes = new GroupBox
            {
                Text = "Local Axes",
                Location = new Point(12, 226),
                Size = new Size(260, 59)
            };

            var lblAngle = new Label
            {
                Text = "Angle:",
                Location = new Point(15, 25),
                AutoSize = true
            };

            _txtAngle = new TextBox
            {
                Location = new Point(60, 22),
                Size = new Size(60, 20),
                Text = "90"
            };
            _txtAngle.KeyPress += AngleBox_KeyPress;

            var btnRotate = new Button
            {
                Text = "Rotate",
                Location = new Point(140, 20),
                Size = new Size(100, 25)
            };
            btnRotate.Click += (s, e) => ApplyBetaAngle();

            grpLocalAxes.Controls.Add(lblAngle);
            grpLocalAxes.Controls.Add(_txtAngle);
            grpLocalAxes.Controls.Add(btnRotate);

            return grpLocalAxes;
        }

        private GroupBox BuildMemberTypeGroup()
        {
            var grpMemberType = new GroupBox
            {
                Text = "Member Type",
                Location = new Point(12, 291),
                Size = new Size(260, 90)
            };

            var btnNormal = new Button
            {
                Text = "Normal",
                Location = new Point(20, 24),
                Size = new Size(100, 26)
            };
            btnNormal.Click += async (s, e) => await RunMemberSpecActionAsync("Applying Normal member spec...", MiscToolsService.ApplyNormalMemberSpecSelected);

            var btnTruss = new Button
            {
                Text = "Truss",
                Location = new Point(140, 24),
                Size = new Size(100, 26)
            };
            btnTruss.Click += async (s, e) => await RunMemberSpecActionAsync("Applying Truss member spec...", MiscToolsService.ApplyTrussMemberSpecSelected);

            var btnTension = new Button
            {
                Text = "Tension",
                Location = new Point(20, 56),
                Size = new Size(100, 26)
            };
            btnTension.Click += async (s, e) => await RunMemberSpecActionAsync("Applying Tension member spec...", MiscToolsService.ApplyTensionMemberSpecSelected);

            var btnCompression = new Button
            {
                Text = "Compression",
                Location = new Point(140, 56),
                Size = new Size(100, 26)
            };
            btnCompression.Click += async (s, e) => await RunMemberSpecActionAsync("Applying Compression member spec...", MiscToolsService.ApplyCompressionMemberSpecSelected);

            _memberTypeButtons = new[] { btnNormal, btnTruss, btnTension, btnCompression };

            grpMemberType.Controls.Add(btnNormal);
            grpMemberType.Controls.Add(btnTruss);
            grpMemberType.Controls.Add(btnTension);
            grpMemberType.Controls.Add(btnCompression);

            return grpMemberType;
        }

        private GroupBox BuildSupportGroup()
        {
            var grpSupport = new GroupBox
            {
                Text = "Support",
                Location = new Point(12, 387),
                Size = new Size(260, 92)
            };

            var btnFixed = new Button
            {
                Text = "Fixed",
                Location = new Point(20, 24),
                Size = new Size(100, 26)
            };
            btnFixed.Click += (s, e) => ServiceOperationResultPresenter.Show(MiscToolsService.ApplyFixedSupportSelected());

            var btnPinned = new Button
            {
                Text = "Pinned",
                Location = new Point(140, 24),
                Size = new Size(100, 26)
            };
            btnPinned.Click += (s, e) => ServiceOperationResultPresenter.Show(MiscToolsService.ApplyPinnedSupportSelected());

            var btnFixedBut = new Button
            {
                Text = "Fixed But",
                Location = new Point(20, 56),
                Size = new Size(100, 26)
            };
            btnFixedBut.Click += (s, e) => ApplyFixedButSupport();

            var btnNone = new Button
            {
                Text = "None",
                Location = new Point(140, 56),
                Size = new Size(100, 26)
            };
            btnNone.Click += (s, e) => ServiceOperationResultPresenter.Show(MiscToolsService.RemoveSupportSelected());

            grpSupport.Controls.Add(btnFixed);
            grpSupport.Controls.Add(btnPinned);
            grpSupport.Controls.Add(btnFixedBut);
            grpSupport.Controls.Add(btnNone);

            return grpSupport;
        }

        private GroupBox BuildMemberIdsGroup()
        {
            var grpMemberIds = new GroupBox
            {
                Text = "Member IDs",
                Location = new Point(12, 485),
                Size = new Size(260, 55)
            };

            var btnRenumber = new Button
            {
                Text = "Renumber",
                Location = new Point(20, 20),
                Size = new Size(100, 26)
            };
            btnRenumber.Click += async (s, e) => await RenumberMembersAsync();

            var btnIdMember = new Button
            {
                Text = "ID MEMBER",
                Location = new Point(140, 20),
                Size = new Size(100, 26)
            };
            btnIdMember.Click += (s, e) => CopySelectedMemberIds();

            grpMemberIds.Controls.Add(btnRenumber);
            grpMemberIds.Controls.Add(btnIdMember);

            return grpMemberIds;
        }

        private TableLayoutPanel BuildReleasesTable()
        {
            var tbl = new TableLayoutPanel
            {
                ColumnCount = 3,
                RowCount = 7,
                Location = new Point(10, 20),
                Size = new Size(240, 145),
                Margin = Padding.Empty,
                Padding = Padding.Empty
            };

            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
            tbl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));

            for (int i = 0; i < tbl.RowCount; i++)
            {
                tbl.RowStyles.Add(new RowStyle(SizeType.Absolute, 20f));
            }

            var lblHeaderStart = new Label
            {
                Text = "Start",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            var lblHeaderEnd = new Label
            {
                Text = "End",
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };

            tbl.Controls.Add(lblHeaderStart, 1, 0);
            tbl.Controls.Add(lblHeaderEnd, 2, 0);

            for (int i = 0; i < DofLabels.Length; i++)
            {
                int row = i + 1;

                var lbl = new Label
                {
                    Text = DofLabels[i],
                    Dock = DockStyle.Fill,
                    TextAlign = ContentAlignment.MiddleLeft
                };

                var chkStart = new CheckBox
                {
                    Anchor = AnchorStyles.None,
                    AutoSize = true,
                    Margin = Padding.Empty
                };

                var chkEnd = new CheckBox
                {
                    Anchor = AnchorStyles.None,
                    AutoSize = true,
                    Margin = Padding.Empty
                };

                _startChecks[i] = chkStart;
                _endChecks[i] = chkEnd;

                tbl.Controls.Add(lbl, 0, row);
                tbl.Controls.Add(chkStart, 1, row);
                tbl.Controls.Add(chkEnd, 2, row);
            }

            return tbl;
        }

        private void ApplyReleases()
        {
            try
            {
                int[] start = ReadChecks(_startChecks);
                int[] end = ReadChecks(_endChecks);
                ServiceOperationResultPresenter.Show(MiscToolsService.ApplyReleasesSelected(start, end));
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Error applying releases: " + ex.Message, "Release Error");
            }
        }

        private void ApplyBetaAngle()
        {
            if (!double.TryParse(_txtAngle.Text, out double angle))
            {
                TopMostMessageBox.ShowWarning("Angle must be a number.", "Local Axes");
                return;
            }

            try
            {
                ServiceOperationResultPresenter.Show(MiscToolsService.ApplyBetaAngleSelected(angle));
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Error applying beta angle: " + ex.Message, "Beta Angle Error");
            }
        }

        private void ApplyFixedButSupport()
        {
            using (var form = new FixedButSupportForm())
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                try
                {
                    ServiceOperationResultPresenter.Show(MiscToolsService.ApplyFixedButSupportSelected(form.ReleaseSpec, form.SpringSpec));
                }
                catch (Exception ex)
                {
                    TopMostMessageBox.ShowError("Error applying fixed but support: " + ex.Message, "Fixed But");
                }
            }
        }

        private void CopySelectedMemberIds()
        {
            try
            {
                ServiceOperationResult result = MiscToolsService.CopySelectedMemberIds(out string clipboardText);
                if (result != null && result.Succeeded && !string.IsNullOrWhiteSpace(clipboardText))
                    Clipboard.SetText(clipboardText);

                ServiceOperationResultPresenter.Show(result);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Error copying member IDs: " + ex.Message, "ID MEMBER");
            }
        }

        private async Task RenumberMembersAsync()
        {
            if (_renumberBusy)
                return;

            DialogResult confirm = TopMostMessageBox.Show(
                this,
                "Renumber all analytical members by property Ref ID?\nThis changes member numbers in STAAD.",
                "Renumber",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return;

            await FormOperationRunner.RunStaThreadAsync(
                this,
                () => _renumberBusy,
                value => _renumberBusy = value,
                "Renumber",
                "Renumbering members by property Ref ID...",
                MiscToolsService.RenumberMembersByPropertyRef,
                "Error renumbering members: ");
        }

        private static int[] ReadChecks(CheckBox[] checks)
        {
            var arr = new int[6];
            for (int i = 0; i < arr.Length && i < checks.Length; i++)
            {
                arr[i] = checks[i].Checked ? 1 : 0;
            }
            return arr;
        }

        private void ClearReleaseChecks()
        {
            foreach (var chk in _startChecks) chk.Checked = false;
            foreach (var chk in _endChecks) chk.Checked = false;
        }

        private void AngleBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (char.IsControl(e.KeyChar)) return;
            if (char.IsDigit(e.KeyChar)) return;
            if (e.KeyChar == '-' && _txtAngle.SelectionStart == 0 && !_txtAngle.Text.Contains("-")) return;
            if (e.KeyChar == '.' && !_txtAngle.Text.Contains(".")) return;
            if (e.KeyChar == ',' && !_txtAngle.Text.Contains(",")) return;
            e.Handled = true;
        }

        private async Task RunMemberSpecActionAsync(string statusText, Func<ServiceOperationResult> operation)
        {
            if (_memberSpecBusy)
                return;

            await FormOperationRunner.RunStaThreadAsync(
                this,
                () => _memberSpecBusy,
                value => _memberSpecBusy = value,
                "Member Spec",
                statusText,
                operation,
                "Member spec failed: ",
                SetMemberTypeButtonsEnabled);
        }

        private void SetMemberTypeButtonsEnabled(bool enabled)
        {
            if (_memberTypeButtons == null)
                return;

            foreach (Button button in _memberTypeButtons)
            {
                if (button != null && !button.IsDisposed)
                    button.Enabled = enabled;
            }
        }
    }
}
