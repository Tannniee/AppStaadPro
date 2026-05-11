using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using StaadSteelApp.AISC360;
using StaadSteelApp.Core;
using StaadSteelApp.Core.Check;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp
{
    public partial class CheckForm : Form
    {
        private void InitializeAutoDetect()
        {
            _selectionTimer = new System.Windows.Forms.Timer();
            _selectionTimer.Interval = 400;
            _selectionTimer.Tick += SelectionTimer_Tick;
        }

        private void SelectionTimer_Tick(object sender, EventArgs e)
        {
            if (!_isAutoUpdateEnabled || _os == null) return;

            try
            {
                int[] selectedBeams = GetSelectedBeamIDs(out string _);
                if (selectedBeams.Length == 1)
                {
                    string currentFrame = selectedBeams[0].ToString();
                    if (currentFrame != _lastDetectedFrame)
                    {
                        _lastDetectedFrame = currentFrame;
                        AutoDetectAndCalculate(selectedBeams[0], selectedBeams);
                    }
                }
            }
            catch (Exception ex)
            {
                // Polling runs frequently; warn only once to avoid spamming.
                WarnOnce("AutoDetect:Poll", "Auto-detect polling failed. STAAD may be busy or disconnected.", ex);
            }
        }

        private void AutoDetectAndCalculate(int beamNo, int[] selectedBeams)
        {
            try
            {
                _allForces = ExtractForcesForBeams(selectedBeams);
                if (_allForces.Count == 0)
                {
                    lblCompare.Text = "No forces found for selection.";
                    return;
                }

                cboFrame.SelectedIndexChanged -= cboFrame_SelectedIndexChanged;
                cboFrame.Items.Clear();
                foreach (int beam in selectedBeams)
                {
                    cboFrame.Items.Add(beam.ToString());
                }

                int idx = cboFrame.Items.IndexOf(beamNo.ToString());
                if (idx >= 0) cboFrame.SelectedIndex = idx;
                cboFrame.SelectedIndexChanged += cboFrame_SelectedIndexChanged;

                lblSection.Text = $"Section: {TryGetSectionName(beamNo)}";
                lblMaterial.Text = $"Material: {TryGetMaterialName(beamNo)}";
                lblCombo.Text = "Combo: --";

                UpdateBuiltUpOption(beamNo);
                PerformCalculation();
            }
            catch (Exception ex)
            {
                lblCompare.Text = $"Auto-detect error: {ex.Message}";
            }
        }

        private void LoadSelectedBeams(bool showMessage)
        {
            int[] selectedBeams = GetSelectedBeamIDs(out string selError);
            if (selectedBeams.Length == 0)
            {
                if (showMessage)
                {
                    string msg = string.IsNullOrEmpty(selError)
                        ? "Please select beams in STAAD."
                        : "Could not read STAAD selection: " + selError;
                    MessageBox.Show(msg, "No Selection", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                return;
            }

            _allForces = ExtractForcesForBeams(selectedBeams);
            if (_allForces.Count == 0)
            {
                if (showMessage)
                    MessageBox.Show("No forces found for selected beams.", "No Results", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            cboFrame.SelectedIndexChanged -= cboFrame_SelectedIndexChanged;
            cboFrame.Items.Clear();
            foreach (int beam in selectedBeams)
            {
                cboFrame.Items.Add(beam.ToString());
            }

            cboFrame.SelectedIndex = 0;
            cboFrame.SelectedIndexChanged += cboFrame_SelectedIndexChanged;

            if (selectedBeams.Length > 0)
                UpdateBuiltUpOption(selectedBeams[0]);

            PerformCalculation();
        }

        private List<int> GetAllLoadCases()
        {
            if (_os == null)
                return new List<int>();

            object openStaad = _os;
            if (CheckForceExtractionService.TryGetAllLoadCases(openStaad, out int[] loadCases, out string error))
            {
                if (!string.IsNullOrWhiteSpace(error))
                    WarnOnce("LoadCases:Partial", "STAAD load case extraction completed with warnings: " + error);

                return new List<int>(loadCases);
            }

            WarnOnce("LoadCases:Adapter", "Could not read STAAD load cases. Force extraction may be incomplete. " + error);
            return new List<int>();
        }

        private List<MemberForceData> ExtractForcesForBeams(int[] beamIds)
        {
            if (_os == null || beamIds == null || beamIds.Length == 0)
                return new List<MemberForceData>();

            object openStaad = _os;
            CheckForceExtractionResult extraction = CheckForceExtractionService.ExtractEndForces(openStaad, beamIds);
            if (extraction.ErrorCount > 0)
                WarnOnce("ForceExtract:MemberEndForces",
                    $"{extraction.ErrorSummary} Some results may be missing or incomplete. See Debug output for samples.");

            return new List<MemberForceData>(extraction.Forces);
        }

        private int[] GetSelectedBeamIDs(out string error)
        {
            error = string.Empty;
            if (_os == null)
                return Array.Empty<int>();

            object openStaad = _os;
            if (CheckForceExtractionService.TryGetSelectedBeamIds(
                    openStaad,
                    new[] { 0, 1 },
                    out int[] beamIds,
                    out error))
            {
                return beamIds;
            }

            return Array.Empty<int>();
        }

        private string TryGetSectionName(int memberNo)
        {
            if (_os == null) return "Unknown";

            object openStaad = _os;
            if (CheckMemberInfoService.TryGetBeamSectionName(openStaad, memberNo, out string sectionName, out string error))
                return string.IsNullOrWhiteSpace(sectionName) ? "Unknown" : sectionName;

            WarnOnce($"GetBeamSectionName:{memberNo}", $"Member {memberNo}: could not read section name from STAAD. Using 'Unknown'. {error}");
            return "Unknown";
        }

        private bool TryGetTaperedSectionData(int memberNo, out CheckTaperedSectionInfo data)
        {
            data = null;
            if (_os == null) return false;

            object openStaad = _os;
            if (!CheckMemberInfoService.TryGetTaperedSectionData(openStaad, memberNo, out CheckTaperedSectionInfo tapered, out string error))
            {
                WarnOnce($"TryGetTapered:{memberNo}", $"Member {memberNo}: failed to read tapered section data from STAAD. {error}");
                return false;
            }

            data = tapered;
            return true;
        }

        private string TryGetMaterialName(int memberNo)
        {
            if (_os == null) return "Steel";

            object openStaad = _os;
            if (CheckMemberInfoService.TryGetBeamMaterialName(openStaad, memberNo, out string materialName, out string error))
                return string.IsNullOrWhiteSpace(materialName) ? "Steel" : materialName;

            WarnOnce($"GetBeamMaterialName:{memberNo}", $"Member {memberNo}: could not read material name from STAAD. Using 'Steel'. {error}");
            return "Steel";
        }

        private void UpdateBuiltUpOption(int memberNo)
        {
            bool isPipeOrBox = false;
            bool isTapered = false;

            try
            {
                var info = SectionService.DetectSelectedSection(_os);
                if (info != null)
                {
                    if (info.PropType == eFramePropType.Pipe || info.PropType == eFramePropType.Box)
                        isPipeOrBox = true;
                    if (string.Equals(info.Code, "Tapered", StringComparison.OrdinalIgnoreCase))
                        isTapered = true;
                }
            }
            catch (Exception ex)
            {
                WarnOnce("UpdateBuiltUpOption:Detect", "Could not detect section type from STAAD selection. Falling back to name-based detection.", ex);
            }

            string sectionName = TryGetSectionName(memberNo).ToUpperInvariant();
            if (sectionName.Contains("PIPE") || sectionName.Contains("HSS") || sectionName.Contains("TUBE") || sectionName.Contains("BOX"))
                isPipeOrBox = true;
            if (sectionName.Contains("TAPER"))
                isTapered = true;

            chkIsBuiltUp.Visible = !isPipeOrBox;
            chkIsBuiltUp.Enabled = !isTapered;
            if (isTapered)
                chkIsBuiltUp.Checked = true;
        }

    }
}
