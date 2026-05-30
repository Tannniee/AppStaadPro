using System;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using StaadSteelApp.Core;
using StaadSteelApp.Core.Advance;
using StaadSteelApp.Core.Misc;

namespace StaadSteelApp
{
    internal sealed class LoadFragmentPreviewPanel : AdvanceActionPanelBase
    {
        private readonly ComboBox _cboLoadCase;
        private readonly TextBox _txtTolerance;
        private readonly TextBox _txtPreview;
        private readonly Button _btnPreview;
        private AdvanceLoadFragmentPreview _preview;

        public override string ActionText => "Preview fragmented loads";
        public override string ApplyText => "Copy IDs";
        public override bool UsesNodeSelection => false;

        public LoadFragmentPreviewPanel()
        {
            AddLabel(this, "_lblLoadFragmentCase", "Case", 2, 8);
            _cboLoadCase = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                FormattingEnabled = true,
                Location = new Point(42, 4),
                Name = "_cboLoadFragmentCase",
                Size = new Size(154, 23)
            };
            _cboLoadCase.SelectedIndexChanged += (sender, args) => ClearPreview();
            Controls.Add(_cboLoadCase);

            AddLabel(this, "_lblLoadFragmentTolerance", "Tol", 202, 8);
            _txtTolerance = AddTextBox(this, "_txtLoadFragmentTolerance", 228, 4, 40);
            _txtTolerance.Text = "0.001";
            _txtTolerance.TextChanged += (sender, args) => NotifyHintChanged();
            _btnPreview = AddButton(this, "_btnLoadFragmentPreview", "Preview", 274, 3, BtnPreview_Click);
            _btnPreview.Size = new Size(46, 25);
            _btnPreview.Text = "Scan";

            _txtPreview = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.WhiteSmoke,
                Location = new Point(2, 36),
                Multiline = true,
                Name = "_txtLoadFragmentPreview",
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Size = new Size(318, 116),
                Text = "Preview has not been run."
            };
            Controls.Add(_txtPreview);
            LoadCases();
        }

        public override void AttachContext(AdvanceActionContext context)
        {
            base.AttachContext(context);
            if (_cboLoadCase.Items.Count == 0)
                LoadCases();
        }

        public override AdvanceActionHint GetHint()
        {
            if (!TryGetSelectedLoadCase(out _))
                return Error("Select a load case first.");

            if (!TryReadTolerance(out _))
                return Error("Tolerance must be zero or a positive number.");

            if (_preview == null)
                return Info("Run Preview to detect repeated UNI member load fragments by load case, direction, and force.");

            if (_preview.FragmentedGroupCount == 0)
                return Info("Preview found no fragmented UNI member load groups.");

            return Info($"Preview ready: {_preview.FragmentedGroupCount} fragmented groups, {_preview.FragmentedItemCount} items.");
        }

        public override bool Apply()
        {
            return CopyFragmentedMemberIds();
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            RunPreview();
        }

        private bool RunPreview()
        {
            if (!TryReadTolerance(out double tolerance))
            {
                Context.ShowWarning("Tolerance must be zero or a positive number.");
                return false;
            }

            if (!TryGetSelectedLoadCase(out int loadCaseNumber))
            {
                Context.ShowWarning("Select a load case first.");
                return false;
            }

            _btnPreview.Enabled = false;
            Cursor = Cursors.AppStarting;
            StatusForm.UpdateMessage("Scanning fragmented UNI member loads in selected load case...", "Load Preview", Color.Blue, autoHideMs: 0);

            try
            {
                bool ok = AdvanceService.TryPreviewLoadFragments(loadCaseNumber, tolerance, out AdvanceLoadFragmentPreview preview, out string message);
                if (!ok)
                {
                    _preview = null;
                    _txtPreview.Text = message;
                    Context.ShowWarning(message);
                    return false;
                }

                _preview = preview;
                _txtPreview.Text = message;
                Context.ShowSuccess($"Preview complete. Fragmented groups: {_preview.FragmentedGroupCount}.");
                return _preview.FragmentedGroupCount > 0;
            }
            catch (Exception ex)
            {
                _preview = null;
                _txtPreview.Text = ex.Message;
                Context.ShowWarning("Preview failed: " + ex.Message);
                return false;
            }
            finally
            {
                Cursor = Cursors.Default;
                _btnPreview.Enabled = true;
                NotifyHintChanged();
            }
        }

        private bool TryReadTolerance(out double tolerance)
        {
            return TryParseDouble(_txtTolerance.Text, out tolerance) && tolerance >= 0;
        }

        private bool CopyFragmentedMemberIds()
        {
            if (_preview == null)
            {
                Context.ShowWarning("Run Preview first.");
                return false;
            }

            int[] memberIds = _preview.Groups
                .SelectMany(group => group.MemberIds)
                .Distinct()
                .OrderBy(id => id)
                .ToArray();

            if (memberIds.Length == 0)
            {
                Context.ShowWarning("Preview has no fragmented member IDs to copy.");
                return false;
            }

            string clipboardText = MemberIdClipboardService.FormatMemberIdRanges(memberIds);
            Clipboard.SetText(clipboardText);
            Context.ShowSuccess($"Copied {memberIds.Length} fragmented member IDs.");
            return true;
        }

        private void LoadCases()
        {
            try
            {
                object selected = _cboLoadCase.SelectedItem;
                _cboLoadCase.BeginUpdate();
                _cboLoadCase.Items.Clear();

                foreach (LoadService.LoadCaseItem loadCase in LoadService.GetPrimaryLoadCases())
                    _cboLoadCase.Items.Add(loadCase);

                if (selected is LoadService.LoadCaseItem previous)
                {
                    for (int i = 0; i < _cboLoadCase.Items.Count; i++)
                    {
                        if (_cboLoadCase.Items[i] is LoadService.LoadCaseItem item && item.Number == previous.Number)
                        {
                            _cboLoadCase.SelectedIndex = i;
                            return;
                        }
                    }
                }

                if (_cboLoadCase.Items.Count > 0)
                    _cboLoadCase.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                _txtPreview.Text = "Could not load primary load cases: " + ex.Message;
            }
            finally
            {
                _cboLoadCase.EndUpdate();
            }
        }

        private bool TryGetSelectedLoadCase(out int loadCaseNumber)
        {
            loadCaseNumber = 0;
            if (!(_cboLoadCase.SelectedItem is LoadService.LoadCaseItem loadCase))
                return false;

            loadCaseNumber = loadCase.Number;
            return loadCaseNumber > 0;
        }

        private void ClearPreview()
        {
            _preview = null;
            _txtPreview.Text = "Preview has not been run.";
            NotifyHintChanged();
        }
    }
}
