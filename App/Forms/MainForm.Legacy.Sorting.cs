using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private static List<string> SortShapesForZone(string zoneName, List<string> shapes)
        {
            if (shapes == null) return new List<string>();

            var order = GetShapeOrderForZone(zoneName);
            if (order.Count == 0) return shapes.OrderBy(s => s).ToList();

            var orderMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < order.Count; i++)
            {
                if (!orderMap.ContainsKey(order[i])) orderMap.Add(order[i], i);
            }

            return shapes
                .OrderBy(s => orderMap.TryGetValue(s, out int idx) ? idx : int.MaxValue)
                .ThenBy(s => s)
                .ToList();
        }

        private static List<string> GetShapeOrderForZone(string zoneName)
        {
            if (string.IsNullOrEmpty(zoneName)) return new List<string>();

            var orders = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    "American", new List<string>
                    {
                        "W Shape", "M Shape", "S Shape", "HP Shape", "B Shape",
                        "Channel", "MC Channel", "Angle", "Tube", "Pipe",
                        "HSS Rectangle", "HSS Round", "HSS Rectangle Old", "HSS Round Old",
                        "Tube Old", "Castellated Comp Beam", "Castellated NonComp Beam",
                        "Solid Rod", "Cable"
                    }
                },
                {
                    "Australian", new List<string>
                    {
                        "UB Shape", "UC Shape", "WB Shape", "WC Shape", "Channel", "Angle"
                    }
                },
                {
                    "British", new List<string>
                    {
                        "UB Shape", "UC Shape", "UP Shape", "JO Shape",
                        "Channel", "Angle", "Tube", "Pipe"
                    }
                },
                {
                    "Chinese", new List<string>
                    {
                        "I Shape", "H Shape", "T Shape", "Channel", "Angle", "Tube", "Pipe"
                    }
                },
                {
                    "European", new List<string>
                    {
                        "IPE Shape", "HD Shape", "HE Shape", "HL Shape", "HP Shape",
                        "DIL Shape", "IPN Shape",
                        "U Channel", "UAP Channel", "UPE Channel", "UPN Channel",
                        "Angle", "Tube", "Pipe", "RHS", "SHS", "CHS",
                        "Bulb Flat", "FlatBar", "SolidSquare"
                    }
                },
                {
                    "Japanese", new List<string>
                    {
                        "H Shape", "H Shape Old", "I Shape", "T Shape", "CT Shape",
                        "Channel", "Angle", "Tube", "Pipe", "RHS", "SHS", "CHS"
                    }
                }
            };

            return orders.TryGetValue(zoneName, out var list) ? list : new List<string>();
        }

        private static List<string> SortSeriesValues(IEnumerable<string> values)
        {
            if (values == null) return new List<string>();
            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(v => v, NaturalStringComparer.Instance)
                .ToList();
        }

        private static string GetSectionText(StaadSteelApp.LegacyDatabaseService.SectionItem item)
        {
            if (item == null) return "";
            if (!string.IsNullOrEmpty(item.StaadName)) return item.StaadName;
            return item.DisplayName ?? "";
        }

        private static List<StaadSteelApp.LegacyDatabaseService.SectionItem> SortSections(
            List<StaadSteelApp.LegacyDatabaseService.SectionItem> sections,
            string shapeName)
        {
            if (sections == null) return new List<StaadSteelApp.LegacyDatabaseService.SectionItem>();
            return sections.OrderBy(s => s, new SectionItemComparer(shapeName)).ToList();
        }

        private sealed class SectionItemComparer
            : IComparer<StaadSteelApp.LegacyDatabaseService.SectionItem>
        {
            private readonly string _shapeName;

            public SectionItemComparer(string shapeName)
            {
                _shapeName = shapeName ?? "";
            }

            public int Compare(
                StaadSteelApp.LegacyDatabaseService.SectionItem x,
                StaadSteelApp.LegacyDatabaseService.SectionItem y)
            {
                string a = GetSectionText(x);
                string b = GetSectionText(y);

                if (TryParseHollowSectionSortKey(a, _shapeName, out var keyA)
                    && TryParseHollowSectionSortKey(b, _shapeName, out var keyB))
                {
                    int seriesCmp = NaturalStringComparer.Instance.Compare(keyA.SeriesKey, keyB.SeriesKey);
                    if (seriesCmp != 0) return seriesCmp;

                    int thicknessCmp = keyA.Thickness.CompareTo(keyB.Thickness);
                    if (thicknessCmp != 0) return thicknessCmp;
                }

                return NaturalStringComparer.Instance.Compare(a, b);
            }
        }

        private sealed class NaturalStringComparer : IComparer<string>
        {
            public static readonly NaturalStringComparer Instance = new NaturalStringComparer();

            public int Compare(string a, string b)
            {
                a = a ?? "";
                b = b ?? "";

                var tokensA = TokenizeForSort(a);
                var tokensB = TokenizeForSort(b);
                int count = Math.Min(tokensA.Count, tokensB.Count);

                for (int i = 0; i < count; i++)
                {
                    var ta = tokensA[i];
                    var tb = tokensB[i];

                    if (ta.IsNumber && tb.IsNumber)
                    {
                        int numCmp = ta.Number.CompareTo(tb.Number);
                        if (numCmp != 0) return numCmp;
                        continue;
                    }

                    if (ta.IsNumber != tb.IsNumber)
                    {
                        return ta.IsNumber ? -1 : 1;
                    }

                    int textCmp = StringComparer.OrdinalIgnoreCase.Compare(ta.Text, tb.Text);
                    if (textCmp != 0) return textCmp;
                }

                if (tokensA.Count != tokensB.Count)
                {
                    return tokensA.Count.CompareTo(tokensB.Count);
                }

                return StringComparer.OrdinalIgnoreCase.Compare(a, b);
            }
        }

        private struct HollowSectionSortKey
        {
            public string SeriesKey;
            public double Thickness;
        }

        private struct SortToken
        {
            public bool IsNumber;
            public double Number;
            public string Text;
        }

        private static bool TryParseHollowSectionSortKey(
            string sectionName,
            string shapeName,
            out HollowSectionSortKey key)
        {
            key = default(HollowSectionSortKey);
            if (!TryParseHollowSectionSeriesAndThickness(sectionName, shapeName, out string seriesKey, out double thickness))
                return false;

            key.SeriesKey = seriesKey;
            key.Thickness = thickness;
            return true;
        }

        private static bool TryParseHollowSectionSeriesAndThickness(
            string sectionName,
            string shapeName,
            out string seriesKey,
            out double thickness)
        {
            seriesKey = "";
            thickness = 0;

            if (string.IsNullOrWhiteSpace(sectionName))
                return false;

            if (IsCompactTubeShape(shapeName))
            {
                return TryParseCompactTubeSection(sectionName.Trim(), out seriesKey, out thickness);
            }

            if (IsDelimitedHollowShape(shapeName))
            {
                return TryParseDelimitedHollowSection(sectionName.Trim(), out seriesKey, out thickness);
            }

            return false;
        }

        private static bool IsCompactTubeShape(string shapeName)
        {
            return string.Equals((shapeName ?? "").Trim(), "Tube", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsDelimitedHollowShape(string shapeName)
        {
            string normalized = (shapeName ?? "").Trim();
            return string.Equals(normalized, "Pipe", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "RHS", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "SHS", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "CHS", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "HSS Rectangle", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "HSS Round", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "HSS Rectangle Old", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "HSS Round Old", StringComparison.OrdinalIgnoreCase)
                || string.Equals(normalized, "Tube Old", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryParseCompactTubeSection(
            string sectionName,
            out string seriesKey,
            out double thickness)
        {
            seriesKey = "";
            thickness = 0;

            var match = Regex.Match(sectionName, @"^(?<prefix>[A-Za-z]+)(?<suffix>\d+(?:\.\d+)?)$");
            if (!match.Success) return false;

            string prefix = match.Groups["prefix"].Value.ToUpperInvariant();
            string suffix = match.Groups["suffix"].Value;
            int dotIndex = suffix.IndexOf('.');
            string integerPart = dotIndex >= 0 ? suffix.Substring(0, dotIndex) : suffix;
            string decimalPart = dotIndex >= 0 ? suffix.Substring(dotIndex) : "";

            for (int sizeDigits = integerPart.Length / 2; sizeDigits >= 2; sizeDigits--)
            {
                if (integerPart.Length <= sizeDigits * 2) continue;

                string d = integerPart.Substring(0, sizeDigits);
                string b = integerPart.Substring(sizeDigits, sizeDigits);
                if (!string.Equals(d, b, StringComparison.Ordinal)) continue;

                string thicknessRaw = integerPart.Substring(sizeDigits * 2) + decimalPart;
                if (!TryParseNumberToken(thicknessRaw, out thickness)) continue;

                seriesKey = prefix + d + b;
                return true;
            }

            return false;
        }

        private static bool TryParseDelimitedHollowSection(
            string sectionName,
            out string seriesKey,
            out double thickness)
        {
            seriesKey = "";
            thickness = 0;

            int lastX = Math.Max(sectionName.LastIndexOf('x'), sectionName.LastIndexOf('X'));
            if (lastX <= 0 || lastX >= sectionName.Length - 1) return false;

            string candidateSeries = sectionName.Substring(0, lastX).Trim();
            string candidateThickness = sectionName.Substring(lastX + 1).Trim();
            if (!TryParseNumberToken(candidateThickness, out thickness)) return false;

            seriesKey = candidateSeries.ToUpperInvariant();
            return true;
        }

        private static List<SortToken> TokenizeForSort(string text)
        {
            var tokens = new List<SortToken>();
            if (string.IsNullOrEmpty(text)) return tokens;

            int i = 0;
            while (i < text.Length)
            {
                if (IsNumberStart(text, i))
                {
                    int start = i;
                    while (i < text.Length && IsNumberChar(text[i])) i++;

                    string raw = text.Substring(start, i - start);
                    tokens.Add(new SortToken
                    {
                        IsNumber = true,
                        Number = ParseNumberToken(raw),
                        Text = raw
                    });
                }
                else
                {
                    int start = i;
                    while (i < text.Length && !IsNumberStart(text, i)) i++;

                    string raw = text.Substring(start, i - start).Trim();
                    if (raw.Length > 0)
                    {
                        tokens.Add(new SortToken
                        {
                            IsNumber = false,
                            Text = raw.ToUpperInvariant()
                        });
                    }
                }
            }

            return tokens;
        }

        private static bool IsNumberStart(string text, int index)
        {
            char c = text[index];
            if (char.IsDigit(c)) return true;
            return c == '.' && index + 1 < text.Length && char.IsDigit(text[index + 1]);
        }

        private static bool IsNumberChar(char c)
        {
            return char.IsDigit(c) || c == '.' || c == '/';
        }

        private static bool TryParseNumberToken(string raw, out double value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(raw)) return false;

            int slashIndex = raw.IndexOf('/');
            if (slashIndex > 0 && slashIndex < raw.Length - 1)
            {
                string numStr = raw.Substring(0, slashIndex);
                string denStr = raw.Substring(slashIndex + 1);

                if (double.TryParse(numStr, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out double num)
                    && double.TryParse(denStr, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double den)
                    && Math.Abs(den) > double.Epsilon)
                {
                    value = num / den;
                    return true;
                }
            }

            return double.TryParse(raw, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private static double ParseNumberToken(string raw)
        {
            return TryParseNumberToken(raw, out double value) ? value : 0;
        }
    }
}
