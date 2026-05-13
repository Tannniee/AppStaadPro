using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Check
{
    internal sealed class CheckSteelDesignResult
    {
        internal string DesignCode { get; set; } = string.Empty;
        internal string DesignStatus { get; set; } = string.Empty;
        internal double CriticalRatio { get; set; }
        internal double AllowableRatio { get; set; }
        internal int CriticalLoadCase { get; set; }
        internal double CriticalSection { get; set; }
        internal string CriticalClause { get; set; } = string.Empty;
        internal string DesignSection { get; set; } = string.Empty;
        internal double[] DesignForces { get; set; } = new double[3];
        internal double KLByR { get; set; }
    }

    internal sealed class CheckForceEnvelopeEntry
    {
        internal string Label { get; set; }
        internal double Value { get; set; }
        internal int LoadCase { get; set; }
        internal string Station { get; set; }
        internal string Unit { get; set; }
        internal bool IsSampled { get; set; }
    }

    internal static class CheckStaadDesignService
    {
        internal static bool TryGetMemberSteelDesignResult(
            object openStaad,
            int memberNo,
            out CheckSteelDesignResult result,
            out string error)
        {
            result = null;
            if (!OpenStaadDesignAdapter.TryGetMemberSteelDesignResults(
                    openStaad,
                    memberNo,
                    out OpenStaadSteelDesignResult adapterResult,
                    out error))
            {
                return false;
            }

            result = new CheckSteelDesignResult
            {
                DesignCode = adapterResult.DesignCode,
                DesignStatus = adapterResult.DesignStatus,
                CriticalRatio = adapterResult.CriticalRatio,
                AllowableRatio = adapterResult.AllowableRatio,
                CriticalLoadCase = adapterResult.CriticalLoadCase,
                CriticalSection = adapterResult.CriticalSection,
                CriticalClause = adapterResult.CriticalClause,
                DesignSection = adapterResult.DesignSection,
                DesignForces = adapterResult.DesignForces ?? new double[3],
                KLByR = adapterResult.KLByR
            };

            return true;
        }

        internal static bool TryGetCriticalSectionForces(
            object openStaad,
            int memberNo,
            CheckSteelDesignResult designResult,
            out MemberForceData forceData,
            out string error)
        {
            forceData = null;
            error = string.Empty;

            if (openStaad == null)
            {
                error = "OpenSTAAD object is not available.";
                return false;
            }

            if (designResult == null)
            {
                error = "STAAD design result is not available.";
                return false;
            }

            if (!OpenStaadOutputAdapter.TryGetIntermediateMemberForcesAtDistance(
                    openStaad,
                    memberNo,
                    designResult.CriticalSection,
                    designResult.CriticalLoadCase,
                    out double[] forces,
                    out error))
            {
                return false;
            }

            forceData = new MemberForceData
            {
                MemberNo = memberNo,
                LoadCase = designResult.CriticalLoadCase,
                Node = $"At {designResult.CriticalSection:F3}",
                Fx = GetAt(forces, 0),
                Fy = GetAt(forces, 1),
                Fz = GetAt(forces, 2),
                Mx = GetAt(forces, 3),
                My = GetAt(forces, 4),
                Mz = GetAt(forces, 5)
            };

            if (designResult.DesignForces != null && designResult.DesignForces.Length >= 3)
            {
                double signFx = Math.Sign(forceData.Fx);
                if (signFx == 0)
                    signFx = 1;

                forceData.Fx = Math.Abs(designResult.DesignForces[0]) * signFx;
            }

            NormalizeCriticalAxialForceSign(forceData, designResult);
            return true;
        }

        internal static List<CheckForceEnvelopeEntry> BuildMemberForceEnvelope(
            object openStaad,
            int memberNo,
            IReadOnlyList<MemberForceData> extractedEndForces,
            int stationCount = 21,
            bool sampleIntermediate = false)
        {
            List<CheckForceEnvelopeEntry> extracted = BuildEnvelopeFromExtractedEndForces(memberNo, extractedEndForces);
            if (extracted.Count > 0)
                return extracted;

            if (!sampleIntermediate)
                return extracted;

            return TrySampleMemberForceEnvelope(openStaad, memberNo, stationCount);
        }

        private static List<CheckForceEnvelopeEntry> TrySampleMemberForceEnvelope(object openStaad, int memberNo, int stationCount)
        {
            var entries = CreateEnvelopeEntrySet();
            if (openStaad == null || memberNo <= 0 || stationCount < 2)
                return entries;

            if (!CheckForceExtractionService.TryGetAllLoadCases(openStaad, out int[] loadCases, out _))
                return entries;

            if (loadCases.Length == 0)
                return entries;

            if (!OpenStaadGeometryAdapter.TryGetBeamLength(openStaad, memberNo, out double lengthM, out string lengthError))
            {
                Debug.WriteLine($"Member {memberNo}: failed to read member length for envelope sampling. {lengthError}");
                return entries;
            }

            foreach (int loadCase in loadCases)
            {
                for (int i = 0; i < stationCount; i++)
                {
                    double distanceM = lengthM * i / (stationCount - 1.0);
                    if (!OpenStaadOutputAdapter.TryGetIntermediateMemberForcesAtDistance(
                            openStaad,
                            memberNo,
                            distanceM,
                            loadCase,
                            out double[] forces,
                            out string forceError))
                    {
                        Debug.WriteLine($"GetIntermediateMemberForcesAtDistance failed for beam {memberNo}, LC {loadCase}, distance {distanceM}: {forceError}");
                        continue;
                    }

                    if (forces == null || forces.Length < 6)
                        continue;

                    string station = $"At {distanceM:F3}";
                    UpdateEnvelopeEntry(entries, "P", forces[0], loadCase, station, "kN", true);
                    UpdateEnvelopeEntry(entries, "V2", forces[1], loadCase, station, "kN", true);
                    UpdateEnvelopeEntry(entries, "V3", forces[2], loadCase, station, "kN", true);
                    UpdateEnvelopeEntry(entries, "T", forces[3], loadCase, station, "kN-m", true);
                    UpdateEnvelopeEntry(entries, "M2", forces[4], loadCase, station, "kN-m", true);
                    UpdateEnvelopeEntry(entries, "M3", forces[5], loadCase, station, "kN-m", true);
                }
            }

            return entries.Where(e => !string.IsNullOrWhiteSpace(e.Station)).ToList();
        }

        private static List<CheckForceEnvelopeEntry> BuildEnvelopeFromExtractedEndForces(int memberNo, IReadOnlyList<MemberForceData> extractedEndForces)
        {
            var entries = CreateEnvelopeEntrySet();
            var forces = extractedEndForces?.Where(f => f.MemberNo == memberNo).ToList() ?? new List<MemberForceData>();
            foreach (var force in forces)
            {
                UpdateEnvelopeEntry(entries, "P", force.Fx, force.LoadCase, force.Node, "kN", false);
                UpdateEnvelopeEntry(entries, "V2", force.Fy, force.LoadCase, force.Node, "kN", false);
                UpdateEnvelopeEntry(entries, "V3", force.Fz, force.LoadCase, force.Node, "kN", false);
                UpdateEnvelopeEntry(entries, "T", force.Mx, force.LoadCase, force.Node, "kN-m", false);
                UpdateEnvelopeEntry(entries, "M2", force.My, force.LoadCase, force.Node, "kN-m", false);
                UpdateEnvelopeEntry(entries, "M3", force.Mz, force.LoadCase, force.Node, "kN-m", false);
            }

            return entries.Where(e => !string.IsNullOrWhiteSpace(e.Station)).ToList();
        }

        private static List<CheckForceEnvelopeEntry> CreateEnvelopeEntrySet()
        {
            return new List<CheckForceEnvelopeEntry>
            {
                new CheckForceEnvelopeEntry { Label = "P", Unit = "kN" },
                new CheckForceEnvelopeEntry { Label = "V2", Unit = "kN" },
                new CheckForceEnvelopeEntry { Label = "V3", Unit = "kN" },
                new CheckForceEnvelopeEntry { Label = "T", Unit = "kN-m" },
                new CheckForceEnvelopeEntry { Label = "M2", Unit = "kN-m" },
                new CheckForceEnvelopeEntry { Label = "M3", Unit = "kN-m" }
            };
        }

        private static void UpdateEnvelopeEntry(
            List<CheckForceEnvelopeEntry> entries,
            string label,
            double value,
            int loadCase,
            string station,
            string unit,
            bool isSampled)
        {
            if (entries == null)
                return;

            CheckForceEnvelopeEntry entry = entries.FirstOrDefault(e => string.Equals(e.Label, label, StringComparison.OrdinalIgnoreCase));
            if (entry == null)
                return;

            if (Math.Abs(value) <= Math.Abs(entry.Value) && !string.IsNullOrWhiteSpace(entry.Station))
                return;

            entry.Value = value;
            entry.LoadCase = loadCase;
            entry.Station = station;
            entry.Unit = unit;
            entry.IsSampled = isSampled;
        }

        private static bool? InferCompressionFromSteelClause(CheckSteelDesignResult steelInfo)
        {
            if (steelInfo == null)
                return null;

            string clause = (steelInfo.CriticalClause ?? string.Empty).Trim().ToUpperInvariant();
            string section = (steelInfo.DesignSection ?? string.Empty).Trim().ToUpperInvariant();
            string status = (steelInfo.DesignStatus ?? string.Empty).Trim().ToUpperInvariant();

            bool suggestsCompression =
                clause.Contains("H1-1") ||
                HasChapterToken(clause, 'E') ||
                HasChapterToken(section, 'E') ||
                clause.Contains("BUCKL") ||
                section.Contains("COMP") ||
                section.StartsWith("E", StringComparison.Ordinal) ||
                status.Contains("COMP");

            if (suggestsCompression)
                return true;

            bool suggestsTension =
                clause.Contains("H1-2") ||
                HasChapterToken(clause, 'D') ||
                HasChapterToken(section, 'D') ||
                section.Contains("TENSION") ||
                section.StartsWith("D", StringComparison.Ordinal) ||
                status.Contains("TENSION");

            if (suggestsTension)
                return false;

            return null;
        }

        private static bool HasChapterToken(string text, char chapter)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            string value = text.ToUpperInvariant();
            char target = char.ToUpperInvariant(chapter);

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] != target)
                    continue;

                bool leftOk = i == 0 || !char.IsLetterOrDigit(value[i - 1]);
                if (!leftOk)
                    continue;

                if (i + 1 >= value.Length)
                    continue;

                char next = value[i + 1];
                if (char.IsDigit(next) || next == '.' || next == '-' || next == '/' || next == '_')
                    return true;
            }

            string compact = new string(value.Where(char.IsLetterOrDigit).ToArray());
            for (int i = 0; i < compact.Length - 1; i++)
            {
                if (compact[i] == target && char.IsDigit(compact[i + 1]))
                    return true;
            }

            return false;
        }

        private static bool IsLikelyCompressionByStaadResult(CheckSteelDesignResult steelInfo, MemberForceData forceData)
        {
            if (steelInfo == null || forceData == null)
                return false;

            double fx = Math.Abs(forceData.Fx);
            if (fx <= 1e-9 || steelInfo.CriticalRatio <= 1e-9)
                return false;

            double impliedCapacity = fx / steelInfo.CriticalRatio;
            return impliedCapacity < 2500.0;
        }

        private static void NormalizeCriticalAxialForceSign(MemberForceData forceData, CheckSteelDesignResult steelInfo)
        {
            if (forceData == null || steelInfo == null)
                return;

            bool? expectCompression = InferCompressionFromSteelClause(steelInfo);
            if (!expectCompression.HasValue && IsLikelyCompressionByStaadResult(steelInfo, forceData))
                expectCompression = true;

            if (!expectCompression.HasValue)
                return;

            double current = forceData.Fx;
            if (Math.Abs(current) <= 1e-9 && steelInfo.DesignForces != null && steelInfo.DesignForces.Length > 0)
                current = Math.Abs(steelInfo.DesignForces[0]);

            if (Math.Abs(current) <= 1e-9)
                return;

            if (expectCompression.Value && current > 0)
                forceData.Fx = -current;
            else if (!expectCompression.Value && current < 0)
                forceData.Fx = -current;
        }

        private static double GetAt(double[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : 0.0;
        }
    }
}
