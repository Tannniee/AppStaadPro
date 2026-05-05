using System;

namespace StaadSteelApp.Core.Interop
{
    internal static class OpenStaadAdapterHelpers
    {
        internal static bool TryGetSubObject(object openStaad, string propertyName, out object subObject, out string error)
        {
            subObject = null;
            error = string.Empty;

            if (openStaad == null)
            {
                error = "OpenSTAAD object is not available.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(propertyName))
            {
                error = "OpenSTAAD property name is blank.";
                return false;
            }

            if (!ComInvoke.TryGetProperty(openStaad, propertyName, out subObject, out error))
            {
                error = $"OpenSTAAD {propertyName} object is not available. {error}";
                return false;
            }

            if (subObject == null)
            {
                error = $"OpenSTAAD {propertyName} object is null.";
                return false;
            }

            return true;
        }

        internal static double[] NormalizeDoubleArray(object value, int expectedLength)
        {
            double[] source = ComResult.ToDoubleArray(value);
            if (expectedLength <= 0 || source.Length == expectedLength)
                return source;

            double[] result = new double[expectedLength];
            Array.Copy(source, result, Math.Min(source.Length, result.Length));
            return result;
        }
    }
}
