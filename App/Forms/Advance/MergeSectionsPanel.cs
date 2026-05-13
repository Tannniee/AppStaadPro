using System;
using System.Drawing;
using System.Windows.Forms;
using StaadSteelApp.Core;
using StaadSteelApp.Core.Advance;

namespace StaadSteelApp
{
    internal sealed class MergeSectionsPanel : AdvanceActionPanelBase
    {
        private readonly TextBox _txtPreview;
        private readonly Button _btnPreview;
        private AdvanceSectionMergePreview _preview;

        public override string ActionText => "Merge duplicate sections";
        public override string ApplyText => "Merge";
        public override bool UsesNodeSelection => false;

        public MergeSectionsPanel()
        {
            AddLabel(this, "_lblMergeSectionsScope", "Scope", 2, 8);
            AddLabel(this, "_lblMergeSectionsScopeValue", "Whole model duplicate sections", 60, 8, 178);
            _btnPreview = AddButton(this, "_btnMergeSectionsPreview", "Preview", 246, 3, BtnPreview_Click);

            _txtPreview = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.WhiteSmoke,
                Location = new Point(2, 36),
                Multiline = true,
                Name = "_txtMergeSectionsPreview",
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                Size = new Size(318, 116),
                Text = "Preview has not been run."
            };
            Controls.Add(_txtPreview);
        }

        public override AdvanceActionHint GetHint()
        {
            if (_preview == null)
                return Info("Run Preview to find same-section groups split across multiple Ref IDs.");

            if (_preview.DuplicateSectionGroupCount == 0)
                return Error("Preview found no duplicate section groups to merge.");

            return Info($"Preview ready: {_preview.DuplicateSectionGroupCount} duplicate groups, {_preview.DuplicateMemberCount} members.");
        }

        public override bool Apply()
        {
            if (_preview == null)
            {
                Context.ShowWarning("Run Preview first.");
                return false;
            }

            if (_preview.DuplicateSectionGroupCount == 0)
            {
                Context.ShowWarning("Preview found no duplicate section groups to merge.");
                return false;
            }

            DialogResult confirm = TopMostMessageBox.Show(
                FindForm(),
                $"Merge {_preview.DuplicateSectionGroupCount} duplicate section groups?\nUnused property Ref IDs will not be deleted.",
                "Merge Sections",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return false;

            bool ok = AdvanceService.TryMergeDuplicateSections(_preview, out int mergedMemberCount, out string message);
            if (!ok)
            {
                Context.ShowWarning(message);
                return false;
            }

            Context.ShowSuccess(message);
            _preview = null;
            _txtPreview.Text = "Merge completed. Run Preview again to refresh model state.";
            NotifyHintChanged();
            return mergedMemberCount > 0;
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            _btnPreview.Enabled = false;
            Cursor = Cursors.AppStarting;
            StatusForm.UpdateMessage("Scanning duplicate section Ref IDs...", "Merge Sections", Color.Blue, autoHideMs: 0);

            try
            {
                bool ok = AdvanceService.TryPreviewSectionMerge(out AdvanceSectionMergePreview preview, out string message);
                if (!ok)
                {
                    _preview = null;
                    _txtPreview.Text = message;
                    Context.ShowWarning(message);
                    return;
                }

                _preview = preview;
                _txtPreview.Text = message;
                Context.ShowSuccess($"Preview complete. Duplicate groups: {_preview.DuplicateSectionGroupCount}.");
            }
            catch (Exception ex)
            {
                _preview = null;
                _txtPreview.Text = ex.Message;
                Context.ShowWarning("Preview failed: " + ex.Message);
            }
            finally
            {
                Cursor = Cursors.Default;
                _btnPreview.Enabled = true;
                NotifyHintChanged();
            }
        }
    }
}
