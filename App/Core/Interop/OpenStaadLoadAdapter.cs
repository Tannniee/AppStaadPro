using System;
using System.Collections.Generic;

namespace StaadSteelApp.Core.Interop
{
    internal static class OpenStaadLoadAdapter
    {
        internal static bool TryGetLoadObject(object openStaad, out object loadObj, out string error)
        {
            return OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Load", out loadObj, out error);
        }

        internal static bool TryGetAllLoadCaseNumbers(object openStaad, out int[] loadCases, out string error)
        {
            return TryGetLoadCaseNumbers(openStaad, includePrimary: true, includeCombinations: true, out loadCases, out error);
        }

        internal static bool TryGetLoadCaseNumbers(
            object openStaad,
            bool includePrimary,
            bool includeCombinations,
            out int[] loadCases,
            out string error)
        {
            loadCases = Array.Empty<int>();
            error = string.Empty;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Load", out object loadObj, out error))
                return false;

            var result = new List<int>();
            var errors = new List<string>();

            if (includePrimary)
            {
                if (TryGetNumberList(loadObj, "GetPrimaryLoadCaseCount", "GetPrimaryLoadCaseNumbers", out int[] primary, out string primaryError))
                    result.AddRange(primary);
                else if (!string.IsNullOrWhiteSpace(primaryError))
                    errors.Add(primaryError);
            }

            if (includeCombinations)
            {
                if (TryGetNumberList(loadObj, "GetLoadCombinationCaseCount", "GetLoadCombinationCaseNumbers", out int[] combos, out string comboError))
                    result.AddRange(combos);
                else if (!string.IsNullOrWhiteSpace(comboError))
                    errors.Add(comboError);
            }

            if (result.Count == 0 && errors.Count > 0)
            {
                error = string.Join(" ", errors);
                return false;
            }

            result.Sort();
            loadCases = Deduplicate(result).ToArray();
            if (errors.Count > 0)
                error = string.Join(" ", errors);

            return true;
        }

        internal static bool TryGetPrimaryLoadCaseNumbers(object loadObj, out int[] loadCases, out string error)
        {
            return TryGetNumberList(loadObj, "GetPrimaryLoadCaseCount", "GetPrimaryLoadCaseNumbers", out loadCases, out error);
        }

        internal static bool TryGetLoadCaseTitle(object loadObj, int loadCaseNo, out string title, out string error)
        {
            title = string.Empty;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(loadObj, "GetLoadCaseTitle", out object result, out error, new object[] { loadCaseNo }))
                return false;

            title = Convert.ToString(result) ?? string.Empty;
            return true;
        }

        internal static bool TryGetActiveLoad(object loadObj, out int activeLoad, out string error)
        {
            activeLoad = 0;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(loadObj, "GetActiveLoad", out object result, out error))
                return false;

            activeLoad = ComResult.ToInt32(result);
            return activeLoad > 0;
        }

        internal static bool TrySetLoadActive(object loadObj, int loadCaseNo, out string error)
        {
            error = string.Empty;

            if (!ComInvoke.TryInvoke(loadObj, "SetLoadActive", out object result, out error, new object[] { loadCaseNo }))
                return false;

            return ComResult.IsTrue(result);
        }

        internal static bool TryGetLoadItemsCount(object loadObj, int loadCaseNo, out int count, out string error)
        {
            count = 0;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(loadObj, "GetLoadItemsCount", out object result, out error, new object[] { loadCaseNo }))
                return false;

            count = Math.Max(0, ComResult.ToInt32(result));
            return true;
        }

        internal static bool TryGetLoadItemType(object loadObj, int loadCaseNo, int itemIndex, out int typeCode, out string error)
        {
            typeCode = 0;
            error = string.Empty;

            if (!ComInvoke.TryInvoke(loadObj, "GetLoadItemType", out object result, out error, new object[] { loadCaseNo, itemIndex }))
                return false;

            typeCode = ComResult.ToInt32(result);
            return typeCode > 0;
        }

        internal static bool TryGetNodalLoadInfo(object loadObj, int itemIndex, out double[] forces, out string error)
        {
            forces = new double[6];
            error = string.Empty;

            object[] args = new object[] { itemIndex, forces };
            if (!ComInvoke.TryInvoke(loadObj, "GetNodalLoadInfo", out object result, out error, args, ComInvoke.ByRefModifiers(2, 1)))
                return false;

            if (!ComResult.IsZeroOrTrue(result))
            {
                error = "GetNodalLoadInfo returned failure.";
                return false;
            }

            forces = OpenStaadAdapterHelpers.NormalizeDoubleArray(args[1], 6);
            return true;
        }

        internal static bool TryGetMemberLoadInfo(object loadObj, int itemIndex, out int direction, out double[] force, out double[] distance, out string error)
        {
            direction = 0;
            force = new double[3];
            distance = new double[3];
            error = string.Empty;

            object[] args = new object[] { itemIndex, direction, force, distance };
            if (!ComInvoke.TryInvoke(loadObj, "GetMemberLoadInfo", out object result, out error, args, ComInvoke.ByRefModifiers(4, 1, 2, 3)))
                return false;

            if (!ComResult.IsZeroOrTrue(result))
            {
                error = "GetMemberLoadInfo returned failure.";
                return false;
            }

            direction = ComResult.ToInt32(args[1]);
            force = OpenStaadAdapterHelpers.NormalizeDoubleArray(args[2], 3);
            distance = OpenStaadAdapterHelpers.NormalizeDoubleArray(args[3], 3);
            return true;
        }

        internal static bool TryGetAssignmentListForLoadType(object loadObj, int loadType, int loadIndex, out int[] assignments, out string error)
        {
            assignments = Array.Empty<int>();
            error = string.Empty;

            if (!ComInvoke.TryInvoke(loadObj, "GetListSizeForLoadType", out object sizeResult, out error, new object[] { loadType, loadIndex }))
                return false;

            int size = ComResult.ToInt32(sizeResult);
            if (size <= 0)
                return true;

            object[] args = new object[] { loadType, loadIndex, new int[size] };
            if (!ComInvoke.TryInvoke(loadObj, "GetAssignmentListForLoadType", out _, out error, args, ComInvoke.ByRefModifiers(3, 2)))
                return false;

            assignments = ComResult.ToIntArray(args[2]);
            return true;
        }

        internal static bool TryAddMemberUniformForce(object loadObj, int[] beamIds, int direction, double force, out string error)
        {
            error = string.Empty;

            if (!ComInvoke.TryInvoke(
                    loadObj,
                    "AddMemberUniformForce",
                    out object result,
                    out error,
                    new object[] { beamIds, direction, force, 0.0, 0.0, 0.0 }))
            {
                return false;
            }

            return ComResult.IsZeroOrTrue(result);
        }

        internal static bool TryAddNodalLoad(object loadObj, int[] nodeIds, double fx, double fy, double fz, double mx, double my, double mz, out string error)
        {
            error = string.Empty;

            if (!ComInvoke.TryInvoke(
                    loadObj,
                    "AddNodalLoad",
                    out object result,
                    out error,
                    new object[] { nodeIds, fx, fy, fz, mx, my, mz }))
            {
                return false;
            }

            return ComResult.IsZeroOrTrue(result);
        }

        private static bool TryGetNumberList(object loadObj, string countMethod, string listMethod, out int[] numbers, out string error)
        {
            numbers = Array.Empty<int>();
            error = string.Empty;

            if (!ComInvoke.TryInvoke(loadObj, countMethod, out object countResult, out error))
                return false;

            int count = ComResult.ToInt32(countResult);
            if (count <= 0)
                return true;

            int[] buffer = new int[count];
            object[] args = new object[] { buffer };
            if (!ComInvoke.TryInvoke(loadObj, listMethod, out _, out error, args, ComInvoke.ByRefModifiers(1, 0)))
                return false;

            numbers = ComResult.ToIntArray(args[0]);
            return true;
        }

        private static List<int> Deduplicate(List<int> values)
        {
            var result = new List<int>(values.Count);
            int? previous = null;
            foreach (int value in values)
            {
                if (value <= 0)
                    continue;

                if (!previous.HasValue || previous.Value != value)
                    result.Add(value);

                previous = value;
            }

            return result;
        }
    }
}
