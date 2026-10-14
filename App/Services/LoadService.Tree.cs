using System;
using System.Collections.Generic;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class LoadService
    {
        public static IReadOnlyList<LoadTreeCase> GetPrimaryLoadTree()
        {
            if (!TryGetOpenStaad(out object openStaad, out _))
                return Array.Empty<LoadTreeCase>();

            return GetPrimaryLoadTree(openStaad);
        }

        internal static IReadOnlyList<LoadTreeCase> GetPrimaryLoadTree(object openStaad)
        {
            var loadCases = GetPrimaryLoadCases(openStaad);
            if (loadCases.Count == 0)
                return Array.Empty<LoadTreeCase>();

            if (!TryGetLoadObject(openStaad, out object loadObj, out _))
                return Array.Empty<LoadTreeCase>();

            var treeCases = new List<LoadTreeCase>(loadCases.Count);
            int originalActiveLoad = TryGetActiveLoad(loadObj);

            try
            {
                foreach (LoadCaseItem loadCase in loadCases)
                {
                    LoadTreeCase treeCase = BuildLoadTreeCase(loadObj, loadCase);
                    if (treeCase.Items.Count > 0)
                    {
                        treeCases.Add(treeCase);
                    }
                }
            }
            finally
            {
                if (originalActiveLoad > 0)
                {
                    TrySetLoadActive(loadObj, originalActiveLoad, out _);
                }
            }

            return treeCases;
        }

        private static LoadTreeCase BuildLoadTreeCase(object loadObj, LoadCaseItem loadCase)
        {
            if (!TrySetLoadActive(loadObj, loadCase.Number, out string activeError))
                throw new InvalidOperationException("Could not activate load case " + loadCase.Number + ": " + activeError);

            if (!OpenStaadLoadAdapter.TryGetLoadItemsCount(loadObj, loadCase.Number, out int itemCount, out string countError))
                throw new InvalidOperationException("Could not read load item count: " + countError);

            if (itemCount <= 0)
                return new LoadTreeCase(loadCase.Number, loadCase.Title, Array.Empty<LoadTreeItem>());

            var typeIndexes = new Dictionary<int, int>();
            var items = new List<LoadTreeItem>(itemCount);

            for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
            {
                if (!OpenStaadLoadAdapter.TryGetLoadItemType(loadObj, loadCase.Number, itemIndex, out int typeCode, out _))
                    continue;

                if (!IsSupportedTreeLoadType(typeCode))
                    continue;

                int typeIndex = typeIndexes.TryGetValue(typeCode, out int currentTypeIndex) ? currentTypeIndex : 0;
                typeIndexes[typeCode] = typeIndex + 1;

                try
                {
                    items.Add(BuildLoadTreeItem(loadObj, itemIndex, typeCode, typeIndex));
                }
                catch (Exception ex)
                {
                    AppLogger.Exception(
                        "Load tree skipped item " + itemIndex + " of type " + typeCode + ".",
                        ex);
                }
            }

            return new LoadTreeCase(loadCase.Number, loadCase.Title, items);
        }

        private static LoadTreeItem BuildLoadTreeItem(object loadObj, int itemIndex, int typeCode, int typeIndex)
        {
            int[] assignments = GetAssignmentListForLoadType(loadObj, typeCode, typeIndex);

            switch (typeCode)
            {
                case 3110:
                case 3120:
                case 3210:
                case 3275:
                    return typeCode == 3110 || typeCode == 3120
                        ? BuildNodalTreeItem(loadObj, itemIndex, typeCode, assignments)
                        : BuildMemberTreeItem(loadObj, itemIndex, typeCode, assignments);
                default:
                    throw new InvalidOperationException("Unsupported load item type " + typeCode + ".");
            }
        }

        private static LoadTreeItem BuildNodalTreeItem(object loadObj, int itemIndex, int typeCode, IReadOnlyList<int> assignments)
        {
            if (!OpenStaadLoadAdapter.TryGetNodalLoadInfo(loadObj, itemIndex, out double[] forces, out string error))
                throw new InvalidOperationException("GetNodalLoadInfo failed for item " + itemIndex + ": " + error);

            string summary = BuildNodalSummary(forces, typeCode);
            string details = BuildDetails(
                itemIndex,
                typeCode,
                GetLoadTypeName(typeCode),
                assignments,
                "Forces",
                $"FX={FormatNumber(forces[0])}, FY={FormatNumber(forces[1])}, FZ={FormatNumber(forces[2])}, MX={FormatNumber(forces[3])}, MY={FormatNumber(forces[4])}, MZ={FormatNumber(forces[5])}");

            return new LoadTreeItem(itemIndex, typeCode, GetLoadTypeName(typeCode), summary, details, assignments);
        }

        private static LoadTreeItem BuildMemberTreeItem(object loadObj, int itemIndex, int typeCode, IReadOnlyList<int> assignments)
        {
            if (!OpenStaadLoadAdapter.TryGetMemberLoadInfo(loadObj, itemIndex, out int direction, out double[] force, out double[] dist, out string error))
                throw new InvalidOperationException("GetMemberLoadInfo failed for item " + itemIndex + ": " + error);

            string summary = BuildMemberSummary(typeCode, direction, force, dist);
            string details = BuildDetails(
                itemIndex,
                typeCode,
                GetLoadTypeName(typeCode),
                assignments,
                "Parameters",
                $"Dir={GetDirectionText(direction)}, W1={FormatNumber(force[0])}, W2={FormatNumber(force[1])}, W3={FormatNumber(force[2])}, D1={FormatNumber(dist[0])}, D2={FormatNumber(dist[1])}, D3={FormatNumber(dist[2])}");

            return new LoadTreeItem(itemIndex, typeCode, GetLoadTypeName(typeCode), summary, details, assignments);
        }

    }
}
