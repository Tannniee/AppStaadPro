using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
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

            var os = StaadManager.GetOpenSTAAD();
            if (os != null)
            {
                try
                {
                    info = SectionService.DetectSelectedSection(os);
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
                    SetSectionInfoToUI(info, os);
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
            // Debug: Confirm click
            // MessageBox.Show("DEBUG: Paste Clicked. HasData: " + SectionClipboard.HasData);

            var info = SectionClipboard.GetCopy();
            if (info == null)
            {
                TopMostMessageBox.ShowWarning("Clipboard is empty.", "Paste");
                UpdateClipboardUi();
                return;
            }

            PopulateLegacySectionMetadata(info);

            var os = StaadManager.GetOpenSTAAD();

            _isPopulating = true;
            try
            {
                SetSectionInfoToUI(info, os);
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

            if (os != null)
            {
                string selectError = "";
                var beams = SectionService.GetSelectedBeamIDsSafe(os, out selectError);
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
                    SectionService.AssignSectionToSelection(os, info);
                    RefreshUsedSectionsListIfVisible();
                }
                catch (Exception ex)
                {
                    TopMostMessageBox.ShowError("Error assigning section: " + ex.Message, "Paste Error");
                }
            }
        }

        private void SetSectionInfoToUI(SectionInfo info, dynamic os = null)
        {
            if (info == null) return;

            PopulateLegacySectionMetadata(info);

            bool isUpt = info.CreateAsUPT
                || (!string.IsNullOrEmpty(info.Code)
                    && info.Code.StartsWith("UPT", StringComparison.OrdinalIgnoreCase));
            if (chkUPT.Checked != isUpt)
                chkUPT.Checked = isUpt;

            // 1. Set Main Type
            // Reverse mapping PropType -> cboType Text
            string typeStr = "";
            switch (info.PropType)
            {
                case eFramePropType.Circle: typeStr = isUpt ? "PRISMATIC" : "Circle"; break;
                case eFramePropType.Rectangular: typeStr = isUpt ? "TUBE" : "Rectangle"; break;
                case eFramePropType.I: typeStr = isUpt ? "WIDE FLANGE" : "Tapered I"; break;
                case eFramePropType.T: typeStr = isUpt ? "TEE" : "Tee"; break;
                case eFramePropType.Channel: typeStr = isUpt ? "CHANNEL" : "Channel"; break;
                case eFramePropType.Angle: typeStr = isUpt ? "ANGLE" : "Angle"; break;
                case eFramePropType.DblAngle: typeStr = isUpt ? "DOUBLE ANGLE" : "2L"; break;
                case eFramePropType.DbChannel: typeStr = "2C"; break;
                case eFramePropType.Pipe: typeStr = isUpt ? "PIPE" : "Tapered Tube"; break;
                case eFramePropType.General: typeStr = isUpt ? "GENERAL" : "General"; break;
                case eFramePropType.ISection: typeStr = "ISECTION"; break;
                case eFramePropType.Prismatic: typeStr = "PRISMATIC"; break;
                case eFramePropType.Legacy: typeStr = "Legacy"; break;
                default: typeStr = isUpt ? "WIDE FLANGE" : "Tapered I"; break;
            }

            // Try setting by text
            int idx = cboType.FindStringExact(typeStr);
            if (idx >= 0) cboType.SelectedIndex = idx;
            else
            {
                // Fallback 
                idx = cboType.FindStringExact(isUpt ? "WIDE FLANGE" : "General");
                if (idx >= 0) cboType.SelectedIndex = idx;
            }

            // 2. Set Material
            EnsureMaterialListLoaded(os, info.Material); 

            // Keep source indicator in sync with copied data.
            txtCode.Text = string.IsNullOrWhiteSpace(info.Code) ? "None" : info.Code;
            
            // 3. Set Dimensions
            txtH1.Text = SectionNamingService.FmtNum(info.H1);
            txtH2.Text = SectionNamingService.FmtNum(info.H2);
            txtTw.Text = SectionNamingService.FmtNum(info.Tw);
            txtBf.Text = isUpt && info.PropType == eFramePropType.Pipe
                ? SectionNamingService.FmtNum(info.Tw)
                : SectionNamingService.FmtNum(info.Bf);
            txtTf.Text = SectionNamingService.FmtNum(info.Tf);
            txtBfb.Text = SectionNamingService.FmtNum(info.Bfb);
            txtTfb.Text = SectionNamingService.FmtNum(info.Tfb);
            txtDis.Text = SectionNamingService.FmtNum(info.Dis);

            // 4. Set Legacy specifics
            if (info.PropType == eFramePropType.Legacy)
            {
                // Suppress cascade events — drive each step explicitly
                _legacyUiLoading = true;
                try
                {
                    // Zone (CountryCode)
                    foreach (var item in cboZone.Items)
                    {
                        if (item is StaadSteelApp.LegacyDatabaseService.ZoneInfo z && z.StaadCountryCode == info.CountryCode)
                        {
                            cboZone.SelectedItem = item;
                            break;
                        }
                    }
                    LoadLegacyShapes(); // Explicit cascade instead of Application.DoEvents()

                    // Shape
                    if (!string.IsNullOrEmpty(info.ShapeName))
                    {
                        int shapeIdx = cboShape.FindStringExact(info.ShapeName);
                        if (shapeIdx >= 0)
                        {
                            cboShape.SelectedIndex = shapeIdx;
                        }
                    }
                    LoadLegacySeries(); // Explicit cascade instead of Application.DoEvents()
                
                    // Series & Section
                    string seriesGuess = ExtractSeriesPrefix(info.Name, info.ShapeName);
                    if (!string.IsNullOrEmpty(seriesGuess))
                    {
                        int exact = cboSeries.FindStringExact(seriesGuess);
                        if (exact >= 0) 
                            cboSeries.SelectedIndex = exact;
                        else
                            if (cboSeries.Items.Count > 0) cboSeries.SelectedIndex = 0; 
                    }
                    LoadLegacySections(); // Explicit cascade instead of Application.DoEvents()

                    // Section
                    for(int i=0; i<cboSection.Items.Count; i++)
                    {
                        var item = cboSection.Items[i];
                        string txt = "";
                        string sName = "";
                        if (item is StaadSteelApp.LegacyDatabaseService.SectionItem sec)
                        {
                            txt = sec.DisplayName;
                            sName = sec.StaadName;
                        }
                        else
                        {
                            txt = item.ToString();
                        }

                        if (string.Equals(sName, info.Name, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(txt, info.Name, StringComparison.OrdinalIgnoreCase))
                        {
                            cboSection.SelectedIndex = i;
                            break;
                        }
                    }

                    // 5. Side Panel (Type Spec)
                     if (info.LegacyTypeSpec != 0 && _sidePanel != null)
                     {
                         if (!_sidePanel.Visible) _sidePanel.Show(this);
                     
                         List<LegacyTypeSpecItem> list = new List<LegacyTypeSpecItem>();
                         if (IsLegacyAngleShape(info.ShapeName)) list = GetAngleTypeSpecItems();
                         else if (IsLegacyChannelShape(info.ShapeName)) list = GetChannelTypeSpecItems();
                         else if (IsLegacyWideFlangeShape(info.ShapeName)) list = GetWideFlangeTypeSpecItems();
                     
                         if (list.Count > 0)
                         {
                             var targetItem = list.FirstOrDefault(x => x.Value == info.LegacyTypeSpec);

                             // UI expects legacy length specs in mm (GetSectionInfoFromUI parses mm -> m).
                             double ToMm(double m) => m * 1000.0;
                             string Len(double m) => SectionNamingService.FmtNum(ToMm(m));
                             string Val(double v) => SectionNamingService.FmtNum(v);

                             string s1 = "", s2 = "", s3 = "", s4 = "";
                             switch (targetItem?.Mode ?? LegacySpecMode.None)
                             {
                                 case LegacySpecMode.AngleSpacing:
                                 case LegacySpecMode.ChannelSpacing:
                                 case LegacySpecMode.DoubleProfile:
                                     s1 = Len(info.LegacySpec1);
                                     break;
                                 case LegacySpecMode.Composite:
                                     s1 = Len(info.LegacySpec1);
                                     s2 = Val(info.LegacySpec2);
                                     s3 = Len(info.LegacySpec3);
                                     s4 = Val(info.LegacySpec4);
                                     break;
                                 case LegacySpecMode.TopCover:
                                 case LegacySpecMode.BottomCover:
                                     s1 = Len(info.LegacySpec1);
                                     s2 = Len(info.LegacySpec2);
                                     break;
                                 case LegacySpecMode.TopBottomCover:
                                     s1 = Len(info.LegacySpec1);
                                     s2 = Len(info.LegacySpec2);
                                     s3 = Len(info.LegacySpec3);
                                     s4 = Len(info.LegacySpec4);
                                     break;
                             }

                             _sidePanel.Initialize(list, targetItem, s1, s2, s3, s4);
                         }
                     }
                 }
                 finally
                 {
                    _legacyUiLoading = false;
                }
            }
            
            // Set Name last to override auto-generation if needed
            txtName.Text = info.Name;
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
