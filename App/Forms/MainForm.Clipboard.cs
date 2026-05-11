using System;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void btnCopy_Click(object sender, EventArgs e)
        {
            SectionInfo info = null;
            bool fromSelection = false;
            string detectError = "";

            object openStaad = null;
            if (TryGetMainOpenStaad(out openStaad))
            {
                try
                {
                    info = SectionService.DetectSelectedSection(openStaad);
                    fromSelection = true;
                }
                catch (Exception ex)
                {
                    detectError = ex.Message;
                }
            }

            if (info == null)
            {
                info = GetSectionInfoFromUI();
            }

            if (info == null)
            {
                if (!string.IsNullOrWhiteSpace(detectError))
                    TopMostMessageBox.ShowWarning(detectError, "Copy Warning");
                return;
            }

            PopulateLegacySectionMetadata(info);

            bool copiedNone =
                string.Equals(info.Name, "None", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(info.Code, "None", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(info.Name);
            if (copiedNone)
            {
                TopMostMessageBox.ShowWarning("Selected member has no valid section to copy.", "Copy Warning");
                return;
            }

            if (fromSelection)
            {
                _isPopulating = true;
                try
                {
                    SetSectionInfoToUI(info, openStaad);
                }
                finally
                {
                    _isPopulating = false;
                }
            }

            SectionClipboard.Set(info);
            UpdateClipboardUi();
            TopMostMessageBox.ShowSuccess("Section copied to clipboard.", "Success");
        }

        private void btnPaste_Click(object sender, EventArgs e)
        {
            var info = SectionClipboard.GetCopy();
            if (info == null)
            {
                TopMostMessageBox.ShowWarning("Clipboard is empty.", "Paste");
                UpdateClipboardUi();
                return;
            }

            PopulateLegacySectionMetadata(info);

            TryGetMainOpenStaad(out object openStaad);

            _isPopulating = true;
            try
            {
                SetSectionInfoToUI(info, openStaad);
                UpdateClipboardUi();
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Error pasting section: " + ex.Message, "Paste Error");
            }
            finally
            {
                _isPopulating = false;
            }

            if (openStaad != null)
            {
                string selectError = "";
                var beams = SectionService.GetSelectedBeamIDsSafe(openStaad, out selectError);
                if (!string.IsNullOrWhiteSpace(selectError))
                {
                    TopMostMessageBox.ShowWarning("Error reading selection: " + selectError, "Paste Warning");
                    return;
                }

                if (beams.Length == 0)
                {
                    TopMostMessageBox.ShowInfo("No beams selected. Pasted to UI only.");
                    return;
                }

                try
                {
                    SectionAssignmentResult result = SectionService.AssignSectionToSelection(openStaad, info);
                    ServiceOperationResultPresenter.Show(result);
                    if (result.Succeeded)
                        RefreshUsedSectionsListIfVisible();
                }
                catch (Exception ex)
                {
                    TopMostMessageBox.ShowError("Error assigning section: " + ex.Message, "Paste Error");
                }
            }
        }

        private void UpdateClipboardUi()
        {
            if (txtCopied == null || btnPaste == null) return;

            txtCopied.Text = SectionClipboard.HasData
                ? SectionClipboard.DisplayText
                : "(No section copied)";
            btnPaste.Enabled = SectionClipboard.HasData;
        }
    }
}
