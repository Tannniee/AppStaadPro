using System;

namespace StaadSteelApp.Core.Interop
{
    internal sealed class OpenStaadTaperedSectionData
    {
        internal double H1 { get; set; }
        internal double H2 { get; set; }
        internal double Tw { get; set; }
        internal double Bf { get; set; }
        internal double Tf { get; set; }
        internal double Bfb { get; set; }
        internal double Tfb { get; set; }
    }

    internal static partial class OpenStaadPropertyAdapter
    {
        internal const int TaperedSectionTypeCode = 680;

        internal static bool TryGetPropertyObject(object openStaad, out object propertyObj, out string error)
        {
            return OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Property", out propertyObj, out error);
        }

        private static double ToDouble(object value)
        {
            return ComResult.TryToDouble(value, out double result) ? result : 0.0;
        }

        private static int[] BuildByRefIndexes(int parameterCount)
        {
            int[] indexes = new int[Math.Max(0, parameterCount - 1)];
            for (int i = 1; i < parameterCount; i++)
                indexes[i - 1] = i;

            return indexes;
        }

        private static bool IsAssignSuccess(object result)
        {
            if (result == null)
                return false;

            if (result is bool boolResult)
                return boolResult;

            return ComResult.TryToDouble(result, out double number) && Math.Abs(number) > 0.0001;
        }

        private static int InvokeInt(object target, string methodName, out string error, params object[] args)
        {
            error = string.Empty;
            if (!ComInvoke.TryInvoke(target, methodName, out object result, out error, args))
                return -1;

            return result == null ? 0 : ComResult.ToInt32(result);
        }
    }
}
