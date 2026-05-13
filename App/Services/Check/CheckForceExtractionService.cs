using System;
using System.Collections.Generic;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Check
{
    internal sealed class CheckForceExtractionResult
    {
        internal CheckForceExtractionResult(
            IReadOnlyList<MemberForceData> forces,
            int[] loadCases,
            int errorCount,
            string errorSummary)
        {
            Forces = forces ?? Array.Empty<MemberForceData>();
            LoadCases = loadCases ?? Array.Empty<int>();
            ErrorCount = errorCount;
            ErrorSummary = errorSummary ?? string.Empty;
        }

        internal IReadOnlyList<MemberForceData> Forces { get; }
        internal int[] LoadCases { get; }
        internal int ErrorCount { get; }
        internal string ErrorSummary { get; }
        internal bool HasForces => Forces.Count > 0;
    }

    internal static class CheckForceExtractionService
    {
        internal static bool TryGetSelectedBeamIds(object openStaad, out int[] selectedBeamIds, out string error)
        {
            return TryGetSelectedBeamIds(openStaad, new[] { 1, 0 }, out selectedBeamIds, out error);
        }

        internal static bool TryGetSelectedBeamIds(object openStaad, int[] sortFlags, out int[] selectedBeamIds, out string error)
        {
            return OpenStaadSelectionAdapter.TryGetSelectedBeamIds(
                openStaad,
                false,
                false,
                sortFlags ?? new[] { 1, 0 },
                out selectedBeamIds,
                out error);
        }

        internal static bool TryGetAllLoadCases(object openStaad, out int[] loadCases, out string error)
        {
            return OpenStaadLoadAdapter.TryGetAllLoadCaseNumbers(openStaad, out loadCases, out error);
        }

        internal static CheckForceExtractionResult ExtractEndForces(object openStaad, IEnumerable<int> beamIds)
        {
            if (openStaad == null)
                return new CheckForceExtractionResult(Array.Empty<MemberForceData>(), Array.Empty<int>(), 0, "OpenSTAAD object is not available.");

            int[] beams = NormalizeIds(beamIds);
            if (beams.Length == 0)
                return new CheckForceExtractionResult(Array.Empty<MemberForceData>(), Array.Empty<int>(), 0, "No beams were supplied.");

            if (!TryGetAllLoadCases(openStaad, out int[] loadCases, out string loadError))
                return new CheckForceExtractionResult(Array.Empty<MemberForceData>(), Array.Empty<int>(), 0, loadError);

            if (loadCases.Length == 0)
                return new CheckForceExtractionResult(Array.Empty<MemberForceData>(), loadCases, 0, "No load cases found.");

            if (!OpenStaadOutputAdapter.TryGetOutputObject(openStaad, out object outputObj, out string outputError))
                return new CheckForceExtractionResult(Array.Empty<MemberForceData>(), loadCases, 0, outputError);

            var results = new List<MemberForceData>(beams.Length * loadCases.Length * 2);
            int errors = 0;

            foreach (int beamNo in beams)
            {
                foreach (int loadCase in loadCases)
                {
                    if (OpenStaadOutputAdapter.TryGetMemberEndForcesFromOutput(
                            outputObj,
                            beamNo,
                            0,
                            loadCase,
                            true,
                            out double[] startForces,
                            out string startError))
                    {
                        results.Add(CreateForceData(beamNo, loadCase, "Start", startForces));
                    }
                    else
                    {
                        errors++;
                        LogForceReadError("start", beamNo, loadCase, startError, errors);
                    }

                    if (OpenStaadOutputAdapter.TryGetMemberEndForcesFromOutput(
                            outputObj,
                            beamNo,
                            1,
                            loadCase,
                            true,
                            out double[] endForces,
                            out string endError))
                    {
                        results.Add(CreateForceData(beamNo, loadCase, "End", endForces));
                    }
                    else
                    {
                        errors++;
                        LogForceReadError("end", beamNo, loadCase, endError, errors);
                    }
                }
            }

            string summary = string.Empty;
            if (!string.IsNullOrWhiteSpace(loadError))
                summary = loadError;
            if (errors > 0)
            {
                if (!string.IsNullOrWhiteSpace(summary))
                    summary += " ";
                summary += $"Force extraction completed with {errors} error(s).";
            }

            return new CheckForceExtractionResult(results, loadCases, errors, summary);
        }

        private static MemberForceData CreateForceData(int memberNo, int loadCase, string station, double[] forces)
        {
            return new MemberForceData
            {
                MemberNo = memberNo,
                LoadCase = loadCase,
                Node = station,
                Fx = GetAt(forces, 0),
                Fy = GetAt(forces, 1),
                Fz = GetAt(forces, 2),
                Mx = GetAt(forces, 3),
                My = GetAt(forces, 4),
                Mz = GetAt(forces, 5)
            };
        }

        private static double GetAt(double[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : 0.0;
        }

        private static int[] NormalizeIds(IEnumerable<int> ids)
        {
            if (ids == null)
                return Array.Empty<int>();

            var result = new List<int>();
            var seen = new HashSet<int>();
            foreach (int id in ids)
            {
                if (id <= 0 || !seen.Add(id))
                    continue;

                result.Add(id);
            }

            return result.ToArray();
        }

        private static void LogForceReadError(string endName, int beamNo, int loadCase, string error, int errorCount)
        {
            if (errorCount > 3)
                return;

            Debug.WriteLine($"GetMemberEndForces({endName}) failed for beam {beamNo}, LC {loadCase}: {error}");
        }
    }
}
