using System;
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
            else if (typeStr == "Tapered Pipe")
            {
                info.PropType = eFramePropType.Pipe;
                info.TaperedTubeShape = TaperedTubeShape.Round;
            }
            else if (typeStr == "Tapered Tube")
            {
                info.PropType = eFramePropType.Pipe;
                info.TaperedTubeShape = TaperedTubeShape.Square;
            }

            else if (typeStr == "Trapezoidal") info.PropType = eFramePropType.Trapezoidal;
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
    }
}
