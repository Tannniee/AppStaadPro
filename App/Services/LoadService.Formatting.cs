using System;
using System.Collections.Generic;
using System.Globalization;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class LoadService
    {
        private static string BuildMemberSummary(int typeCode, int direction, IReadOnlyList<double> force, IReadOnlyList<double> dist)
        {
            string dir = GetDirectionText(direction);

            switch (typeCode)
            {
                case 3210:
                case 3275:
                    return "UNI " + dir + " " + FormatNumber(force[0]) + " kN/m";
                default:
                    return GetLoadTypeName(typeCode) + " " + dir;
            }
        }

        private static string BuildNodalSummary(IReadOnlyList<double> force, int typeCode)
        {
            var parts = new List<string>(6);

            AppendForce(parts, "FX", force[0], "kN");
            AppendForce(parts, "FY", force[1], "kN");
            AppendForce(parts, "FZ", force[2], "kN");
            AppendForce(parts, "MX", force[3], "kNm");
            AppendForce(parts, "MY", force[4], "kNm");
            AppendForce(parts, "MZ", force[5], "kNm");

            if (parts.Count == 0)
                return GetLoadTypeName(typeCode);

            return string.Join(" ", parts);
        }

        private static void AppendForce(ICollection<string> parts, string name, double value, string unit)
        {
            if (Math.Abs(value) < 1e-9)
                return;

            parts.Add(name + " " + FormatNumber(value) + " " + unit);
        }

        private static string BuildDetails(
            int itemIndex,
            int typeCode,
            string typeName,
            IReadOnlyList<int> assignments,
            string parameterLabel,
            string parameterValue)
        {
            string assignmentText = assignments.Count == 0
                ? "(none)"
                : string.Join(", ", assignments);

            if (string.IsNullOrWhiteSpace(parameterLabel) || string.IsNullOrWhiteSpace(parameterValue))
            {
                return $"Item Index: {itemIndex}\r\nType: {typeName} ({typeCode})\r\nAssignments: {assignmentText}";
            }

            return $"Item Index: {itemIndex}\r\nType: {typeName} ({typeCode})\r\n{parameterLabel}: {parameterValue}\r\nAssignments: {assignmentText}";
        }

        private static bool IsSupportedTreeLoadType(int typeCode)
        {
            return typeCode == 3110 ||
                   typeCode == 3120 ||
                   typeCode == 3210 ||
                   typeCode == 3275;
        }

        private static int TryGetActiveLoad(object loadObj)
        {
            return OpenStaadLoadAdapter.TryGetActiveLoad(loadObj, out int activeLoad, out _)
                ? activeLoad
                : 0;
        }

        private static string GetLoadTypeName(int typeCode)
        {
            switch (typeCode)
            {
                case 4000: return "SelfWeight";
                case 3110: return "Nodal Load";
                case 3120: return "Nodal Load (Inclined)";
                case 3312: return "Region Node Load";
                case 3910: return "Support Displacement";
                case 3210: return "Uniform Force";
                case 3220: return "Uniform Moment";
                case 3230: return "Concentrated Force";
                case 3240: return "Concentrated Moment";
                case 3250: return "Linear Varying";
                case 3260: return "Trapezoidal";
                case 3261: return "Hydrostatic";
                case 3275: return "Uniform Force (Physical)";
                case 3280: return "Uniform Moment (Physical)";
                case 3285: return "Concentrated Force (Physical)";
                case 3290: return "Concentrated Moment (Physical)";
                case 3295: return "Trapezoidal (Physical)";
                case 3810: return "Fixed End";
                default: return "Load Type " + typeCode;
            }
        }

        private static string GetDirectionText(int direction)
        {
            switch (direction)
            {
                case 1: return "X";
                case 2: return "Y";
                case 3: return "Z";
                case 4: return "GX";
                case 5: return "GY";
                case 6: return "GZ";
                case 7: return "PX";
                case 8: return "PY";
                case 9: return "PZ";
                default: return direction.ToString(CultureInfo.InvariantCulture);
            }
        }

        private static string FormatNumber(double value)
        {
            return value.ToString("0.#####", CultureInfo.InvariantCulture);
        }

        private static int[] CopyIds(IReadOnlyList<int> values)
        {
            if (values == null || values.Count == 0)
                return Array.Empty<int>();

            var items = new int[values.Count];
            for (int i = 0; i < values.Count; i++)
            {
                items[i] = values[i];
            }

            return items;
        }

        private static string BuildHighlightKey(bool isNodeLoad, IReadOnlyList<int> ids)
        {
            return (isNodeLoad ? "N:" : "M:") + string.Join(",", ids);
        }
    }
}
