using System;
using System.Collections.Generic;
using System.Linq;

namespace StaadSteelApp.Core.Advance
{
    internal static partial class AdvanceConvertUptService
    {
        private static bool TryBuildCandidate(int typeCode, double[] props, out ConversionCandidate candidate, out string error)
        {
            candidate = null;
            error = string.Empty;
            props = props ?? Array.Empty<double>();

            if (IsLegacyPipe(typeCode))
            {
                double od = GetAt(props, 1) * 1000.0;
                double thickness = GetAt(props, 2) * 1000.0;
                double id = od - (2.0 * thickness);

                if (od <= 0.0 || thickness <= 0.0 || id <= 0.0)
                {
                    error = $"Invalid legacy pipe dimensions OD={od:0.###}, t={thickness:0.###}.";
                    return false;
                }

                string displayName = SectionNamingService.GenerateSectionName(
                    eFramePropType.Pipe,
                    od,
                    0,
                    thickness,
                    0,
                    0,
                    0,
                    0);

                candidate = ConversionCandidate.ForPipe(displayName, od, thickness, id);
                return true;
            }

            if (IsLegacyTube(typeCode))
            {
                double depth = GetAt(props, 1) * 1000.0;
                double width = GetAt(props, 2) * 1000.0;
                double thickness = GetAt(props, 3) * 1000.0;

                if (depth <= 0.0 || width <= 0.0 || thickness <= 0.0 || (2.0 * thickness) >= Math.Min(depth, width))
                {
                    error = $"Invalid legacy tube dimensions D={depth:0.###}, W={width:0.###}, t={thickness:0.###}.";
                    return false;
                }

                string displayName = SectionNamingService.GenerateSectionName(
                    eFramePropType.Rectangular,
                    depth,
                    width,
                    thickness,
                    0,
                    thickness,
                    0,
                    0);

                candidate = ConversionCandidate.ForTube(displayName, depth, width, thickness);
                return true;
            }

            return false;
        }

        private static bool IsLegacyPipeOrTube(int typeCode)
        {
            return IsLegacyPipe(typeCode) || IsLegacyTube(typeCode);
        }

        private static bool IsLegacyPipe(int typeCode)
        {
            return typeCode == 655 || typeCode == 660 || typeCode == 661;
        }

        private static bool IsLegacyTube(int typeCode)
        {
            return typeCode == 650 || typeCode == 654;
        }

        private static bool IsExistingUptPipeOrTube(int typeCode)
        {
            return typeCode == 695 || typeCode == 696;
        }

        private static int[] NormalizeIds(IEnumerable<int> ids)
        {
            return ids == null
                ? Array.Empty<int>()
                : ids.Where(id => id > 0).Distinct().OrderBy(id => id).ToArray();
        }

        private static double GetAt(double[] values, int index)
        {
            if (values == null || index < 0 || index >= values.Length)
                return 0.0;

            return values[index];
        }

        private static double RoundDimension(double value)
        {
            return Math.Round(value / DimensionToleranceMm) * DimensionToleranceMm;
        }

        private sealed class PreviewBucket
        {
            internal PreviewBucket(ConversionCandidate candidate)
            {
                Candidate = candidate;
            }

            internal ConversionCandidate Candidate { get; }
            internal HashSet<int> PropertyRefIds { get; } = new HashSet<int>();
            internal List<int> MemberIds { get; } = new List<int>();

            internal AdvanceUptConversionGroup ToGroup()
            {
                return new AdvanceUptConversionGroup
                {
                    DisplayName = Candidate.DisplayName,
                    UptPropertyName = Candidate.DisplayName,
                    Shape = Candidate.Shape,
                    DepthMm = Candidate.DepthMm,
                    WidthMm = Candidate.WidthMm,
                    ThicknessMm = Candidate.ThicknessMm,
                    InnerDiameterMm = Candidate.InnerDiameterMm,
                    MemberIds = NormalizeIds(MemberIds),
                    LegacyPropertyRefIds = PropertyRefIds.OrderBy(id => id).ToArray()
                };
            }
        }

        private sealed class ConversionCandidate
        {
            internal string Key { get; private set; }
            internal string SortKey { get; private set; }
            internal string DisplayName { get; private set; }
            internal AdvanceUptConversionShape Shape { get; private set; }
            internal double DepthMm { get; private set; }
            internal double WidthMm { get; private set; }
            internal double ThicknessMm { get; private set; }
            internal double InnerDiameterMm { get; private set; }

            internal static ConversionCandidate ForPipe(string displayName, double od, double thickness, double id)
            {
                return new ConversionCandidate
                {
                    Key = $"PIPE|{RoundDimension(od):0.###}|{RoundDimension(thickness):0.###}",
                    SortKey = $"1|{RoundDimension(od):000000.###}|{RoundDimension(thickness):000000.###}",
                    DisplayName = displayName,
                    Shape = AdvanceUptConversionShape.Pipe,
                    DepthMm = od,
                    ThicknessMm = thickness,
                    InnerDiameterMm = id
                };
            }

            internal static ConversionCandidate ForTube(string displayName, double depth, double width, double thickness)
            {
                return new ConversionCandidate
                {
                    Key = $"TUBE|{RoundDimension(depth):0.###}|{RoundDimension(width):0.###}|{RoundDimension(thickness):0.###}",
                    SortKey = $"0|{RoundDimension(depth):000000.###}|{RoundDimension(width):000000.###}|{RoundDimension(thickness):000000.###}",
                    DisplayName = displayName,
                    Shape = AdvanceUptConversionShape.Tube,
                    DepthMm = depth,
                    WidthMm = width,
                    ThicknessMm = thickness
                };
            }
        }
    }
}
