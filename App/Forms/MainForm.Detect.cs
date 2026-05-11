using System;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void btnDetect_Click(object sender, EventArgs e)
        {
            try
            {
                if (!TryGetMainOpenStaad(out object openStaad)) return;

                var info = SectionService.DetectSelectedSection(openStaad);
                ApplyDetectedSection(openStaad, info);
                RefreshUsedSectionsListIfVisible(info?.PropertyRefNo ?? 0);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Error: " + ex.Message, "Detect Error");
            }
        }

        private void btnDetectAll_Click(object sender, EventArgs e)
        {
            try
            {
                if (!TryGetMainOpenStaad(out object openStaad)) return;

                var info = SectionService.DetectSelectedSection(openStaad);
                ApplyDetectedSection(openStaad, info);

                ServiceOperationResult result = SectionService.SelectSameSection_DetectAll(openStaad);
                ServiceOperationResultPresenter.Show(result);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Error: " + ex.Message, "Detect Error");
            }
        }
    }
}
