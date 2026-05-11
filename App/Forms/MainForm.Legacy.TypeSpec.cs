using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void UpdateLegacyTypeSpecUI()
        {
            if (!string.Equals(cboType.Text, "Legacy", StringComparison.OrdinalIgnoreCase))
            {
                _sidePanel?.Hide();
                UpdateMiscPanelLocation();
                return;
            }

            string shape = cboShape.Text ?? "";
            if (IsLegacyNoTypeSpecShape(shape))
            {
                _sidePanel?.Hide();
                UpdateMiscPanelLocation();
                return;
            }

            bool isAngle = IsLegacyAngleShape(shape);
            bool isWide = IsLegacyWideFlangeShape(shape);
            bool isChannel = IsLegacyChannelShape(shape);

            if (!isAngle && !isWide && !isChannel)
            {
                _sidePanel?.Hide();
                UpdateMiscPanelLocation();
                return;
            }

            if (_sidePanel == null || _sidePanel.IsDisposed)
            {
                _sidePanel = new TypeSpecForm();
                _sidePanel.ValueChanged += (s, e) => UpdateName();
            }

            var items = isAngle ? GetAngleTypeSpecItems() : isChannel ? GetChannelTypeSpecItems() : GetWideFlangeTypeSpecItems();

            // Preserve current input values while rebuilding the available Type Spec list.
            _sidePanel.Initialize(
                items,
                _sidePanel.SelectedItem,
                _sidePanel.Spec1Text,
                _sidePanel.Spec2Text,
                _sidePanel.Spec3Text,
                _sidePanel.Spec4Text);

            if (!_sidePanel.Visible)
            {
                _sidePanel.Show(this);
                UpdateSidePanelLocation();
                UpdateMiscPanelLocation();
            }
        }

        private bool ValidateLegacyTypeSpecInputs(LegacyTypeSpecItem item, StaadSteelApp.Core.SectionInfo info)
        {
            if (item == null || item.Mode == LegacySpecMode.None) return true;

            bool HasValue(double v) => Math.Abs(v) > 1e-9;

            switch (item.Mode)
            {
                case LegacySpecMode.AngleSpacing:
                case LegacySpecMode.ChannelSpacing:
                case LegacySpecMode.DoubleProfile:
                    if (!HasValue(info.LegacySpec1))
                    {
                        TopMostMessageBox.ShowWarning("Type Spec requires SP (clear spacing) value.", "Legacy Type Spec");
                        return false;
                    }
                    break;
                case LegacySpecMode.Composite:
                    if (!HasValue(info.LegacySpec1) || !HasValue(info.LegacySpec2)
                        || !HasValue(info.LegacySpec3) || !HasValue(info.LegacySpec4))
                    {
                        TopMostMessageBox.ShowWarning("Composite Type Spec requires CT, FC, CW, and CD values.", "Legacy Type Spec");
                        return false;
                    }
                    break;
                case LegacySpecMode.TopCover:
                    if (!HasValue(info.LegacySpec1) || !HasValue(info.LegacySpec2))
                    {
                        TopMostMessageBox.ShowWarning("Top Cover Type Spec requires WP and TH values.", "Legacy Type Spec");
                        return false;
                    }
                    break;
                case LegacySpecMode.BottomCover:
                    if (!HasValue(info.LegacySpec1) || !HasValue(info.LegacySpec2))
                    {
                        TopMostMessageBox.ShowWarning("Bottom Cover Type Spec requires BW and BT values.", "Legacy Type Spec");
                        return false;
                    }
                    break;
                case LegacySpecMode.TopBottomCover:
                    if (!HasValue(info.LegacySpec1) || !HasValue(info.LegacySpec2)
                        || !HasValue(info.LegacySpec3) || !HasValue(info.LegacySpec4))
                    {
                        TopMostMessageBox.ShowWarning("Top/Bottom Cover Type Spec requires WP, TH, BW, and BT values.", "Legacy Type Spec");
                        return false;
                    }
                    break;
                default:
                    break;
            }

            return true;
        }

        private static List<LegacyTypeSpecItem> GetAngleTypeSpecItems()
        {
            return new List<LegacyTypeSpecItem>
            {
                new LegacyTypeSpecItem("ST (Single Section)", 0, LegacySpecMode.None),
                new LegacyTypeSpecItem("RA (Reverse Y-Z)", 1, LegacySpecMode.None),
                new LegacyTypeSpecItem("LD (Long Leg B-B)", 3, LegacySpecMode.AngleSpacing),
                new LegacyTypeSpecItem("SD (Short Leg B-B)", 4, LegacySpecMode.AngleSpacing),
                new LegacyTypeSpecItem("SA (Star Angle)", 12, LegacySpecMode.None)
            };
        }

        private static List<LegacyTypeSpecItem> GetChannelTypeSpecItems()
        {
            return new List<LegacyTypeSpecItem>
            {
                new LegacyTypeSpecItem("ST (Single Section)", 0, LegacySpecMode.None),
                new LegacyTypeSpecItem("D (Double Channel)", 2, LegacySpecMode.ChannelSpacing),
                new LegacyTypeSpecItem("BA (Back to Back)", 10, LegacySpecMode.ChannelSpacing),
                new LegacyTypeSpecItem("FR (Front to Front)", 11, LegacySpecMode.ChannelSpacing)
            };
        }

        private static List<LegacyTypeSpecItem> GetWideFlangeTypeSpecItems()
        {
            return new List<LegacyTypeSpecItem>
            {
                new LegacyTypeSpecItem("ST (Single Section)", 0, LegacySpecMode.None),
                new LegacyTypeSpecItem("D (Double Profile)", 2, LegacySpecMode.DoubleProfile),
                new LegacyTypeSpecItem("T (Tee Cut)", 5, LegacySpecMode.None),
                new LegacyTypeSpecItem("CM (Composite)", 6, LegacySpecMode.Composite),
                new LegacyTypeSpecItem("TC (Top Cover)", 7, LegacySpecMode.TopCover),
                new LegacyTypeSpecItem("BC (Bottom Cover)", 8, LegacySpecMode.BottomCover),
                new LegacyTypeSpecItem("TB (Top+Bottom)", 9, LegacySpecMode.TopBottomCover)
            };
        }

        private static bool IsLegacyAngleShape(string shapeName)
        {
            if (string.IsNullOrEmpty(shapeName)) return false;
            return shapeName.IndexOf("angle", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsLegacyWideFlangeShape(string shapeName)
        {
            if (string.IsNullOrWhiteSpace(shapeName)) return false;

            string shape = shapeName.Trim();
            string lower = shape.ToLowerInvariant();

            if (lower.Contains("tube")
                || lower.Contains("pipe")
                || lower.Contains("hss")
                || lower.Contains("rhs")
                || lower.Contains("shs")
                || lower.Contains("chs")
                || lower.Contains("angle")
                || lower.Contains("channel")
                || lower.Contains("t shape")
                || lower.Contains("ct shape")
                || lower.Contains("bulb")
                || lower.Contains("flat")
                || lower.Contains("solid"))
            {
                return false;
            }

            var exact = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "W Shape", "M Shape", "S Shape", "HP Shape", "B Shape", "WW Shape",
                "UB Shape", "UC Shape", "WB Shape", "WC Shape", "JO Shape",
                "IPE Shape", "IPN Shape", "DIL Shape", "HD Shape", "HE Shape", "HL Shape",
                "H Shape", "H Shape Old", "I Shape"
            };

            if (exact.Contains(shape)) return true;

            if (!shape.EndsWith("Shape", StringComparison.OrdinalIgnoreCase))
                return false;

            string prefix = shape.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault() ?? "";

            var prefixes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "W", "M", "S", "HP", "B", "WW", "UB", "UC", "WB", "WC",
                "IPE", "IPN", "DIL", "HD", "HE", "HL", "H", "I", "JO"
            };

            return prefixes.Contains(prefix);
        }

        private static bool IsLegacyChannelShape(string shapeName)
        {
            if (string.IsNullOrEmpty(shapeName)) return false;
            return shapeName.IndexOf("channel", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsLegacyNoTypeSpecShape(string shapeName)
        {
            if (string.IsNullOrWhiteSpace(shapeName)) return false;

            string lower = shapeName.ToLowerInvariant();
            if (lower.Contains("hss")) return true;
            if (lower.Contains("rhs") || lower.Contains("shs") || lower.Contains("chs")) return true;
            if (lower.Contains("tube") || lower.Contains("pipe")) return true;

            return false;
        }

    }
}
