using System;

namespace StaadSteelApp.Core.Advance
{
    internal enum AdvanceElevationAlignDisposition
    {
        Ready = 0,
        Unchanged = 1,
        VerticalAlreadyCollinear = 2,
        SkippedVerticalOutside = 3,
        WithinCoordinatePrecision = 4
    }

    internal sealed class AdvanceElevationAlignPreviewRow
    {
        public int NodeId { get; set; }
        public double X { get; set; }
        public double OldY { get; set; }
        public double NewY { get; set; }
        public double Z { get; set; }
        public bool CanApply { get; set; }
        public string Status { get; set; }
        public AdvanceElevationAlignDisposition Disposition { get; set; }

        public double DeltaY => NewY - OldY;

        public bool RequiresUpdate => CanApply && Math.Abs(DeltaY) > AdvanceConstants.CoordEpsilon;
    }
}
