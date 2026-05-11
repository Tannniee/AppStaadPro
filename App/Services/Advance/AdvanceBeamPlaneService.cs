using System;
using System.Collections.Generic;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceBeamPlaneService
    {
        internal static bool TryAlignNodesToBeamPlanes(
            int[] targetNodeIds,
            int directionBeamId,
            bool alignToXY,
            bool alignToXZ,
            bool alignToYZ,
            out int updatedCount,
            out string message)
        {
            updatedCount = 0;
            message = string.Empty;

            if (targetNodeIds == null || targetNodeIds.Length == 0)
            {
                message = "No selected node found for beam-plane alignment.";
                return false;
            }

            if (directionBeamId <= 0)
            {
                message = "Direction beam must be a valid positive integer.";
                return false;
            }

            if (!alignToXY && !alignToXZ && !alignToYZ)
            {
                message = "Please choose at least one plane (XY/XZ/YZ).";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetGeometry(out _, out object geometry, out message))
                return false;

            if (!AdvanceSelectionService.TryGetBeamIncidence(geometry, directionBeamId, out int nodeA, out int nodeB))
            {
                message = $"Could not read incidence of direction beam {directionBeamId}.";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, nodeA, out AdvancePoint3D beamStart, out string startError))
            {
                message = $"Could not read start node {nodeA} of beam {directionBeamId}: {startError}";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, nodeB, out AdvancePoint3D beamEnd, out string endError))
            {
                message = $"Could not read end node {nodeB} of beam {directionBeamId}: {endError}";
                return false;
            }

            if (!ValidateSelectedPlanes(beamStart, beamEnd, alignToXY, alignToXZ, alignToYZ, out string planeError))
            {
                message = planeError;
                return false;
            }

            int[] validTargets = AdvanceSelectionService.FilterValidNodeIds(geometry, targetNodeIds, out int invalidCount);
            if (validTargets.Length == 0)
            {
                message = invalidCount > 0
                    ? $"No valid selected node found (ignored {invalidCount} invalid/deleted node IDs)."
                    : "No valid selected node found.";
                return false;
            }

            var failed = new List<string>();
            foreach (int nodeId in validTargets)
            {
                if (!AdvanceSelectionService.TryGetNodePoint(geometry, nodeId, out AdvancePoint3D current, out string readError))
                {
                    failed.Add($"Node {nodeId}: read failed ({readError})");
                    continue;
                }

                AdvancePoint3D next = new AdvancePoint3D(
                    alignToYZ ? beamStart.X : current.X,
                    alignToXZ ? beamStart.Y : current.Y,
                    alignToXY ? beamStart.Z : current.Z);

                if (!AdvanceWriteService.TrySetNodePoint(geometry, nodeId, next, out string writeError))
                {
                    failed.Add($"Node {nodeId}: set failed ({writeError})");
                    continue;
                }

                updatedCount++;
            }

            if (updatedCount == 0)
            {
                string detail = failed.Count > 0 ? failed[0] : "No node was updated.";
                message = "Beam-plane alignment failed. " + detail;
                return false;
            }

            message =
                $"Aligned {updatedCount}/{validTargets.Length} node(s) to beam {directionBeamId} plane(s): {FormatPlanes(alignToXY, alignToXZ, alignToYZ)}.";

            if (invalidCount > 0)
                message += $"\nIgnored {invalidCount} invalid/deleted selected node ID(s).";

            if (failed.Count > 0)
                message += "\nSome nodes failed. First: " + failed[0];

            return true;
        }

        private static bool ValidateSelectedPlanes(
            AdvancePoint3D beamStart,
            AdvancePoint3D beamEnd,
            bool alignToXY,
            bool alignToXZ,
            bool alignToYZ,
            out string error)
        {
            var invalidPlanes = new List<string>();

            if (alignToXY && Math.Abs(beamStart.Z - beamEnd.Z) > AdvanceConstants.CoordEpsilon)
                invalidPlanes.Add("XY requires the beam to have constant Z");

            if (alignToXZ && Math.Abs(beamStart.Y - beamEnd.Y) > AdvanceConstants.CoordEpsilon)
                invalidPlanes.Add("XZ requires the beam to have constant Y");

            if (alignToYZ && Math.Abs(beamStart.X - beamEnd.X) > AdvanceConstants.CoordEpsilon)
                invalidPlanes.Add("YZ requires the beam to have constant X");

            if (invalidPlanes.Count == 0)
            {
                error = string.Empty;
                return true;
            }

            string validPlanes = FormatCompatiblePlanes(beamStart, beamEnd);
            string dx = AdvanceGeometryService.FormatCoordinate(beamEnd.X - beamStart.X);
            string dy = AdvanceGeometryService.FormatCoordinate(beamEnd.Y - beamStart.Y);
            string dz = AdvanceGeometryService.FormatCoordinate(beamEnd.Z - beamStart.Z);
            error =
                "Selected beam cannot define the requested plane(s): " + string.Join("; ", invalidPlanes) + "." +
                $"\nBeam delta: dX={dx}, dY={dy}, dZ={dz}." +
                $"\nCompatible plane(s): {validPlanes}.";
            return false;
        }

        private static string FormatPlanes(bool alignToXY, bool alignToXZ, bool alignToYZ)
        {
            var planes = new List<string>();
            if (alignToXY)
                planes.Add("XY");
            if (alignToXZ)
                planes.Add("XZ");
            if (alignToYZ)
                planes.Add("YZ");

            return string.Join(", ", planes);
        }

        private static string FormatCompatiblePlanes(AdvancePoint3D beamStart, AdvancePoint3D beamEnd)
        {
            var planes = new List<string>();

            if (Math.Abs(beamStart.Z - beamEnd.Z) <= AdvanceConstants.CoordEpsilon)
                planes.Add("XY");

            if (Math.Abs(beamStart.Y - beamEnd.Y) <= AdvanceConstants.CoordEpsilon)
                planes.Add("XZ");

            if (Math.Abs(beamStart.X - beamEnd.X) <= AdvanceConstants.CoordEpsilon)
                planes.Add("YZ");

            return planes.Count > 0 ? string.Join(", ", planes) : "None";
        }
    }
}
