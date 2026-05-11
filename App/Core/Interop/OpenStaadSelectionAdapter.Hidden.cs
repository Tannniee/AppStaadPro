using System;
using System.Globalization;
using StaadSteelApp.Core;

namespace StaadSteelApp.Core.Interop
{
    internal static partial class OpenStaadSelectionAdapter
    {
        private static int? TryGetHiddenEntitiesFlag(object geometry)
        {
            try
            {
                object result = ComInvoke.Invoke(geometry, "GetFlagForHiddenEntities");
                return Convert.ToInt32(result, CultureInfo.InvariantCulture);
            }
            catch (Exception ex)
            {
                AppLogger.Exception("OpenSTAAD did not return the hidden-entities flag.", ex);
                return null;
            }
        }

        private static void TrySetHiddenEntitiesFlag(object geometry, int flagValue)
        {
            try
            {
                ComInvoke.Invoke(geometry, "SetFlagForHiddenEntities", new object[] { flagValue });
            }
            catch (Exception ex)
            {
                AppLogger.Exception("OpenSTAAD did not accept the hidden-entities flag update.", ex);
            }
        }
    }
}
