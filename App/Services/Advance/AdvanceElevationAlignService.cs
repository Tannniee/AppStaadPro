using System;
using System.Collections.Generic;
using System.Linq;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceElevationAlignService
    {
        internal static bool TryBuildPreview(
            int[] targetNodeIds,
            int[] vectorNodeIds,
            out AdvanceElevationAlignPreviewRow[] previewRows,
            out string message)
        {
            previewRows = Array.Empty<AdvanceElevationAlignPreviewRow>();
            message = string.Empty;

            if (targetNodeIds == null || targetNodeIds.Length == 0)
            {
                message = "Capture at least one target node first.";
                return false;
            }

            if (vectorNodeIds == null || vectorNodeIds.Length != 2 || vectorNodeIds[0] == vectorNodeIds[1])
            {
                message = "Capture exactly two different vector nodes.";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetGeometry(out object openStaad, out object geometry, out message))
                return false;

            double coordinateStep = GetCoordinateStorageStep(openStaad);
            double coordinateTolerance = GetCoordinateTolerance(openStaad);

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, vectorNodeIds[0], out AdvancePoint3D vectorA, out string readAError))
            {
                message = $"Could not read vector node {vectorNodeIds[0]}: {readAError}";
                return false;
            }

            if (!AdvanceSelectionService.TryGetNodePoint(geometry, vectorNodeIds[1], out AdvancePoint3D vectorB, out string readBError))
            {
                message = $"Could not read vector node {vectorNodeIds[1]}: {readBError}";
                return false;
            }

            int[] validTargets = AdvanceSelectionService.FilterValidNodeIds(geometry, targetNodeIds, out int invalidCount);
            if (validTargets.Length == 0)
            {
                message = invalidCount > 0
                    ? $"No valid target node found (ignored {invalidCount} invalid/deleted node IDs)."
                    : "No valid target node found.";
                return false;
            }

            var rows = new List<AdvanceElevationAlignPreviewRow>(validTargets.Length);
            foreach (int nodeId in validTargets)
            {
                if (!AdvanceSelectionService.TryGetNodePoint(geometry, nodeId, out AdvancePoint3D current, out string readError))
                {
                    message = $"Could not read target node {nodeId}: {readError}";
                    return false;
                }

                if (!AdvanceElevationAlignCalculator.TryCalculateYOnly(
                    vectorA,
                    vectorB,
                    current,
                    out double alignedY,
                    out bool canApply,
                    out AdvanceElevationAlignDisposition disposition,
                    out string status,
                    out string computeError))
                {
                    message = computeError;
                    return false;
                }

                if (canApply)
                {
                    double staadY = QuantizeCoordinate(alignedY, coordinateStep);
                    if (Math.Abs(staadY - current.Y) <= coordinateTolerance)
                    {
                        // Keep the exact read-back value so a second preview is stable.
                        alignedY = current.Y;
                        disposition = AdvanceElevationAlignDisposition.WithinCoordinatePrecision;
                        status = "Already aligned within STAAD coordinate precision";
                    }
                    else
                    {
                        alignedY = staadY;
                        disposition = AdvanceElevationAlignDisposition.Ready;
                        status = "Ready (STAAD-storable Y)";
                    }
                }

                rows.Add(new AdvanceElevationAlignPreviewRow
                {
                    NodeId = nodeId,
                    X = current.X,
                    OldY = current.Y,
                    NewY = alignedY,
                    Z = current.Z,
                    CanApply = canApply,
                    Disposition = disposition,
                    Status = status
                });
            }

            previewRows = rows.ToArray();
            int readyCount = rows.Count(row => row.RequiresUpdate);
            int unchangedCount = rows.Count(row => row.Disposition == AdvanceElevationAlignDisposition.Unchanged);
            int precisionCount = rows.Count(row => row.Disposition == AdvanceElevationAlignDisposition.WithinCoordinatePrecision);
            int verticalCollinearCount = rows.Count(row => row.Disposition == AdvanceElevationAlignDisposition.VerticalAlreadyCollinear);
            int skippedCount = rows.Count(row => row.Disposition == AdvanceElevationAlignDisposition.SkippedVerticalOutside);

            message =
                $"Preview: {readyCount} ready, {unchangedCount} unchanged, {precisionCount} within STAAD precision, " +
                $"{verticalCollinearCount} already on vertical vector, {skippedCount} vertical-vector skipped.";
            if (invalidCount > 0)
                message += $" Ignored {invalidCount} invalid/deleted node ID(s).";

            return true;
        }

        internal static bool TryApplyPreview(
            int[] targetNodeIds,
            int[] vectorNodeIds,
            AdvanceElevationAlignPreviewRow[] displayedRows,
            out int updatedCount,
            out int unchangedCount,
            out int skippedCount,
            out int precisionSkippedCount,
            out string message)
        {
            updatedCount = 0;
            unchangedCount = 0;
            skippedCount = 0;
            precisionSkippedCount = 0;
            message = string.Empty;

            if (displayedRows == null || displayedRows.Length == 0)
            {
                message = "No preview data is available to apply.";
                return false;
            }

            if (!TryBuildPreview(targetNodeIds, vectorNodeIds, out AdvanceElevationAlignPreviewRow[] currentRows, out string previewMessage))
            {
                message = previewMessage;
                return false;
            }

            if (!PreviewMatches(displayedRows, currentRows))
            {
                message = "Node or vector coordinates changed after preview. Close this preview and create a new one before applying.";
                return false;
            }

            if (!currentRows.Any(row => row.RequiresUpdate))
            {
                message = "No node requires a Y update.";
                return false;
            }

            if (!AdvanceOpenStaadService.TryGetGeometry(out object openStaad, out object geometry, out message))
                return false;

            double coordinateTolerance = GetCoordinateTolerance(openStaad);

            var attemptedRows = new List<AdvanceElevationAlignPreviewRow>();
            foreach (AdvanceElevationAlignPreviewRow row in currentRows)
            {
                if (!row.CanApply)
                {
                    if (row.Disposition == AdvanceElevationAlignDisposition.VerticalAlreadyCollinear)
                        unchangedCount++;
                    else
                        skippedCount++;
                    continue;
                }

                if (!row.RequiresUpdate)
                {
                    if (row.Disposition == AdvanceElevationAlignDisposition.WithinCoordinatePrecision)
                        precisionSkippedCount++;
                    else
                        unchangedCount++;
                    continue;
                }

                var next = new AdvancePoint3D(row.X, row.NewY, row.Z);
                if (!AdvanceWriteService.TrySetNodePoint(geometry, row.NodeId, next, out string writeError))
                {
                    string rollbackMessage = TryRollback(openStaad, geometry, attemptedRows, coordinateTolerance);
                    message = $"Node {row.NodeId} could not be updated: {writeError}. {rollbackMessage}";
                    updatedCount = 0;
                    return false;
                }

                attemptedRows.Add(row);
            }

            TryRefreshView(openStaad, out string refreshError);
            List<AdvanceElevationAlignPreviewRow> unverifiedRows = FindRowsNotAtTarget(
                geometry,
                attemptedRows,
                coordinateTolerance,
                out string verifyError);
            if (unverifiedRows.Count > 0)
            {
                AppLogger.Warning(
                    $"SetNodeCoordinate reflection call was not reflected in read-back for {unverifiedRows.Count}/{attemptedRows.Count} node(s). " +
                    "Retrying with dynamic COM binding. First node: " + unverifiedRows[0].NodeId + ".");

                foreach (AdvanceElevationAlignPreviewRow row in unverifiedRows)
                {
                    if (!TrySetNodePointDynamic(geometry, row, out string dynamicError))
                    {
                        string rollbackMessage = TryRollback(openStaad, geometry, attemptedRows, coordinateTolerance);
                        message =
                            $"Node {row.NodeId} did not accept the dynamic SetNodeCoordinate fallback: {dynamicError}. " +
                            rollbackMessage;
                        return false;
                    }
                }

                TryRefreshView(openStaad, out refreshError);
                unverifiedRows = FindRowsNotAtTarget(
                    geometry,
                    attemptedRows,
                    coordinateTolerance,
                    out verifyError);
            }

            if (unverifiedRows.Count > 0)
            {
                AdvanceElevationAlignPreviewRow first = unverifiedRows[0];
                string rollbackMessage = TryRollback(openStaad, geometry, attemptedRows, coordinateTolerance);
                message =
                    $"OpenSTAAD accepted SetNodeCoordinate but node {first.NodeId} still reads back at its old coordinates. " +
                    "The Y-only alignment was not confirmed. " +
                    (string.IsNullOrWhiteSpace(verifyError) ? string.Empty : "Read-back details: " + verifyError + ". ") +
                    rollbackMessage;
                return false;
            }

            updatedCount = CountRowsActuallyMoved(geometry, attemptedRows, out int readBackUnchangedCount);
            precisionSkippedCount += readBackUnchangedCount;
            if (updatedCount == 0)
            {
                message =
                    $"OpenSTAAD did not move any of the {attemptedRows.Count} requested node(s). " +
                    "Their requested Y changes are below STAAD's internal coordinate step.";
                return false;
            }

            AdvanceWriteService.HighlightNodes(geometry, attemptedRows.Select(row => row.NodeId).ToArray());
            TryRefreshView(openStaad, out _);
            message =
                $"Y-only alignment completed: {updatedCount} updated, " +
                $"{unchangedCount} unchanged, {skippedCount} vertical-vector skipped, " +
                $"{precisionSkippedCount} within STAAD coordinate precision. " +
                "X and Z were preserved, and all updated coordinates passed OpenSTAAD read-back verification.";
            if (!string.IsNullOrWhiteSpace(refreshError))
                message += " View refresh warning: " + refreshError;
            return true;
        }

        private static bool PreviewMatches(
            AdvanceElevationAlignPreviewRow[] displayedRows,
            AdvanceElevationAlignPreviewRow[] currentRows)
        {
            if (displayedRows.Length != currentRows.Length)
                return false;

            for (int i = 0; i < displayedRows.Length; i++)
            {
                AdvanceElevationAlignPreviewRow displayed = displayedRows[i];
                AdvanceElevationAlignPreviewRow current = currentRows[i];
                if (displayed.NodeId != current.NodeId ||
                    displayed.Disposition != current.Disposition ||
                    Math.Abs(displayed.X - current.X) > AdvanceConstants.CoordEpsilon ||
                    Math.Abs(displayed.OldY - current.OldY) > AdvanceConstants.CoordEpsilon ||
                    Math.Abs(displayed.NewY - current.NewY) > AdvanceConstants.CoordEpsilon ||
                    Math.Abs(displayed.Z - current.Z) > AdvanceConstants.CoordEpsilon)
                {
                    return false;
                }
            }

            return true;
        }

        private static List<AdvanceElevationAlignPreviewRow> FindRowsNotAtTarget(
            object geometry,
            List<AdvanceElevationAlignPreviewRow> rows,
            double coordinateTolerance,
            out string error)
        {
            error = string.Empty;
            var mismatches = new List<AdvanceElevationAlignPreviewRow>();

            foreach (AdvanceElevationAlignPreviewRow row in rows)
            {
                if (!AdvanceSelectionService.TryGetNodePoint(geometry, row.NodeId, out AdvancePoint3D actual, out string readError))
                {
                    if (string.IsNullOrWhiteSpace(error))
                        error = $"Node {row.NodeId} read failed: {readError}";
                    mismatches.Add(row);
                    continue;
                }

                if (!CoordinatesMatch(actual, row.X, row.NewY, row.Z, coordinateTolerance))
                {
                    if (string.IsNullOrWhiteSpace(error))
                    {
                        error =
                            $"Node {row.NodeId} expected ({Format(row.X)}, {Format(row.NewY)}, {Format(row.Z)}) " +
                            $"but read ({Format(actual.X)}, {Format(actual.Y)}, {Format(actual.Z)})";
                    }
                    mismatches.Add(row);
                }
            }

            return mismatches;
        }

        private static bool TrySetNodePointDynamic(
            object geometry,
            AdvanceElevationAlignPreviewRow row,
            out string error)
        {
            error = string.Empty;
            try
            {
                dynamic dynamicGeometry = geometry;
                dynamicGeometry.SetNodeCoordinate(row.NodeId, row.X, row.NewY, row.Z);
                return true;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        private static int CountRowsActuallyMoved(
            object geometry,
            List<AdvanceElevationAlignPreviewRow> rows,
            out int unchangedCount)
        {
            int movedCount = 0;
            unchangedCount = 0;

            foreach (AdvanceElevationAlignPreviewRow row in rows)
            {
                if (!AdvanceSelectionService.TryGetNodePoint(geometry, row.NodeId, out AdvancePoint3D actual, out _))
                    continue;

                if (Math.Abs(actual.Y - row.OldY) > AdvanceConstants.CoordEpsilon)
                    movedCount++;
                else
                    unchangedCount++;
            }

            return movedCount;
        }

        private static bool TryRefreshView(object openStaad, out string error)
        {
            error = string.Empty;
            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "View", out object view, out error))
                return false;

            return ComInvoke.TryInvoke(view, "RefreshView", out _, out error);
        }

        private static string TryRollback(
            object openStaad,
            object geometry,
            List<AdvanceElevationAlignPreviewRow> updatedRows,
            double coordinateTolerance)
        {
            if (updatedRows.Count == 0)
                return "No earlier node required rollback.";

            for (int i = updatedRows.Count - 1; i >= 0; i--)
            {
                AdvanceElevationAlignPreviewRow row = updatedRows[i];
                var original = new AdvancePoint3D(row.X, row.OldY, row.Z);
                AdvanceWriteService.TrySetNodePoint(geometry, row.NodeId, original, out _);
            }

            TryRefreshView(openStaad, out _);
            var rollbackFailures = new List<AdvanceElevationAlignPreviewRow>();
            foreach (AdvanceElevationAlignPreviewRow row in updatedRows)
            {
                if (!AdvanceSelectionService.TryGetNodePoint(geometry, row.NodeId, out AdvancePoint3D actual, out _) ||
                    !CoordinatesMatch(actual, row.X, row.OldY, row.Z, coordinateTolerance))
                {
                    rollbackFailures.Add(row);
                }
            }

            if (rollbackFailures.Count > 0)
            {
                foreach (AdvanceElevationAlignPreviewRow row in rollbackFailures)
                {
                    try
                    {
                        dynamic dynamicGeometry = geometry;
                        dynamicGeometry.SetNodeCoordinate(row.NodeId, row.X, row.OldY, row.Z);
                    }
                    catch
                    {
                    }
                }

                TryRefreshView(openStaad, out _);
                rollbackFailures = rollbackFailures
                    .Where(row =>
                        !AdvanceSelectionService.TryGetNodePoint(geometry, row.NodeId, out AdvancePoint3D actual, out _) ||
                        !CoordinatesMatch(actual, row.X, row.OldY, row.Z, coordinateTolerance))
                    .ToList();
            }

            if (rollbackFailures.Count == 0)
                return $"Read-back confirmed rollback of {updatedRows.Count} attempted node(s).";

            return "Rollback could not be confirmed for node(s): " +
                string.Join(", ", rollbackFailures.Select(row => row.NodeId)) + ".";
        }

        private static bool CoordinatesMatch(
            AdvancePoint3D actual,
            double expectedX,
            double expectedY,
            double expectedZ,
            double tolerance)
        {
            return Math.Abs(actual.X - expectedX) <= tolerance &&
                   Math.Abs(actual.Y - expectedY) <= tolerance &&
                   Math.Abs(actual.Z - expectedZ) <= tolerance;
        }

        private static double GetCoordinateStorageStep(object openStaad)
        {
            // SetNodeCoordinate in the tested STAAD version stores four decimal
            // places in the model's base length unit. Quantizing preview targets
            // first makes Apply and the next Preview agree with STAAD's model.
            return 0.0001;
        }

        private static double GetCoordinateTolerance(object openStaad)
        {
            return GetCoordinateStorageStep(openStaad) / 2.0 + AdvanceConstants.CoordEpsilon;
        }

        private static double QuantizeCoordinate(double value, double coordinateStep)
        {
            if (coordinateStep <= 0.0)
                return value;

            return Math.Round(value / coordinateStep, MidpointRounding.AwayFromZero) * coordinateStep;
        }

        private static string Format(double value)
        {
            return value.ToString("0.######", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
