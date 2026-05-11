using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceOpenStaadService
    {
        internal static bool TryGetOpenStaad(out object openStaad, out string message)
        {
            if (StaadManager.TryGetOpenSTAAD(out openStaad, out message))
                return true;

            if (string.IsNullOrWhiteSpace(message))
                message = "Could not connect to OpenSTAAD.";

            return false;
        }

        internal static bool TryGetGeometry(out object openStaad, out object geometry, out string message)
        {
            geometry = null;

            if (!TryGetOpenStaad(out openStaad, out message))
                return false;

            if (OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out geometry, out message))
                return true;

            return false;
        }
    }
}
