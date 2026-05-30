using System;
using System.Drawing;
using System.Windows.Forms;
using StaadSteelApp.Core;
using StaadSteelApp.Core.Advance;

namespace StaadSteelApp
{
    internal sealed class MergeMemberReleasesPanel : AdvanceActionPanelBase
    {
        private readonly TextBox _txtPreview;
        private readonly Button _btnPreview;
        private AdvanceMemberReleaseMergePreview _preview;

        public override string ActionText => "Merge member releases";
        public override string ApplyText => "Merge";
        public override bool UsesNodeSelection => false;

        public MergeMemberReleasesPanel()
        {
            AddLabel(this, "_lblMergeReleasesScope", "Scope", 2, 8);
            AddLabel(this, "_lblMergeReleasesScopeValue", "Whole model member releases", 60, 8, 178);
            _btnPreview = AddButton(this, "_btnMergeReleasesPreview", "Preview", 246, 3, BtnPreview_Click);

            _txtPreview = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.WhiteSmoke,
                Location = new Point(2, 36),
                Multiline = true,
                Name = "_txtMergeReleasesPreview",
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
                return Info("Run Preview to find identical START/END member releases.");

            if (_preview.MergeGroupCount == 0)
                return Error("Preview found no duplicate member releases to merge.");

            return Info($"Preview ready: {_preview.MergeGroupCount} release groups, {_preview.MergeMemberCount} assignments.");
        }

        public override bool Apply()
        {
            if (_preview == null)
            {
                Context.ShowWarning("Run Preview first.");
                return false;
            }

            if (_preview.MergeGroupCount == 0)
            {
                Context.ShowWarning("Preview found no duplicate member releases to merge.");
                return false;
            }

            DialogResult confirm = TopMostMessageBox.Show(
                FindForm(),
                $"Merge {_preview.MergeGroupCount} member release groups?\nThe tool will remove and recreate release assignments for those members.",
                "Merge Releases",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return false;

            bool ok = AdvanceService.TryMergeMemberReleases(_preview, out int mergedMemberCount, out string message);
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
            StatusForm.UpdateMessage("Scanning member release specifications...", "Merge Releases", Color.Blue, autoHideMs: 0);

            try
            {
                bool ok = AdvanceService.TryPreviewMemberReleaseMerge(out AdvanceMemberReleaseMergePreview preview, out string message);
                if (!ok)
                {
                    _preview = null;
                    _txtPreview.Text = message;
                    Context.ShowWarning(message);
                    return;
                }

                _preview = preview;
                _txtPreview.Text = message;
                Context.ShowSuccess($"Preview complete. Release groups: {_preview.MergeGroupCount}.");
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
