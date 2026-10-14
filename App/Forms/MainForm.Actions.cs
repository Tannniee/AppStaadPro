using System;
using System.Drawing;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private bool _isRunningAnalysis;
        private bool _isHighlightingRatio;

        private void btnApplySel_Click(object sender, EventArgs e)
        {
            try
            {
                var info = GetSectionInfoFromUI();
                if (info == null) return;

                if (!TryGetMainOpenStaad(out object openStaad)) return;

                SectionAssignmentResult result = SectionService.AssignSectionToSelection(openStaad, info);
                ServiceOperationResultPresenter.Show(result);
                if (result.Succeeded)
                    RefreshUsedSectionsListIfVisible();
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Error: " + ex.Message);
            }
        }

        private void btnApplyAll_Click(object sender, EventArgs e)
        {
            try
            {
                if (!TryGetMainOpenStaad(out object openStaad)) return;

                // Step A: Select Same
                ServiceOperationResult detectAllResult = SectionService.SelectSameSection_DetectAll(openStaad);
                ServiceOperationResultPresenter.Show(detectAllResult);
                if (detectAllResult != null && !detectAllResult.Succeeded)
                    return;

                // Step B: Assign
                var info = GetSectionInfoFromUI();
                if (info == null) return;
                SectionAssignmentResult result = SectionService.AssignSectionToSelection(openStaad, info);
                ServiceOperationResultPresenter.Show(result);
                if (result.Succeeded)
                    RefreshUsedSectionsListIfVisible();
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Error Apply All: " + ex.Message);
            }
        }

        private void btnClean_Click(object sender, EventArgs e)
        {
            try
            {
                using (var optForm = new ExportOptionsForm())
                {
                    if (optForm.ShowDialog(this) != DialogResult.OK || !optForm.Confirmed)
                        return;

                    if (!TryGetMainOpenStaad(out object openStaad)) return;

                    var mode = optForm.SelectedMode == ExportMode.Galvanizing
                        ? SectionManagerService.CoatingExportMode.Galvanizing
                        : SectionManagerService.CoatingExportMode.MassBreakdown;

                    ServiceOperationResult result;
                    if (mode == SectionManagerService.CoatingExportMode.MassBreakdown)
                    {
                        if (!SectionManagerService.TryBuildMassBreakdownSnapshot(
                                openStaad, out var export, out string selectionError))
                        {
                            TopMostMessageBox.ShowError("Could not read STAAD member selection: " + selectionError, "Mass Breakdown");
                            return;
                        }
                        using (var previewForm = new MassBreakdownPreviewForm(export.Stats))
                        {
                            previewForm.Text = export.SelectedBeamCount > 0
                                ? $"Mass Breakdown Preview ({export.SelectedBeamCount} selected beams)"
                                : "Mass Breakdown Preview (full model)";
                            if (previewForm.ShowDialog(this) != DialogResult.OK)
                                return;

                            result = SectionManagerService.ExportToExcel(export, mode, previewForm.CreateOptions());
                        }
                    }
                    else
                    {
                        result = SectionManagerService.ExportToExcel(openStaad, mode);
                    }

                    ServiceOperationResultPresenter.Show(result);
                }
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Error Exporting: " + ex.Message);
            }
        }

        private void btnMisc_Click(object sender, EventArgs e)
        {
            ShowOwnedToolForm(
                ref _miscPanel,
                () => new MiscToolsForm(),
                () => _miscPanel = null,
                _ => UpdateMiscPanelLocation());
        }

        private void btnHideSelected_Click(object sender, EventArgs e)
        {
            try
            {
                if (!TryGetMainOpenStaad(out object openStaad)) return;

                ServiceOperationResult result = ViewFilterService.HideSelectedBeams(openStaad);
                ServiceOperationResultPresenter.Show(result);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Hide selected error: " + ex.Message, "View Filter");
            }
        }

        private void btnViewSelected_Click(object sender, EventArgs e)
        {
            try
            {
                if (!TryGetMainOpenStaad(out object openStaad)) return;

                ServiceOperationResult result = ViewFilterService.ViewOnlySelectedBeams(openStaad);
                ServiceOperationResultPresenter.Show(result);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("View selected error: " + ex.Message, "View Filter");
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            if (_isRunningAnalysis)
            {
                StatusForm.UpdateMessage("Analysis is already running.", "RUN", Color.DarkOrange, autoHideMs: 1500);
                return;
            }

            bool restoreRunEnabled = btnRun.Enabled;
            _isRunningAnalysis = true;
            btnRun.Enabled = false;

            try
            {
                if (!TryGetMainOpenStaad(out object openStaad)) return;

                // AnalyzeEx runs visibly and waits for STAAD to finish before returning.
                StatusForm.UpdateMessage("Running Analysis...", "RUN", Color.Blue, autoHideMs: 0);

                AnalysisRunResult runResult = AnalysisRunService.RunAnalysis(openStaad);
                if (runResult.Succeeded)
                {
                    StatusForm.UpdateMessage(runResult.Message, "RUN", Color.DarkGreen, autoHideMs: 3000);
                }
                else
                {
                    StatusForm.UpdateMessage(runResult.Message, "RUN", Color.DarkRed, autoHideMs: 0);
                }
            }
            catch (Exception ex)
            {
                AppLogger.Exception("Run analysis failed.", ex);
                StatusForm.UpdateMessage("Run Error: " + ex.Message, "RUN", Color.DarkRed, autoHideMs: 0);
            }
            finally
            {
                btnRun.Enabled = restoreRunEnabled;
                _isRunningAnalysis = false;
            }
        }

        private void btnRatio_Click(object sender, EventArgs e)
        {
            if (_isHighlightingRatio)
            {
                StatusForm.UpdateMessage("Ratio scan is already running.", "RATIO", Color.DarkOrange, autoHideMs: 1500);
                return;
            }

            using (var form = new RatioThresholdForm(0.95))
            {
                if (form.ShowDialog(this) != DialogResult.OK)
                    return;

                if (!TryGetMainOpenStaad(out object openStaad)) return;

                bool restoreRatioEnabled = btnRatio.Enabled;
                _isHighlightingRatio = true;
                btnRatio.Enabled = false;

                try
                {
                    StatusForm.UpdateMessage(
                        $"Scanning design ratios >= {form.Threshold:0.###}...",
                        "RATIO",
                        Color.Blue,
                        autoHideMs: 0);

                    ServiceOperationResult result = RatioHighlightService.HighlightByCriticalRatio(openStaad, form.Threshold);
                    ServiceOperationResultPresenter.Show(result);
                }
                catch (Exception ex)
                {
                    AppLogger.Exception("Ratio highlight failed.", ex);
                    StatusForm.UpdateMessage("Ratio Error: " + ex.Message, "RATIO", Color.DarkRed, autoHideMs: 0);
                }
                finally
                {
                    btnRatio.Enabled = restoreRatioEnabled;
                    _isHighlightingRatio = false;
                }
            }
        }

        private void btnAdvance_Click(object sender, EventArgs e)
        {
            try
            {
                ShowOwnedToolForm(
                    ref _advanceForm,
                    () => new AdvanceAlignForm(),
                    () => _advanceForm = null,
                    focus: true);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Advance Error: " + ex.Message, "Advance");
            }
        }

        private void btnAssignLoad_Click(object sender, EventArgs e)
        {
            try
            {
                ShowOwnedToolForm(
                    ref _assignLoadForm,
                    () => new AssignLoadForm(),
                    () => _assignLoadForm = null,
                    focus: true);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Assign Load Error: " + ex.Message, "Assign Load");
            }
        }

        private void btnGroups_Click(object sender, EventArgs e)
        {
            try
            {
                ShowOwnedToolForm(
                    ref _groupsForm,
                    () =>
                    {
                        var form = new GroupsForm();
                        form.RestoreLocation(GetDefaultGroupsLocation());
                        return form;
                    },
                    () => _groupsForm = null,
                    focus: true);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Group Error: " + ex.Message, "Groups");
            }
        }

        private Point GetDefaultGroupsLocation()
        {
            return new Point(Right - 5, Top + 320);
        }
    }
}
