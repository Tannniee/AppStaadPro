using System;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceElevationAlignCalculator
    {
        internal static bool TryCalculateYOnly(
            AdvancePoint3D vectorA,
            AdvancePoint3D vectorB,
            AdvancePoint3D target,
            out double alignedY,
            out bool canApply,
            out AdvanceElevationAlignDisposition disposition,
            out string status,
            out string error)
        {
            alignedY = target.Y;
            canApply = false;
            disposition = AdvanceElevationAlignDisposition.Unchanged;
            status = string.Empty;
            error = string.Empty;

            double dx = vectorB.X - vectorA.X;
            double dy = vectorB.Y - vectorA.Y;
            double dz = vectorB.Z - vectorA.Z;
            double fullLength = Math.Sqrt(dx * dx + dy * dy + dz * dz);

            if (fullLength <= AdvanceConstants.AxisEpsilon)
            {
                error = "The two vector nodes have identical coordinates or are too close to define a direction.";
                return false;
            }

            double planLengthSquared = dx * dx + dz * dz;
            double planLength = Math.Sqrt(planLengthSquared);
            double verticalTolerance = AdvanceConstants.AxisEpsilon * Math.Max(1.0, fullLength);

            if (planLength <= verticalTolerance)
            {
                double offsetX = target.X - vectorA.X;
                double offsetZ = target.Z - vectorA.Z;
                double planOffset = Math.Sqrt(offsetX * offsetX + offsetZ * offsetZ);

                if (planOffset <= AdvanceConstants.CoordEpsilon)
                {
                    disposition = AdvanceElevationAlignDisposition.VerticalAlreadyCollinear;
                    status = "Vertical vector: already collinear; Y unchanged";
                }
                else
                {
                    disposition = AdvanceElevationAlignDisposition.SkippedVerticalOutside;
                    status = "Skipped: vertical vector requires X/Z changes";
                }

                return true;
            }

            double t = ((target.X - vectorA.X) * dx + (target.Z - vectorA.Z) * dz) / planLengthSquared;
            alignedY = vectorA.Y + t * dy;
            canApply = true;

            if (Math.Abs(alignedY - target.Y) <= AdvanceConstants.CoordEpsilon)
            {
                disposition = AdvanceElevationAlignDisposition.Unchanged;
                status = "No change";
            }
            else
            {
                disposition = AdvanceElevationAlignDisposition.Ready;
                status = "Ready";
            }

            return true;
        }
    }
}
