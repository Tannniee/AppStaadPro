using System;
using System.Collections.Generic;
using System.Globalization;

namespace StaadSteelApp.Core.Interop
{
    internal static class ComResult
    {
        internal static bool IsZeroOrTrue(object result)
        {
            if (result == null)
                return true;

            if (result is bool boolResult)
                return boolResult;

            if (TryToInt32(result, out int code))
                return code == 0 || code == 1;

            return false;
        }

        internal static bool IsTrue(object result)
        {
            if (result == null)
                return false;

            if (result is bool boolResult)
                return boolResult;

            return TryToInt32(result, out int code) && code != 0;
        }

        internal static bool TryToInt32(object value, out int result)
        {
            result = 0;
            if (value == null)
                return false;

            try
            {
                result = Convert.ToInt32(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static int ToInt32(object value, int fallback = 0)
        {
            return TryToInt32(value, out int result) ? result : fallback;
        }

        internal static bool TryToDouble(object value, out double result)
        {
            result = 0.0;
            if (value == null)
                return false;

            try
            {
                result = Convert.ToDouble(value, CultureInfo.InvariantCulture);
                return true;
            }
            catch
            {
                return false;
            }
        }

        internal static int[] ToIntArray(object value)
        {
            if (value is int[] ints)
                return ints;

            if (!(value is Array array) || array.Length == 0)
                return Array.Empty<int>();

            var items = new List<int>(array.Length);
            foreach (object item in array)
            {
                if (TryToInt32(item, out int id))
                    items.Add(id);
            }

            return items.ToArray();
        }

        internal static double[] ToDoubleArray(object value)
        {
            if (value is double[] doubles)
                return doubles;

            if (!(value is Array array) || array.Length == 0)
                return Array.Empty<double>();

            var items = new List<double>(array.Length);
            foreach (object item in array)
            {
                items.Add(TryToDouble(item, out double number) ? number : 0.0);
            }

            return items.ToArray();
        }
    }
}
