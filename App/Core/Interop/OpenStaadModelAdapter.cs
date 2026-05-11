using System;

namespace StaadSteelApp.Core.Interop
{
    internal static class OpenStaadModelAdapter
    {
        internal static bool TryGetBaseUnit(object openStaad, out int baseUnit, out string error)
        {
            baseUnit = 0;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(openStaad, "GetBaseUnit", out object result, out error))
                return false;

            if (ComResult.TryToInt32(result, out baseUnit))
                return true;

            error = "OpenSTAAD GetBaseUnit returned a non-numeric value.";
            return false;
        }

        internal static bool TryGetStaadFile(object openStaad, object fullPathFlag, out string staadFilePath, out string error)
        {
            staadFilePath = string.Empty;
            error = string.Empty;

            object[] args = new object[] { string.Empty, fullPathFlag };
            if (!ComInvoke.TryInvoke(openStaad, "GetSTAADFile", out object result, out error, args, ComInvoke.ByRefModifiers(2, 0)))
                return false;

            string pathFromOut = Convert.ToString(args[0]) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(pathFromOut))
            {
                staadFilePath = pathFromOut;
                return true;
            }

            string pathFromRet = Convert.ToString(result) ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(pathFromRet))
            {
                staadFilePath = pathFromRet;
                return true;
            }

            return false;
        }
    }
}
