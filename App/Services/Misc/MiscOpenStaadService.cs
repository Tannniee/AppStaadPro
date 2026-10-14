using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Misc
{
    internal static class MiscOpenStaadService
    {
        internal static bool TryGetOpenStaad(out object openStaad, out string message)
        {
            if (StaadManager.TryGetOpenSTAAD(out openStaad, out message))
                return true;

            if (string.IsNullOrWhiteSpace(message))
                message = "Could not connect to OpenSTAAD.";

            return false;
        }

        internal static bool TryGetConnection(out StaadConnectionSnapshot connection, out string message)
        {
            if (StaadManager.TryGetConnection(out connection, out message))
                return true;

            if (string.IsNullOrWhiteSpace(message))
                message = "Could not connect to OpenSTAAD.";

            return false;
        }

        internal static bool TryGetProperty(out object propertyObj, out string message)
        {
            propertyObj = null;

            if (!TryGetOpenStaad(out object openStaad, out message))
                return false;

            return OpenStaadPropertyAdapter.TryGetPropertyObject(openStaad, out propertyObj, out message);
        }

        internal static bool TryGetProperty(object openStaad, out object propertyObj, out string message)
        {
            propertyObj = null;
            if (openStaad == null)
            {
                message = "OpenSTAAD connection is not available.";
                return false;
            }

            return OpenStaadPropertyAdapter.TryGetPropertyObject(openStaad, out propertyObj, out message);
        }
    }
}
