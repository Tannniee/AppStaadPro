using System;
using System.Collections.Generic;
using System.Linq;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    internal static class RatioHighlightService
    {
        internal static ServiceOperationResult HighlightByCriticalRatio(object openStaad, double threshold)
        {
            if (openStaad == null)
                return ServiceOperationResult.Error("Ratio", "Not connected to STAAD.");

            if (threshold <= 0.0)
                return ServiceOperationResult.Warning("Ratio", "Please enter a ratio threshold greater than zero.");

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out string geometryError))
                return ServiceOperationResult.Error("Ratio", geometryError);

            if (!OpenStaadGeometryAdapter.TryGetBeamList(geometry, out int[] beamIds, out string beamError))
                return ServiceOperationResult.Error("Ratio", "Could not read member list: " + beamError);

            if (beamIds.Length == 0)
                return ServiceOperationResult.Warning("Ratio", "No members found in the active STAAD model.");

            var highlighted = new List<int>();
            int readableCount = 0;
            int failedCount = 0;
            double maxRatio = 0.0;

            foreach (int beamId in beamIds)
            {
                if (!OpenStaadDesignAdapter.TryGetMemberSteelDesignResults(
                        openStaad,
                        beamId,
                        out OpenStaadSteelDesignResult designResult,
                        out _))
                {
                    failedCount++;
                    continue;
                }

                readableCount++;
                double ratio = designResult.CriticalRatio;
                if (ratio > maxRatio)
                    maxRatio = ratio;

                if (ratio >= threshold)
                    highlighted.Add(beamId);
            }

            if (readableCount == 0)
            {
                return ServiceOperationResult.Warning(
                    "Ratio",
                    "No steel design ratios were available. Run STAAD steel design/check before using Ratio.");
            }

            if (highlighted.Count == 0)
            {
                return ServiceOperationResult.Info(
                    "Ratio",
                    $"No members with ratio >= {threshold:0.###}. Max ratio: {maxRatio:0.###}.");
            }

            int[] highlightIds = highlighted.Distinct().OrderBy(x => x).ToArray();
            if (!OpenStaadGeometryAdapter.TryHighlightBeams(geometry, highlightIds, out string highlightError))
                return ServiceOperationResult.Error("Ratio", "Could not highlight ratio members: " + highlightError);

            string skippedText = failedCount > 0 ? $" Skipped {failedCount} member(s) without design result." : string.Empty;
            return ServiceOperationResult.Success(
                "Ratio",
                $"Highlighted {highlightIds.Length} member(s) with ratio >= {threshold:0.###}. Max ratio: {maxRatio:0.###}.{skippedText}");
        }
    }
}
