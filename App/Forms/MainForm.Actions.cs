using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Runtime.InteropServices;
using StaadSteelApp.Core;
using StaadSteelApp.Core.Check;
using StaadSteelApp.Core.Interop;
using StaadSteelApp.AISC360;
using System.Linq;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void btnApplySel_Click(object sender, EventArgs e)
        {
            try
            {
                var info = GetSectionInfoFromUI();
                if (info == null) return;

                var os = StaadManager.GetOpenSTAAD();
                SectionService.AssignSectionToSelection(os, info);
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
                var os = StaadManager.GetOpenSTAAD();
                // Step A: Select Same
                SectionService.SelectSameSection_DetectAll();

                // Step B: Assign
                var info = GetSectionInfoFromUI();
                if (info == null) return;
                SectionService.AssignSectionToSelection(os, info);
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

                    var os = StaadManager.GetOpenSTAAD();
                    if (os == null) return;

                    var mode = optForm.SelectedMode == ExportMode.Galvanizing
                        ? SectionManagerService.CoatingExportMode.Galvanizing
                        : SectionManagerService.CoatingExportMode.MassBreakdown;

                    SectionManagerService.ExportToExcel(os, mode);
                }
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Error Exporting: " + ex.Message);
            }
        }

        private void btnMisc_Click(object sender, EventArgs e)
        {
            if (_miscPanel == null || _miscPanel.IsDisposed)
            {
                _miscPanel = new MiscToolsForm();
                _miscPanel.FormClosed += (s, args) => _miscPanel = null;
            }

            if (_miscPanel.Owner != this)
            {
                this.AddOwnedForm(_miscPanel);
            }

            if (!_miscPanel.Visible)
            {
                _miscPanel.Show(this);
            }

            UpdateMiscPanelLocation();
            _miscPanel.BringToFront();
        }

        private void btnFindReplace_Click(object sender, EventArgs e)
        {
            // "Check" Button Action
            try
            {
                dynamic os = StaadManager.GetOpenSTAAD();
                if (os == null) return;

                // 1. Get Selected Beams
                object openStaad = os;
                if (!OpenStaadSelectionAdapter.TryGetSelectedBeamIds(
                        openStaad,
                        false,
                        false,
                        new[] { 1, 0 },
                        out int[] selectedBeams,
                        out string selectionError))
                {
                    TopMostMessageBox.ShowError("Could not read selected beams: " + selectionError, "Check");
                    return;
                }

                if (selectedBeams.Length == 0)
                {
                    TopMostMessageBox.ShowInfo("No beams selected in STAAD.", "Check");
                    return;
                }

                StatusForm.UpdateMessage($"Extracting forces for {selectedBeams.Length} beams...", "CHECK", Color.Blue, 0);
                CheckForceExtractionResult extraction = CheckForceExtractionService.ExtractEndForces(openStaad, selectedBeams);
                if (!extraction.HasForces)
                {
                    string message = string.IsNullOrWhiteSpace(extraction.ErrorSummary)
                        ? "No forces found for selected beams."
                        : "No forces found for selected beams. " + extraction.ErrorSummary;
                    TopMostMessageBox.ShowInfo(message, "Check");
                    return;
                }
                
                // 3b. AISC Calculation
                // Removed pre-calculation loop. Passed to CheckForm for dynamic calculation.
                
                StatusForm.UpdateMessage("Extraction complete. Opening Check Form...", "CHECK", Color.Green, 1000);

                // 4. Show in Form
                CheckForm form = new CheckForm();
                
                // Pass raw data and OS reference
                form.LoadData(new List<MemberForceData>(extraction.Forces), os); 
                form.Show(); 
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Check Error: " + ex.Message);
            }
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            try
            {
                object os = StaadManager.GetOpenSTAAD();
                if (os == null) return;

                // Simple Run: AnalyzeEx(bSilent=false, bHidden=false, bWait=true)
                // We Wait=true so the code blocks here until STAAD finishes.
                StatusForm.UpdateMessage("Running Analysis...", "RUN", Color.Blue, autoHideMs: 0);
                Application.DoEvents();

                if (OpenStaadAnalysisAdapter.TryAnalyze(os, false, false, true, out string analyzeError))
                {
                    // Logic simplified: Just report finished. No error counting.
                    StatusForm.UpdateMessage("Analysis Execution Finished.", "RUN", Color.DarkGreen, autoHideMs: 3000);
                }
                else
                {
                    StatusForm.UpdateMessage("Failed to execute AnalyzeEx: " + analyzeError, "RUN", Color.DarkRed, autoHideMs: 0);
                }
            }
            catch (Exception ex)
            {
                StatusForm.UpdateMessage("Run Error: " + ex.Message, "RUN", Color.DarkRed, autoHideMs: 0);
            }
        }

        private void btnAdvance_Click(object sender, EventArgs e)
        {
            try
            {
                if (_advanceForm == null || _advanceForm.IsDisposed)
                {
                    _advanceForm = new AdvanceAlignForm();
                    _advanceForm.FormClosed += (s, args) => _advanceForm = null;
                }

                if (_advanceForm.Owner != this)
                {
                    AddOwnedForm(_advanceForm);
                }

                if (!_advanceForm.Visible)
                {
                    _advanceForm.Show(this);
                }

                _advanceForm.BringToFront();
                _advanceForm.Focus();
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
                if (_assignLoadForm == null || _assignLoadForm.IsDisposed)
                {
                    _assignLoadForm = new AssignLoadForm();
                    _assignLoadForm.FormClosed += (s, args) => _assignLoadForm = null;
                }

                if (_assignLoadForm.Owner != this)
                {
                    AddOwnedForm(_assignLoadForm);
                }

                if (!_assignLoadForm.Visible)
                {
                    _assignLoadForm.Show(this);
                }

                _assignLoadForm.BringToFront();
                _assignLoadForm.Focus();
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Assign Load Error: " + ex.Message, "Assign Load");
            }
        }
    }
}
