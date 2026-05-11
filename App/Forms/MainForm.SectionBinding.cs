using System;
using System.Collections.Generic;
using System.Linq;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void SetSectionInfoToUI(SectionInfo info, object openStaad = null)
        {
            if (info == null) return;

            PopulateLegacySectionMetadata(info);

            bool isUpt = info.CreateAsUPT
                || (!string.IsNullOrEmpty(info.Code)
                    && info.Code.StartsWith("UPT", StringComparison.OrdinalIgnoreCase));
            if (chkUPT.Checked != isUpt)
                chkUPT.Checked = isUpt;

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

            int idx = cboType.FindStringExact(typeStr);
            if (idx >= 0) cboType.SelectedIndex = idx;
            else
            {
                idx = cboType.FindStringExact(isUpt ? "WIDE FLANGE" : "General");
                if (idx >= 0) cboType.SelectedIndex = idx;
            }

            EnsureMaterialListLoaded(openStaad, info.Material);

            txtCode.Text = string.IsNullOrWhiteSpace(info.Code) ? "None" : info.Code;
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

            if (info.PropType == eFramePropType.Legacy)
            {
                _legacyUiLoading = true;
                try
                {
                    foreach (var item in cboZone.Items)
                    {
                        if (item is StaadSteelApp.LegacyDatabaseService.ZoneInfo z && z.StaadCountryCode == info.CountryCode)
                        {
                            cboZone.SelectedItem = item;
                            break;
                        }
                    }
                    LoadLegacyShapes();

                    if (!string.IsNullOrEmpty(info.ShapeName))
                    {
                        int shapeIdx = cboShape.FindStringExact(info.ShapeName);
                        if (shapeIdx >= 0)
                        {
                            cboShape.SelectedIndex = shapeIdx;
                        }
                    }
                    LoadLegacySeries();

                    string seriesGuess = ExtractSeriesPrefix(info.Name, info.ShapeName);
                    if (!string.IsNullOrEmpty(seriesGuess))
                    {
                        int exact = cboSeries.FindStringExact(seriesGuess);
                        if (exact >= 0)
                            cboSeries.SelectedIndex = exact;
                        else if (cboSeries.Items.Count > 0)
                            cboSeries.SelectedIndex = 0;
                    }
                    LoadLegacySections();

                    for (int i = 0; i < cboSection.Items.Count; i++)
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

            txtName.Text = info.Name;
        }

        private void ApplyDetectedSection(object openStaad, SectionInfo info)
        {
            if (info == null)
            {
                TopMostMessageBox.ShowWarning("No frame selected or no property assigned.", "Check Selection");
                return;
            }

            _isPopulating = true;
            try
            {
                EnsureMaterialListLoaded(openStaad, info.Material);

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

                txtCode.Text = info.Code;
                txtName.Text = info.Name;

                if (!string.IsNullOrWhiteSpace(info.Material))
                    cboMaterial.Text = info.Material;
                else if (cboMaterial.Items.Count > 0)
                    cboMaterial.SelectedIndex = 0;

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

                if (string.Equals(info.Code, "Legacy", StringComparison.OrdinalIgnoreCase)
                    || (!string.IsNullOrEmpty(info.Code)
                        && info.Code.StartsWith("UPT", StringComparison.OrdinalIgnoreCase)))
                {
                    txtName.Text = info.Name;
                    if (string.Equals(info.Code, "Legacy", StringComparison.OrdinalIgnoreCase))
                        TryAutoSelectLegacy(info.Name);
                }
                else
                {
                    _isPopulating = false;
                    UpdateName();
                    _isPopulating = true;
                }
            }
            finally
            {
                _isPopulating = false;
            }
        }

        private void SetCbo(string val)
        {
            for (int i = 0; i < cboType.Items.Count; i++)
            {
                if (cboType.Items[i].ToString().Equals(val, StringComparison.OrdinalIgnoreCase))
                {
                    cboType.SelectedIndex = i;
                    return;
                }
            }

            for (int i = 0; i < cboType.Items.Count; i++)
            {
                if (cboType.Items[i].ToString().StartsWith(val, StringComparison.OrdinalIgnoreCase))
                {
                    cboType.SelectedIndex = i;
                    return;
                }
            }

            if (cboType.Items.Count > 0) cboType.SelectedIndex = 0;
        }
    }
}
