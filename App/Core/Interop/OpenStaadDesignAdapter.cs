using System;

namespace StaadSteelApp.Core.Interop
{
    internal sealed class OpenStaadSteelDesignResult
    {
        internal string DesignCode { get; set; } = string.Empty;
        internal string DesignStatus { get; set; } = string.Empty;
        internal double CriticalRatio { get; set; }
        internal double AllowableRatio { get; set; }
        internal int CriticalLoadCase { get; set; }
        internal double CriticalSection { get; set; }
        internal string CriticalClause { get; set; } = string.Empty;
        internal string DesignSection { get; set; } = string.Empty;
        internal double[] DesignForces { get; set; } = new double[3];
        internal double KLByR { get; set; }
    }

    internal static class OpenStaadDesignAdapter
    {
        internal static bool TryGetMemberSteelDesignResults(
            object openStaad,
            int memberNo,
            out OpenStaadSteelDesignResult designResult,
            out string error)
        {
            designResult = null;
            error = string.Empty;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Output", out object outputObj, out error))
                return false;

            object[] args = new object[]
            {
                memberNo,
                string.Empty,
                string.Empty,
                0.0,
                0.0,
                0,
                0.0,
                string.Empty,
                string.Empty,
                new double[3],
                0.0
            };

            if (!ComInvoke.TryInvoke(
                    outputObj,
                    "GetMemberSteelDesignResults",
                    out _,
                    out error,
                    args,
                    ComInvoke.ByRefModifiers(11, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10)))
                return false;

            int criticalLoadCase = ComResult.ToInt32(args[5]);
            if (criticalLoadCase <= 0)
            {
                error = "STAAD steel design result did not return a critical load case.";
                return false;
            }

            designResult = new OpenStaadSteelDesignResult
            {
                DesignCode = Convert.ToString(args[1]) ?? string.Empty,
                DesignStatus = Convert.ToString(args[2]) ?? string.Empty,
                CriticalRatio = ToDouble(args[3]),
                AllowableRatio = ToDouble(args[4]),
                CriticalLoadCase = criticalLoadCase,
                CriticalSection = ToDouble(args[6]),
                CriticalClause = Convert.ToString(args[7]) ?? string.Empty,
                DesignSection = Convert.ToString(args[8]) ?? string.Empty,
                DesignForces = OpenStaadAdapterHelpers.NormalizeDoubleArray(args[9], 3),
                KLByR = ToDouble(args[10])
            };

            return true;
        }

        private static double ToDouble(object value)
        {
            return ComResult.TryToDouble(value, out double result) ? result : 0.0;
        }
    }
}
