using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private SectionInfo GetSectionInfoFromUI()
        {
            SectionInfo info = new SectionInfo();
            info.Code = txtCode.Text;
            info.Name = txtName.Text;
            info.Material = cboMaterial.Text;
            info.CreateAsUPT = chkUPT.Checked;

            if (string.Equals(info.Name, "None", StringComparison.OrdinalIgnoreCase))
                info.Name = "";

            // Get STAAD API Country Code from Legacy (NOT XML CountryId!)
            if (cboZone.SelectedItem is StaadSteelApp.LegacyDatabaseService.ZoneInfo z)
            {
                info.CountryCode = z.StaadCountryCode;
            }

            // Get Type
            string typeStr = cboType.Text;

            if (typeStr == "Circle") info.PropType = eFramePropType.Circle;
            else if (typeStr == "Rectangle") info.PropType = eFramePropType.Rectangular;
            else if (typeStr == "Tapered I") info.PropType = eFramePropType.I;
            else if (typeStr == "Tee" || typeStr == "TEE") info.PropType = eFramePropType.T;
            else if (typeStr == "Channel" || typeStr == "CHANNEL") info.PropType = eFramePropType.Channel;
            else if (typeStr == "Angle" || typeStr == "ANGLE") info.PropType = eFramePropType.Angle;
            else if (typeStr == "2L" || typeStr == "DOUBLE ANGLE") info.PropType = eFramePropType.DblAngle;
            else if (typeStr == "2C") info.PropType = eFramePropType.DbChannel;
            else if (typeStr == "Tapered Tube") info.PropType = eFramePropType.Pipe; // Might be incorrect, but keeping legacy mapping

            else if (typeStr == "Trapezoidal") info.PropType = eFramePropType.General; 
            else if (typeStr == "General" || typeStr == "GENERAL") info.PropType = eFramePropType.General;
            else if (typeStr == "Legacy") info.PropType = eFramePropType.Legacy;
            else if (typeStr == "WIDE FLANGE") info.PropType = eFramePropType.I;
            else if (typeStr == "TUBE") info.PropType = eFramePropType.Rectangular;
            else if (typeStr == "PIPE") info.PropType = eFramePropType.Pipe;
            else if (typeStr == "ISECTION") info.PropType = eFramePropType.ISection;
            else if (typeStr == "PRISMATIC") info.PropType = eFramePropType.Prismatic;

            // ... other types
            else info.PropType = eFramePropType.I; // Default

            // Get Dims (Parse handle empty/invalid)
            info.H1 = Parse(txtH1.Text);
            info.H2 = Parse(txtH2.Text);
            info.Tw = Parse(txtTw.Text);
            info.Bf = Parse(txtBf.Text);
            info.Tf = Parse(txtTf.Text);
            info.Bfb = Parse(txtBfb.Text);
            info.Tfb = Parse(txtTfb.Text);
            info.Dis = Parse(txtDis.Text);

            if (typeStr == "PIPE")
            {
                double outerDiameter = info.H1;
                double wallThickness = info.Bf;

                if (outerDiameter > 0 && wallThickness > 0)
                {
                    double innerDiameter = outerDiameter - (2.0 * wallThickness);
                    if (innerDiameter <= 0)
                    {
                        TopMostMessageBox.Show(
                            "PIPE requires OD > 2 x thickness.\n" +
                            $"Current values: OD = {SectionNamingService.FmtNum(outerDiameter)}, Thk = {SectionNamingService.FmtNum(wallThickness)}.");
                        return null;
                    }

                    info.Tw = wallThickness;
                    info.Bf = innerDiameter;
                }
            }

            // For Legacy: Capture Shape name and STAAD section name
            if (typeStr == "Legacy" && cboShape.SelectedItem != null)
            {
                info.ShapeName = cboShape.SelectedItem.ToString();
                if (cboSection.SelectedItem is StaadSteelApp.LegacyDatabaseService.SectionItem item)
                {
                    info.Name = item.StaadName;
                }
            }

            if (_sidePanel != null && _sidePanel.Visible && typeStr == "Legacy")
            {
                info.LegacyTypeSpec = _sidePanel.SelectedItem?.Value ?? 0;
                if (_sidePanel.SelectedItem != null)
                {
                    switch (_sidePanel.SelectedItem.Mode)
                    {
                        case LegacySpecMode.AngleSpacing:
                        case LegacySpecMode.ChannelSpacing:
                        case LegacySpecMode.DoubleProfile:
                            info.LegacySpec1 = ParseLegacyLength(_sidePanel.Spec1Text);
                            break;
                        case LegacySpecMode.Composite:
                            info.LegacySpec1 = ParseLegacyLength(_sidePanel.Spec1Text);
                            info.LegacySpec2 = ParseLegacyValue(_sidePanel.Spec2Text);
                            info.LegacySpec3 = ParseLegacyLength(_sidePanel.Spec3Text);
                            info.LegacySpec4 = ParseLegacyValue(_sidePanel.Spec4Text);
                            break;
                        case LegacySpecMode.TopCover:
                        case LegacySpecMode.BottomCover:
                            info.LegacySpec1 = ParseLegacyLength(_sidePanel.Spec1Text);
                            info.LegacySpec2 = ParseLegacyLength(_sidePanel.Spec2Text);
                            break;
                        case LegacySpecMode.TopBottomCover:
                            info.LegacySpec1 = ParseLegacyLength(_sidePanel.Spec1Text);
                            info.LegacySpec2 = ParseLegacyLength(_sidePanel.Spec2Text);
                            info.LegacySpec3 = ParseLegacyLength(_sidePanel.Spec3Text);
                            info.LegacySpec4 = ParseLegacyLength(_sidePanel.Spec4Text);
                            break;
                    }
                }
            }

            if (typeStr == "Legacy" && info.LegacyTypeSpec != 0)
            {
                var typeItem = _sidePanel?.SelectedItem;
                if (!ValidateLegacyTypeSpecInputs(typeItem, info))
                {
                    return null;
                }
            }

            if (typeStr.Contains("Tapered") && string.IsNullOrEmpty(info.Name))
            {
                // Allow empty name
            }
            else if (string.IsNullOrEmpty(info.Name))
            {
                TopMostMessageBox.Show("Please enter a Section Name.");
                return null;
            }
            return info;
        }

         private void UpdateVisibility()
         {
             // Reset all
             txtH1.Visible = lblH1.Visible = true;
             txtH2.Visible = lblH2.Visible = true;
             txtTw.Visible = lblTw.Visible = true;
             txtBf.Visible = lblBf.Visible = true;
             txtTf.Visible = lblTf.Visible = true;
             txtBfb.Visible = lblBfb.Visible = true;
             txtTfb.Visible = lblTfb.Visible = true;
             txtDis.Visible = lblDis.Visible = false;

            // Hide Legacy by default
            lblZone.Visible = cboZone.Visible = false;
            lblShape.Visible = cboShape.Visible = false;
            lblSeries.Visible = cboSeries.Visible = false;
            lblSection.Visible = cboSection.Visible = false;
            // grpLegacyTypeSpec.Visible = false; // Removed logic

            string type = cboType.Text;

            // Update Code field to show source
            if (type == "Legacy") txtCode.Text = "Legacy";
            else txtCode.Text = chkUPT.Checked ? "UPT (User)" : "Prismatic";

            // Cho phép đổi tên nếu là UPT. Nếu không phải UPT, Name mặc định là ReadOnly và tự gen.
            txtName.ReadOnly = !chkUPT.Checked;

            if (type == "Circle" || type == "PRISMATIC")
            {
                // PRISMATIC UPT expects AX, IZ, IY, IX, AY, AZ, YD, ZD 
                txtH2.Visible = lblH2.Visible = (type == "PRISMATIC");
                txtTw.Visible = lblTw.Visible = false;
                txtBf.Visible = lblBf.Visible = false;
                txtTf.Visible = lblTf.Visible = false;
                txtBfb.Visible = lblBfb.Visible = false;
                txtTfb.Visible = lblTfb.Visible = false;

                lblH1.Text = type == "Circle" ? "Diam (YD)" : "YD";
                lblH2.Text = "ZD";
            }
            else if (type == "Tapered Tube")
            {
                // d1, d2, th
                txtBf.Visible = lblBf.Visible = false;
                txtTf.Visible = lblTf.Visible = false;
                txtBfb.Visible = lblBfb.Visible = false;
                txtTfb.Visible = lblTfb.Visible = false;

                lblH1.Text = "Dep Start"; lblH2.Text = "Dep End"; lblTw.Text = "Thk";
            }
            else if (type == "Rectangle" || type == "GENERAL")
            {
                // YD, ZD or General params
                txtBf.Visible = lblBf.Visible = false;
                txtBfb.Visible = lblBfb.Visible = false;
                txtTfb.Visible = lblTfb.Visible = false;
                lblH1.Text = type == "GENERAL" ? "D" : "Depth (YD)"; 
                lblH2.Text = type == "GENERAL" ? "B" : "Width (ZD)";

                if (type == "Rectangle")
                {
                    txtTw.Visible = lblTw.Visible = false;
                    txtTf.Visible = lblTf.Visible = false;
                }
                else
                {
                    // GENERAL: AddUPTPropertyGENERAL(..., varD, varTD, varB, varTB, ...)
                    txtTw.Visible = lblTw.Visible = true;
                    txtTf.Visible = lblTf.Visible = true;
                    lblTw.Text = "TD"; lblTf.Text = "TB";
                }
            }
            else if (type == "Channel" || type == "Double Channel" || type == "CHANNEL")
            {
                // AddUPTPropertyCHANNEL(..., varD, varTW, varWF, varTF, ...)
                txtH2.Visible = lblH2.Visible = false;
                txtBfb.Visible = lblBfb.Visible = false;
                txtTfb.Visible = lblTfb.Visible = false;

                lblH1.Text = type == "CHANNEL" ? "D" : "Depth"; 
                lblBf.Text = type == "CHANNEL" ? "WF" : "Width"; 
                lblTw.Text = type == "CHANNEL" ? "TW" : "Web Thk"; 
                lblTf.Text = type == "CHANNEL" ? "TF" : "Flange Thk";
            }
            else if (type == "Tee" || type == "TEE")
            {
                // AddUPTPropertyTEE(..., varD, varWF, varTF, varTW, ...)
                txtH2.Visible = lblH2.Visible = false;
                txtBfb.Visible = lblBfb.Visible = false;
                txtTfb.Visible = lblTfb.Visible = false;

                lblH1.Text = type == "TEE" ? "D" : "Depth"; 
                lblBf.Text = type == "TEE" ? "WF" : "Width"; 
                lblTw.Text = type == "TEE" ? "TW" : "Stem Thk"; 
                lblTf.Text = type == "TEE" ? "TF" : "Flange Thk";
            }
            else if (type == "Angle" || type == "Double Angle" || type == "ANGLE" || type == "DOUBLE ANGLE")
            {
                 // AddUPTPropertyANGLE(..., varD, varWF, varTF, varR, varAY, varAZ)
                 // AddUPTPropertyDOUBLEANGLE(..., varD, varWF, varTF, varSP, ...)
                 // => No TW!
                 txtH2.Visible = lblH2.Visible = false;
                 txtBfb.Visible = lblBfb.Visible = false;
                 txtTfb.Visible = lblTfb.Visible = false;
                 txtTw.Visible = lblTw.Visible = false;
 
                 bool isUpt = (type == "ANGLE" || type == "DOUBLE ANGLE");
                 if (!isUpt) {
                     txtTw.Visible = lblTw.Visible = true; // App's default Angle might need Thk2
                 }

                 lblH1.Text = isUpt ? "D" : "Leg 1"; 
                 lblBf.Text = isUpt ? "WF" : "Leg 2"; 
                 lblTf.Text = isUpt ? "TF" : "Thk"; 
                 lblTw.Text = isUpt ? "TW" : "Thk2";

                 if (type == "DOUBLE ANGLE")
                 {
                     txtDis.Visible = lblDis.Visible = true;
                     lblDis.Text = "SP"; // Space
                 }
            }
            else if (type == "2L")
            {
                 // 2L: same visible dims as Angle, plus gap (Dis)
                 txtH2.Visible = lblH2.Visible = false;
                 txtBfb.Visible = lblBfb.Visible = false;
                 txtTfb.Visible = lblTfb.Visible = false;
                 txtDis.Visible = lblDis.Visible = true;

                 lblH1.Text = "Leg 1"; lblBf.Text = "Leg 2"; lblTf.Text = "Thk"; lblTw.Text = "Thk2";
                 lblDis.Text = "Gap";
            }
            else if (type == "2C")
            {
                 // 2C: same visible dims as Channel, plus gap (Dis)
                 txtH2.Visible = lblH2.Visible = false;
                 txtBfb.Visible = lblBfb.Visible = false;
                 txtTfb.Visible = lblTfb.Visible = false;
                 txtDis.Visible = lblDis.Visible = true;

                 lblH1.Text = "Depth"; lblBf.Text = "Width"; lblTw.Text = "Web Thk"; lblTf.Text = "Flange Thk";
                 lblDis.Text = "Gap";
            }
            else if (type == "WIDE FLANGE" || type == "ISECTION")
            {
                // AddUPTPropertyWIDEFLANGE(..., varD, varTW, varWF, varTF, ...)
                txtH2.Visible = lblH2.Visible = false;
                txtBfb.Visible = lblBfb.Visible = false;
                txtTfb.Visible = lblTfb.Visible = false;

                if (type == "ISECTION")
                {
                    txtH2.Visible = lblH2.Visible = true;
                    txtBfb.Visible = lblBfb.Visible = true;
                    txtTfb.Visible = lblTfb.Visible = true;
                    lblH1.Text = "DWW"; lblH2.Text = "DWW1";
                    lblTw.Text = "TWW"; 
                    lblBf.Text = "BFF"; lblTf.Text = "TFF";
                    lblBfb.Text = "BFF1"; lblTfb.Text = "TFF1";
                }
                else
                {
                    lblH1.Text = "D"; 
                    lblBf.Text = "WF"; 
                    lblTw.Text = "TW"; 
                    lblTf.Text = "TF";
                }
            }
            else if (type == "TUBE" || type == "PIPE")
            {
                // AddUPTPropertyTUBE(..., varD, varWF, varTF, ...) => No TW
                // UI accepts PIPE as OD + thickness, then converts to OD + ID before Create.
                txtH2.Visible = lblH2.Visible = false;
                txtTw.Visible = lblTw.Visible = false;
                txtBfb.Visible = lblBfb.Visible = false;
                txtTfb.Visible = lblTfb.Visible = false;
                
                if (type == "PIPE")
                {
                    txtTf.Visible = lblTf.Visible = false;
                    lblH1.Text = "OD"; lblBf.Text = "Thk";
                }
                else // TUBE
                {
                    lblH1.Text = "D"; lblBf.Text = "WF"; lblTf.Text = "TF";
                }
            }
            else if (type == "Legacy")
            {
                 // New Legacy Mode
                 lblH1.Visible = txtH1.Visible = false;
                 lblH2.Visible = txtH2.Visible = false;
                lblTw.Visible = txtTw.Visible = false;
                lblBf.Visible = txtBf.Visible = false;
                lblTf.Visible = txtTf.Visible = false;
                lblBfb.Visible = txtBfb.Visible = false;
                lblTfb.Visible = txtTfb.Visible = false;
                lblDis.Visible = txtDis.Visible = false;

                // Show Legacy Controls
                lblZone.Visible = cboZone.Visible = true;
                lblShape.Visible = cboShape.Visible = true;
                lblSeries.Visible = cboSeries.Visible = true;
                lblSection.Visible = cboSection.Visible = true;

                // Ensure loaded
                if (cboZone.Items.Count == 0 && _legacyService != null)
                {
                    LoadLegacyZones();
                }

                lblBfb.Text = "Bfb (Bot)"; lblTfb.Text = "Tfb (Bot)";
            }

            UpdateLegacyTypeSpecUI();
        }

         private void UpdateName()
         {
             // Skip if populating from detect
             if (_isPopulating) return;

            // Auto-generate descriptive name based on type and dimensions
            string type = cboType.Text;
            double h1 = Parse(txtH1.Text);
            double h2 = Parse(txtH2.Text);
            double tw = Parse(txtTw.Text);
            double bf = Parse(txtBf.Text);
             double tf = Parse(txtTf.Text);
             double bfb = Parse(txtBfb.Text);
             double tfb = Parse(txtTfb.Text);
             double dis = Parse(txtDis.Text);

             string name = "";

             if (chkUPT.Checked)
             {
                 name = SectionNamingService.GenerateUptSectionName(type, h1, h2, tw, bf, tf, bfb, tfb, dis);
                 txtName.Text = name;
                 return;
             }

            if (type == "Circle")
            {
                // Circle-Ã˜300
                name = $"Circle-Ã˜{h1:0}";
            }
            else if (type == "Rectangle")
            {
                // Rect-200x150
                name = $"Rect-{h1:0}x{h2:0}";
            }
             else if (type == "Tee")
             {
                 // T-300x200x10x15
                 name = $"T-{h1:0}x{bf:0}x{tw:0}x{tf:0}";
             }
             else if (type == "Channel" || type == "Double Channel")
             {
                 name = SectionNamingService.GenerateSectionName(eFramePropType.Channel, h1, h2, tw, bf, tf, bfb, tfb, dis);
             }
             else if (type == "Angle" || type == "Double Angle")
             {
                 name = SectionNamingService.GenerateSectionName(eFramePropType.Angle, h1, h2, tw, bf, tf, bfb, tfb, dis);
             }
             else if (type == "2L")
             {
                 name = SectionNamingService.GenerateSectionName(eFramePropType.DblAngle, h1, h2, tw, bf, tf, bfb, tfb, dis);
             }
             else if (type == "2C")
             {
                 name = SectionNamingService.GenerateSectionName(eFramePropType.DbChannel, h1, h2, tw, bf, tf, bfb, tfb, dis);
             }
             else if (type == "Tapered Tube")
             {
                 // Tube-(300~400)x10
                 if (Math.Abs(h1 - h2) > 0.1)
                     name = $"Tube-({h1:0}~{h2:0})x{tw:0}";
                else
                    name = $"Tube-{h1:0}x{tw:0}";
            }
            else if (type == "Tapered I" || type == "I")
            {
                // I-(500~1200)x10x400x12 or I-500x10x400x12
                string depthStr = Math.Abs(h1 - h2) > 0.1 ? $"({h1:0}~{h2:0})" : $"{h1:0}";
                name = $"I-{depthStr}x{tw:0}x{bf:0}x{tf:0}";

                // Add bottom flange if different
                if (Math.Abs(bf - bfb) > 0.1 || Math.Abs(tf - tfb) > 0.1)
                    name += $"/{bfb:0}x{tfb:0}";
            }
            else if (type == "Legacy")
            {
                // Keep name from dropdown selection
                return;
            }

            txtName.Text = name;
        }

        private double Parse(string s)
        {
            if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double v))
                return v;
            if (double.TryParse(s, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.CurrentCulture, out v))
                return v;
            return 0;
        }

        private double ParseLegacyLength(string s)
        {
            // Type Spec lengths are entered in mm; STAAD API expects meters.
            return Parse(s) / 1000.0;
        }
        private double ParseLegacyValue(string s) { return Parse(s); }

        private void EnsureMaterialListLoaded(dynamic os, string selectedMaterial)
        {
            if (os == null) return;

            if (!_materialListLoaded)
            {
                var mats = SectionService.GetIsotropicMaterialNames(os);
                if (mats.Count > 0)
                {
                    cboMaterial.Items.Clear();
                    cboMaterial.Items.Add("None");
                    foreach (var m in mats)
                    {
                        if (!cboMaterial.Items.Contains(m))
                            cboMaterial.Items.Add(m);
                    }
                    _materialListLoaded = true;
                }
            }

            string preferred = string.IsNullOrWhiteSpace(selectedMaterial) ? "None" : selectedMaterial;
            if (!string.IsNullOrWhiteSpace(preferred))
            {
                if (!cboMaterial.Items.Contains(preferred))
                {
                    cboMaterial.Items.Insert(0, preferred);
                }
                cboMaterial.Text = preferred;
            }
        }

        private void grpDimensions_Enter(object sender, EventArgs e)
        {
        }

        // --- UX Helpers ---

        private void SetupInputFields()
        {
            var boxes = new[] { txtH1, txtH2, txtTw, txtBf, txtTf, txtBfb, txtTfb, txtDis };
            foreach (var box in boxes)
            {
                SetupNumericBox(box);
            }
        }

        private void SetupNumericBox(TextBox box)
        {
            box.Enter += Box_Enter;
            box.KeyPress += Box_KeyPress;
            box.Click += Box_Click; /* Ensure click also triggers if already focused logic needed,
                                     * but Enter with BeginInvoke usually handles the initial focus click nicely.
                                     * Adding Click just in case user clicks again?
                                     * Actually User said "click into... entire cell green".
                                     * BeginInvoke in Enter handles the focus-by-click case.
                                     */
        }

        private void Box_Enter(object sender, EventArgs e)
        {
            var box = sender as TextBox;
            if (box == null) return;
            // Use BeginInvoke to ensure SelectAll happens after any default mouse placement
            BeginInvoke((Action)delegate { box.SelectAll(); });
        }

        // Optional: If user clicks into an already focused box, do they want Select All again?
        // Standard behavior is usually: First click (Get Focus) -> Select All. Second click -> Place Cursor.
        // User request: "click into... select all". "click" implies getting focus usually.
        // I will stick to just Enter + BeginInvoke which is the standard "Excel-like" behavior for entering a cell of a grid.
        // But to be safe and strictly follow "click into a dimension... selected", Enter covers the "Click to Focus" case.

        private void Box_Click(object sender, EventArgs e)
        {
            // If we really want to enforce select all on every click even if focused, it might be annoying for editing.
            // But "Enter" covers the transition. I will leave Click empty or remove it if not needed.
            // Actually, the "Click" event fires after "Enter" if clicking gives focus.
            // BeginInvoke in Enter is the robust way.
        }

        private void Box_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Allow control chars (Backspace, etc.)
            if (char.IsControl(e.KeyChar)) return;

            // Allow Digits
            if (char.IsDigit(e.KeyChar)) return;

            // Allow ONE decimal point
            if (e.KeyChar == '.' && !((TextBox)sender).Text.Contains('.')) return;

            // Block everything else
            e.Handled = true;
        }
    }
}
