using System;
using System.Drawing;
using System.Windows.Forms;
using StaadSteelApp.Core;
using StaadSteelApp.Core.Advance;

namespace StaadSteelApp
{
    internal sealed class ConvertUptPanel : AdvanceActionPanelBase
    {
        private readonly TextBox _txtPreview;
        private readonly Button _btnPreview;
        private AdvanceUptConversionPreview _preview;

        public override string ActionText => "Convert legacy Pipe/Tube to UPT";
        public override string ApplyText => "Convert";
        public override bool UsesNodeSelection => false;

        public ConvertUptPanel()
        {
            AddLabel(this, "_lblConvertUptScope", "Scope", 2, 8);
            AddLabel(this, "_lblConvertUptScopeValue", "Whole model legacy Pipe/Tube", 60, 8, 166);
            _btnPreview = AddButton(this, "_btnConvertUptPreview", "Preview", 246, 3, BtnPreview_Click);

            _txtPreview = new TextBox
            {
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                BackColor = Color.WhiteSmoke,
                Location = new Point(2, 36),
                Multiline = true,
                Name = "_txtConvertUptPreview",
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
                return Info("Run Preview to detect whole-model legacy Pipe/Tube sections before conversion.");

            if (_preview.EligibleGroupCount == 0)
                return Error("Preview found no legacy Pipe/Tube sections to convert.");

            return Info($"Preview ready: {_preview.EligibleMemberCount} members across {_preview.EligibleGroupCount} groups.");
        }

        public override bool Apply()
        {
            if (_preview == null)
            {
                Context.ShowWarning("Run Preview first.");
                return false;
            }

            if (_preview.EligibleGroupCount == 0)
            {
                Context.ShowWarning("Preview found no legacy Pipe/Tube sections to convert.");
                return false;
            }

            DialogResult confirm = TopMostMessageBox.Show(
                FindForm(),
                $"Convert {_preview.EligibleMemberCount} members across {_preview.EligibleGroupCount} groups to UPT?",
                "Convert UPT",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm != DialogResult.Yes)
                return false;

            bool ok = AdvanceService.TryConvertLegacyPipeTubeToUpt(_preview, out int convertedMemberCount, out string message);
            if (!ok)
            {
                Context.ShowWarning(message);
                return false;
            }

            Context.ShowSuccess(message);
            _preview = null;
            _txtPreview.Text = "Conversion completed. Run Preview again to refresh model state.";
            NotifyHintChanged();
            return convertedMemberCount > 0;
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            _btnPreview.Enabled = false;
            Cursor = Cursors.AppStarting;
            StatusForm.UpdateMessage("Scanning legacy Pipe/Tube sections...", "Convert UPT", Color.Blue, autoHideMs: 0);

            try
            {
                bool ok = AdvanceService.TryPreviewLegacyPipeTubeUptConversion(out AdvanceUptConversionPreview preview, out string message);
                if (!ok)
                {
                    _preview = null;
                    _txtPreview.Text = message;
                    Context.ShowWarning(message);
                    return;
                }

                _preview = preview;
                _txtPreview.Text = message;
                Context.ShowSuccess($"Preview complete. Eligible groups: {_preview.EligibleGroupCount}, members: {_preview.EligibleMemberCount}.");
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
