using System;
using System.Collections.Generic;
using System.Linq;

namespace StaadSteelApp.Core.Advance
{
    internal static partial class AdvanceConvertUptService
    {
        private static bool TryBuildCandidate(
            int typeCode,
            double[] props,
            out ConversionCandidate candidate,
            out CandidateBuildFailure failure,
            out string error)
        {
            candidate = null;
            failure = CandidateBuildFailure.None;
            error = string.Empty;
            props = props ?? Array.Empty<double>();

            if (IsLegacyPipe(typeCode))
            {
                double od = GetAt(props, 1) * 1000.0;
                double thickness = GetAt(props, 2) * 1000.0;
                double id = od - (2.0 * thickness);

                if (od <= 0.0 || thickness <= 0.0 || id <= 0.0)
                {
                    failure = CandidateBuildFailure.InvalidDimensions;
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

                candidate = ConversionCandidate.ForPipe("Legacy Pipe", displayName, od, thickness, id);
                return true;
            }

            if (IsLegacyTube(typeCode))
            {
                double depth = GetAt(props, 1) * 1000.0;
                double width = GetAt(props, 2) * 1000.0;
                double thickness = GetAt(props, 3) * 1000.0;

                if (depth <= 0.0 || width <= 0.0 || thickness <= 0.0 || (2.0 * thickness) >= Math.Min(depth, width))
                {
                    failure = CandidateBuildFailure.InvalidDimensions;
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

                candidate = ConversionCandidate.ForTube("Legacy Tube", displayName, depth, width, thickness);
                return true;
            }

            if (typeCode == TaperedTubeTypeCode)
            {
                double startSize = GetAt(props, 4) * 1000.0;
                double endSize = GetAt(props, 5) * 1000.0;
                double thickness = GetAt(props, 6) * 1000.0;
                int sectionType = (int)Math.Round(GetAt(props, 7));

                if (sectionType != TaperedTubeRoundReadType && sectionType != TaperedTubeSquareReadType)
                {
                    failure = CandidateBuildFailure.UnsupportedTaperedSubtype;
                    error = $"Unsupported tapered tube subtype {sectionType}; only Round (1) and Square (6) can be converted.";
                    return false;
                }

                if (startSize <= 0.0 || endSize <= 0.0 || thickness <= 0.0 ||
                    (2.0 * thickness) >= Math.Min(startSize, endSize))
                {
                    failure = CandidateBuildFailure.InvalidDimensions;
                    error = $"Invalid tapered dimensions D1={startSize:0.###}, D2={endSize:0.###}, TH={thickness:0.###}.";
                    return false;
                }

                if (Math.Abs(startSize - endSize) > DimensionToleranceMm)
                {
                    failure = CandidateBuildFailure.NonUniformTapered;
                    error = $"D1={startSize:0.###} mm and D2={endSize:0.###} mm differ; only uniform start/end sizes can be converted.";
                    return false;
                }

                double uniformSize = RoundDimension((startSize + endSize) / 2.0);
                if (uniformSize <= 0.0 || (2.0 * thickness) >= uniformSize)
                {
                    failure = CandidateBuildFailure.InvalidDimensions;
                    error = $"Uniform target size {uniformSize:0.###} mm is invalid for TH={thickness:0.###} mm.";
                    return false;
                }

                if (sectionType == TaperedTubeRoundReadType)
                {
                    double innerDiameter = uniformSize - (2.0 * thickness);
                    string displayName = SectionNamingService.GenerateSectionName(
                        eFramePropType.Pipe,
                        uniformSize,
                        0,
                        thickness,
                        0,
                        0,
                        0,
                        0);

                    candidate = ConversionCandidate.ForPipe(
                        "Tapered Pipe",
                        displayName,
                        uniformSize,
                        thickness,
                        innerDiameter);
                    return true;
                }

                string tubeDisplayName = SectionNamingService.GenerateSectionName(
                    eFramePropType.Rectangular,
                    uniformSize,
                    uniformSize,
                    thickness,
                    0,
                    thickness,
                    0,
                    0);

                candidate = ConversionCandidate.ForTube(
                    "Tapered Tube",
                    tubeDisplayName,
                    uniformSize,
                    uniformSize,
                    thickness);
                return true;
            }

            failure = CandidateBuildFailure.NotSupported;
            return false;
        }

        private static bool IsLegacyPipeOrTube(int typeCode)
        {
            return IsLegacyPipe(typeCode) || IsLegacyTube(typeCode);
        }

        private static bool IsConvertiblePipeOrTube(int typeCode)
        {
            return IsLegacyPipeOrTube(typeCode) || typeCode == TaperedTubeTypeCode;
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
            internal PreviewBucket(ConversionCandidate candidate, string materialName)
            {
                Candidate = candidate;
                MaterialName = materialName ?? string.Empty;
            }

            internal ConversionCandidate Candidate { get; }
            internal string MaterialName { get; }
            internal HashSet<int> PropertyRefIds { get; } = new HashSet<int>();
            internal List<int> MemberIds { get; } = new List<int>();
            internal HashSet<string> SourceDescriptions { get; } =
                new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            internal Dictionary<int, int> SourcePropertyRefByMember { get; } =
                new Dictionary<int, int>();

            internal AdvanceUptConversionGroup ToGroup()
            {
                return new AdvanceUptConversionGroup
                {
                    SourceDescription = string.Join(" / ", SourceDescriptions.OrderBy(value => value, StringComparer.OrdinalIgnoreCase)),
                    DisplayName = Candidate.DisplayName,
                    UptPropertyName = Candidate.DisplayName,
                    Shape = Candidate.Shape,
                    DepthMm = Candidate.DepthMm,
                    WidthMm = Candidate.WidthMm,
                    ThicknessMm = Candidate.ThicknessMm,
                    InnerDiameterMm = Candidate.InnerDiameterMm,
                    MaterialName = MaterialName,
                    MemberIds = NormalizeIds(MemberIds),
                    LegacyPropertyRefIds = PropertyRefIds.OrderBy(id => id).ToArray(),
                    SourceCandidateKey = Candidate.Key,
                    SourcePropertyRefByMember = new Dictionary<int, int>(SourcePropertyRefByMember)
                };
            }
        }

        private sealed class ConversionCandidate
        {
            internal string Key { get; private set; }
            internal string SortKey { get; private set; }
            internal string SourceDescription { get; private set; }
            internal string DisplayName { get; private set; }
            internal AdvanceUptConversionShape Shape { get; private set; }
            internal double DepthMm { get; private set; }
            internal double WidthMm { get; private set; }
            internal double ThicknessMm { get; private set; }
            internal double InnerDiameterMm { get; private set; }

            internal static ConversionCandidate ForPipe(
                string sourceDescription,
                string displayName,
                double od,
                double thickness,
                double id)
            {
                return new ConversionCandidate
                {
                    Key = $"PIPE|{RoundDimension(od):0.###}|{RoundDimension(thickness):0.###}",
                    SortKey = $"1|{RoundDimension(od):000000.###}|{RoundDimension(thickness):000000.###}",
                    SourceDescription = sourceDescription,
                    DisplayName = displayName,
                    Shape = AdvanceUptConversionShape.Pipe,
                    DepthMm = od,
                    ThicknessMm = thickness,
                    InnerDiameterMm = id
                };
            }

            internal static ConversionCandidate ForTube(
                string sourceDescription,
                string displayName,
                double depth,
                double width,
                double thickness)
            {
                return new ConversionCandidate
                {
                    Key = $"TUBE|{RoundDimension(depth):0.###}|{RoundDimension(width):0.###}|{RoundDimension(thickness):0.###}",
                    SortKey = $"0|{RoundDimension(depth):000000.###}|{RoundDimension(width):000000.###}|{RoundDimension(thickness):000000.###}",
                    SourceDescription = sourceDescription,
                    DisplayName = displayName,
                    Shape = AdvanceUptConversionShape.Tube,
                    DepthMm = depth,
                    WidthMm = width,
                    ThicknessMm = thickness
                };
            }
        }

        private enum CandidateBuildFailure
        {
            None,
            NotSupported,
            InvalidDimensions,
            NonUniformTapered,
            UnsupportedTaperedSubtype
        }
    }
}
