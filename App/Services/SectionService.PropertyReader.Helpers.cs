using System;
using System.Collections.Generic;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        private static int[] ToIntArray(object raw)
        {
            if (raw == null) return Array.Empty<int>();

            if (raw is int singleInt)
                return singleInt > 0 ? new[] { singleInt } : Array.Empty<int>();

            if (raw is int[] typed)
            {
                var cleaned = new List<int>(typed.Length);
                var seen = new HashSet<int>();
                foreach (int value in typed)
                {
                    if (value <= 0) continue;
                    if (seen.Add(value))
                        cleaned.Add(value);
                }
                return cleaned.ToArray();
            }

            if (raw is Array array)
            {
                var cleaned = new List<int>(array.Length);
                var seen = new HashSet<int>();
                foreach (object item in array)
                {
                    int value;
                    try
                    {
                        value = Convert.ToInt32(item);
                    }
                    catch (Exception)
                    {
                        continue;
                    }

                    if (value <= 0) continue;
                    if (seen.Add(value))
                        cleaned.Add(value);
                }
                return cleaned.ToArray();
            }

            return Array.Empty<int>();
        }

        private static double[] ToDoubleArray(object raw, int minLength)
        {
            var values = new List<double>();

            if (raw is double[] doubles)
            {
                values.AddRange(doubles);
            }
            else if (raw is Array array)
            {
                foreach (object item in array)
                {
                    try
                    {
                        values.Add(Convert.ToDouble(item));
                    }
                    catch (Exception)
                    {
                        values.Add(0.0);
                    }
                }
            }

            while (values.Count < minLength)
                values.Add(0.0);

            return values.ToArray();
        }

        private static double GetArrayValue(double[] values, int index)
        {
            return values != null && index >= 0 && index < values.Length ? values[index] : 0.0;
        }
    }
}
