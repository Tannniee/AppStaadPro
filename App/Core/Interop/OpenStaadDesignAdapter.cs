using System;
using System.Collections.Generic;

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

        internal static bool TryGetDesignObject(object openStaad, out object designObj, out string error)
        {
            return OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Design", out designObj, out error);
        }

        internal static bool TryGetDesignBriefCode(object designObj, int briefRef, out int code, out string error)
        {
            code = 0;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(designObj, "GetDesignBriefCode", out object codeObj, out error, new object[] { briefRef }))
                return false;

            code = ComResult.ToInt32(codeObj);
            return true;
        }

        internal static bool TryGetMemberDesignParameters(
            object designObj,
            int briefRef,
            int memberNo,
            out Dictionary<string, string> parameters,
            out string error)
        {
            parameters = null;
            error = string.Empty;

            Type paramType = Type.GetTypeFromProgID("StaadPro.MembSteelDgnParams");
            if (paramType == null)
            {
                error = "Could not create COM type from ProgID 'StaadPro.MembSteelDgnParams'.";
                return false;
            }

            object paramObject;
            try
            {
                paramObject = Activator.CreateInstance(paramType);
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }

            if (paramObject == null)
            {
                error = "STAAD member design parameter object could not be created.";
                return false;
            }

            object[] args = new object[] { briefRef, memberNo, paramObject };
            if (!ComInvoke.TryInvoke(designObj, "GetMemberDesignParameters", out object retRaw, out error, args))
                return false;

            int ret = ComResult.ToInt32(retRaw);
            if (ret != 0)
            {
                error = "GetMemberDesignParameters returned " + ret + ".";
                return false;
            }

            if (!ComInvoke.TryGetProperty(paramObject, "Count", out object countRaw, out error))
                return false;

            int count = ComResult.ToInt32(countRaw);
            if (count <= 0)
            {
                error = "No design parameters returned.";
                return false;
            }

            var parsed = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < count; i++)
            {
                if (!ComInvoke.TryGetProperty(paramObject, "Name", out object nameRaw, out string nameError, new object[] { i }))
                {
                    error = nameError;
                    continue;
                }

                string name = Convert.ToString(nameRaw);
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                if (!ComInvoke.TryGetProperty(paramObject, "Value", out object valueRaw, out string valueError, new object[] { i }))
                {
                    error = valueError;
                    valueRaw = string.Empty;
                }

                parsed[name.Trim().ToUpperInvariant()] = (Convert.ToString(valueRaw) ?? string.Empty).Trim();
            }

            parameters = parsed;
            return parsed.Count > 0;
        }

        private static double ToDouble(object value)
        {
            return ComResult.TryToDouble(value, out double result) ? result : 0.0;
        }
    }
}
