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

        internal static bool TryGetProcessId(object openStaad, out int processId, out string error)
        {
            processId = 0;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(openStaad, "GetProcessId", out object result, out error))
                return false;

            if (ComResult.TryToInt32(result, out processId) && processId > 0)
                return true;

            error = "OpenSTAAD GetProcessId returned a non-positive or non-numeric value.";
            return false;
        }

        internal static bool TryGetMainWindowHandle(object openStaad, out long mainWindowHandle, out string error)
        {
            mainWindowHandle = 0;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(openStaad, "GetMainWindowHandle", out object result, out error))
                return false;

            try
            {
                mainWindowHandle = Convert.ToInt64(result);
                if (mainWindowHandle > 0)
                    return true;
            }
            catch
            {
            }

            error = "OpenSTAAD GetMainWindowHandle returned a non-positive or non-numeric value.";
            return false;
        }
    }
}
