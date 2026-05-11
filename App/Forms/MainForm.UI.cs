using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
         private void UpdateVisibility()
         {
             // Start from the full prismatic input set, then hide fields per type.
             txtH1.Visible = lblH1.Visible = true;
             txtH2.Visible = lblH2.Visible = true;
             txtTw.Visible = lblTw.Visible = true;
             txtBf.Visible = lblBf.Visible = true;
             txtTf.Visible = lblTf.Visible = true;
             txtBfb.Visible = lblBfb.Visible = true;
             txtTfb.Visible = lblTfb.Visible = true;
             txtDis.Visible = lblDis.Visible = false;

            // Legacy catalog selectors are shown only for Legacy mode.
            lblZone.Visible = cboZone.Visible = false;
            lblShape.Visible = cboShape.Visible = false;
            lblSeries.Visible = cboSeries.Visible = false;
            lblSection.Visible = cboSection.Visible = false;

            string type = cboType.Text;

            // Code field indicates the current section source.
            if (type == "Legacy") txtCode.Text = "Legacy";
            else txtCode.Text = chkUPT.Checked ? "UPT (User)" : "Prismatic";

            // UPT names are user-editable; other section names are generated from dimensions.
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
                 // UPT angle creation does not use TW.
                 txtH2.Visible = lblH2.Visible = false;
                 txtBfb.Visible = lblBfb.Visible = false;
                 txtTfb.Visible = lblTfb.Visible = false;
                 txtTw.Visible = lblTw.Visible = false;

                 bool isUpt = (type == "ANGLE" || type == "DOUBLE ANGLE");
                 if (!isUpt) {
                     txtTw.Visible = lblTw.Visible = true;
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
                // Circle-D300
                name = $"Circle-D{h1:0}";
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
                // I-(500~1200)x400x10x12 or I-500x400x10x12
                string depthStr = Math.Abs(h1 - h2) > 0.1 ? $"({h1:0}~{h2:0})" : $"{h1:0}";
                name = $"I-{depthStr}x{bf:0}x{tw:0}x{tf:0}";

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

    }
}
