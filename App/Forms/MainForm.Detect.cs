using System;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void btnDetect_Click(object sender, EventArgs e)
        {
            try
            {
                var model = StaadManager.GetOpenSTAAD(); // Updated to StaadManager
                if (model == null) return;

                // No prep step is needed for STAAD here.
                var info = SectionService.DetectSelectedSection(model);
                ApplyDetectedSection(model, info);
                RefreshUsedSectionsListIfVisible(info?.PropertyRefNo ?? 0);

                // No finish step is needed after detection here.
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
                var os = StaadManager.GetOpenSTAAD();
                if (os == null) return;

                var info = SectionService.DetectSelectedSection(os);
                ApplyDetectedSection(os, info);

                // Call the new STAAD logic
                SectionService.SelectSameSection_DetectAll();
                RefreshUsedSectionsListIfVisible(info?.PropertyRefNo ?? 0);
            }
            catch (Exception ex)
            {
                TopMostMessageBox.ShowError("Error: " + ex.Message, "Detect Error");
            }
        }

        private void ApplyDetectedSection(dynamic model, SectionInfo info)
        {
            if (info == null)
            {
                TopMostMessageBox.ShowWarning("No frame selected or no property assigned.", "Check Selection");
                return;
            }

            _isPopulating = true; // Prevent UpdateName during population
            try
            {
                EnsureMaterialListLoaded(model, info.Material);

                if (info.Code == "None")
                {
                    txtCode.Text = "None";
                    txtName.Text = "None";
                    txtH1.Text = "0";
                    txtH2.Text = "0";
                    txtTw.Text = "0";
                    txtBf.Text = "0";
                    txtTf.Text = "0";
                    txtBfb.Text = "0";
                    txtTfb.Text = "0";
                    txtDis.Text = "0";
                    return;
                }

                bool isUpt = info.CreateAsUPT
                    || (!string.IsNullOrEmpty(info.Code)
                        && info.Code.StartsWith("UPT", StringComparison.OrdinalIgnoreCase));
                if (chkUPT.Checked != isUpt)
                    chkUPT.Checked = isUpt;

                // Set ComboBox based on detected type and mode (UPT vs default).
                if (isUpt)
                {
                    if (info.PropType == eFramePropType.ISection) SetCbo("ISECTION");
                    else if (info.PropType == eFramePropType.Prismatic || info.PropType == eFramePropType.Circle) SetCbo("PRISMATIC");
                    else if (info.PropType == eFramePropType.General) SetCbo("GENERAL");
                    else if (info.PropType == eFramePropType.Pipe) SetCbo("PIPE");
                    else if (info.PropType == eFramePropType.Rectangular || info.PropType == eFramePropType.Box) SetCbo("TUBE");
                    else if (info.PropType == eFramePropType.Channel) SetCbo("CHANNEL");
                    else if (info.PropType == eFramePropType.T) SetCbo("TEE");
                    else if (info.PropType == eFramePropType.Angle) SetCbo("ANGLE");
                    else if (info.PropType == eFramePropType.DblAngle) SetCbo("DOUBLE ANGLE");
                    else SetCbo("WIDE FLANGE");
                }
                else
                {
                    if (info.PropType == eFramePropType.Pipe || info.PropType == eFramePropType.Circle)
                        SetCbo("Circle");
                    else if (info.PropType == eFramePropType.Rectangular || info.PropType == eFramePropType.Box)
                        SetCbo("Rectangle");
                    else if (info.PropType == eFramePropType.Channel) SetCbo("Channel");
                    else if (info.PropType == eFramePropType.T) SetCbo("Tee");
                    else if (info.PropType == eFramePropType.Angle) SetCbo("Angle");
                    else if (info.PropType == eFramePropType.DblAngle) SetCbo("2L");
                    else if (info.PropType == eFramePropType.DbChannel) SetCbo("2C");
                    else if (info.PropType == eFramePropType.I) SetCbo("Tapered I");
                    else if (info.Code == "Legacy") SetCbo("Legacy");
                    else SetCbo("Tapered I");
                }

                UpdateVisibility();

                // Populate Materials
                txtCode.Text = info.Code;
                txtName.Text = info.Name;

                if (!string.IsNullOrWhiteSpace(info.Material))
                    cboMaterial.Text = info.Material;
                else if (cboMaterial.Items.Count > 0) cboMaterial.SelectedIndex = 0;

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

                // If Legacy or UPT, try to auto-select in dropdown (keep original name for lookup)
                if (string.Equals(info.Code, "Legacy", StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(info.Code)
                        && info.Code.StartsWith("UPT", StringComparison.OrdinalIgnoreCase)))
                {
                    txtName.Text = info.Name; // Restore original name
                    if (string.Equals(info.Code, "Legacy", StringComparison.OrdinalIgnoreCase))
                        TryAutoSelectLegacy(info.Name);
                }
                else
                {
                    // Generate descriptive name from dimensions (e.g., I-(700~500)x10x300x14)
                    // Temporarily allow UpdateName to run while still inside the try block
                    _isPopulating = false;
                    UpdateName();
                    _isPopulating = true;
                }
            }
            finally
            {
                _isPopulating = false; // Re-enable UpdateName
            }
        }

        private void SetCbo(string val)
        {
            // Direct match first
            for (int i = 0; i < cboType.Items.Count; i++)
            {
                if (cboType.Items[i].ToString().Equals(val, StringComparison.OrdinalIgnoreCase))
                {
                    cboType.SelectedIndex = i;
                    return;
                }
            }
            // StartsWith match
            for (int i = 0; i < cboType.Items.Count; i++)
            {
                if (cboType.Items[i].ToString().StartsWith(val, StringComparison.OrdinalIgnoreCase))
                {
                    cboType.SelectedIndex = i;
                    return;
                }
            }
            // Default to first item
            if (cboType.Items.Count > 0) cboType.SelectedIndex = 0;
        }
    }
}
