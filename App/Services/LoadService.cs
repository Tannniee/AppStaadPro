using System;
using System.Collections.Generic;
using System.Globalization;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static class LoadService
    {
        private static bool? _lastHighlightWasNodeLoad;
        private static string _lastHighlightKey = string.Empty;

        public sealed class LoadCaseItem
        {
            public LoadCaseItem(int number, string title)
            {
                Number = number;
                Title = title ?? string.Empty;
            }

            public int Number { get; }
            public string Title { get; }

            public override string ToString()
            {
                return string.IsNullOrWhiteSpace(Title)
                    ? Number.ToString(CultureInfo.InvariantCulture)
                    : $"{Number} - {Title}";
            }
        }

        public sealed class LoadTreeCase
        {
            public LoadTreeCase(int number, string title, IReadOnlyList<LoadTreeItem> items)
            {
                Number = number;
                Title = title ?? string.Empty;
                Items = items ?? Array.Empty<LoadTreeItem>();
            }

            public int Number { get; }
            public string Title { get; }
            public IReadOnlyList<LoadTreeItem> Items { get; }
        }

        public sealed class LoadTreeItem
        {
            public LoadTreeItem(int itemIndex, int typeCode, string typeName, string summary, string details, IReadOnlyList<int> assignments)
            {
                ItemIndex = itemIndex;
                TypeCode = typeCode;
                TypeName = typeName ?? string.Empty;
                Summary = string.IsNullOrWhiteSpace(summary) ? "Load Item" : summary;
                Details = details ?? string.Empty;
                Assignments = assignments ?? Array.Empty<int>();
            }

            public int ItemIndex { get; }
            public int TypeCode { get; }
            public string TypeName { get; }
            public string Summary { get; }
            public string Details { get; }
            public IReadOnlyList<int> Assignments { get; }
        }

        public enum MemberLoadDirection
        {
            LocalX = 1,
            LocalY = 2,
            LocalZ = 3,
            GlobalX = 4,
            GlobalY = 5,
            GlobalZ = 6,
            ProjectedX = 7,
            ProjectedY = 8,
            ProjectedZ = 9
        }

        public enum GroupType
        {
            Node = 1,
            Member = 2
        }

        public static IReadOnlyList<LoadCaseItem> GetPrimaryLoadCases()
        {
            var os = StaadManager.GetOpenSTAAD();
            if (os == null)
                return Array.Empty<LoadCaseItem>();

            object openStaad = os;
            if (!OpenStaadLoadAdapter.TryGetLoadObject(openStaad, out object loadObj, out _) ||
                !OpenStaadLoadAdapter.TryGetPrimaryLoadCaseNumbers(loadObj, out int[] numbers, out _) ||
                numbers.Length == 0)
            {
                return Array.Empty<LoadCaseItem>();
            }

            Array.Sort(numbers);
            var loadCases = new List<LoadCaseItem>(numbers.Length);
            foreach (int number in numbers)
            {
                string title = string.Empty;
                OpenStaadLoadAdapter.TryGetLoadCaseTitle(loadObj, number, out title, out _);

                loadCases.Add(new LoadCaseItem(number, title));
            }

            return loadCases;
        }

        public static IReadOnlyList<LoadTreeCase> GetPrimaryLoadTree()
        {
            var loadCases = GetPrimaryLoadCases();
            if (loadCases.Count == 0)
                return Array.Empty<LoadTreeCase>();

            var os = StaadManager.GetOpenSTAAD();
            if (os == null)
                return Array.Empty<LoadTreeCase>();

            object openStaad = os;
            if (!OpenStaadLoadAdapter.TryGetLoadObject(openStaad, out object loadObj, out _))
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

        public static IReadOnlyList<string> GetGroupNames(GroupType groupType)
        {
            var os = StaadManager.GetOpenSTAAD();
            if (os == null)
                return Array.Empty<string>();

            object openStaad = os;
            if (!OpenStaadGeometryAdapter.TryGetGroupNames(openStaad, (int)groupType, out string[] names, out _) ||
                names.Length == 0)
            {
                return Array.Empty<string>();
            }

            Array.Sort(names, StringComparer.CurrentCultureIgnoreCase);
            return names;
        }

        public static int[] GetGroupEntities(string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                return Array.Empty<int>();

            var os = StaadManager.GetOpenSTAAD();
            if (os == null)
                return Array.Empty<int>();

            object openStaad = os;
            return OpenStaadGeometryAdapter.TryGetGroupEntities(openStaad, groupName, out int[] entities, out _)
                ? entities
                : Array.Empty<int>();
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
                catch
                {
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

        private static LoadTreeItem CreateSimpleTreeItem(int itemIndex, int typeCode, string typeName, string summary, IReadOnlyList<int> assignments)
        {
            string details = BuildDetails(itemIndex, typeCode, typeName, assignments, null, null);
            return new LoadTreeItem(itemIndex, typeCode, typeName, summary, details, assignments);
        }

        public static void AssignUniformMemberLoad(int loadCaseNo, MemberLoadDirection direction, double force)
        {
            var os = StaadManager.GetOpenSTAAD();
            if (os == null)
                return;

            object openStaad = os;
            if (!OpenStaadSelectionAdapter.TryGetSelectedBeamIds(
                    openStaad,
                    false,
                    false,
                    new[] { 0, 1 },
                    out int[] beams,
                    out string error))
            {
                beams = Array.Empty<int>();
            }

            if (beams.Length == 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                    ? "Please select member(s) in STAAD."
                    : "Could not read selected member(s): " + error);
            }

            AssignUniformMemberLoad(loadCaseNo, beams, direction, force, "OpenSTAAD rejected the member load command.");
        }

        public static void AssignUniformMemberLoadToGroup(int loadCaseNo, string groupName, MemberLoadDirection direction, double force)
        {
            int[] members = GetGroupEntities(groupName);
            if (members.Length == 0)
                throw new InvalidOperationException("The selected member group does not contain any members.");

            AssignUniformMemberLoad(loadCaseNo, members, direction, force, "OpenSTAAD rejected the member group load command.");
        }

        public static void AssignNodalLoad(int loadCaseNo, double fx, double fy, double fz, double mx, double my, double mz)
        {
            var os = StaadManager.GetOpenSTAAD();
            if (os == null)
                return;

            object openStaad = os;
            int ignoredInvalidCount;
            if (!OpenStaadSelectionAdapter.TryGetSelectedNodeIds(
                    openStaad,
                    false,
                    false,
                    new[] { 0, 1 },
                    out int[] nodes,
                    out ignoredInvalidCount,
                    out string error))
            {
                nodes = Array.Empty<int>();
            }

            if (nodes.Length == 0)
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                    ? "Please select node(s) in STAAD."
                    : "Could not read selected node(s): " + error);
            }

            AssignNodalLoad(loadCaseNo, nodes, fx, fy, fz, mx, my, mz, "OpenSTAAD rejected the nodal load command.");
        }

        public static void AssignNodalLoadToGroup(int loadCaseNo, string groupName, double fx, double fy, double fz, double mx, double my, double mz)
        {
            int[] nodes = GetGroupEntities(groupName);
            if (nodes.Length == 0)
                throw new InvalidOperationException("The selected node group does not contain any nodes.");

            AssignNodalLoad(loadCaseNo, nodes, fx, fy, fz, mx, my, mz, "OpenSTAAD rejected the nodal group load command.");
        }

        public static void HighlightLoadAssignments(LoadTreeItem item)
        {
            if (item == null)
                return;

            var os = StaadManager.GetOpenSTAAD();
            if (os == null)
                throw new InvalidOperationException("STAAD is not available.");

            int[] ids = CopyIds(item.Assignments);
            if (ids.Length == 0)
                throw new InvalidOperationException("This load item does not contain any assigned entities.");

            object openStaad = os;
            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out string geometryError))
                throw new InvalidOperationException(geometryError);

            bool isNodeLoad = item.TypeCode == 3110 || item.TypeCode == 3120;
            string highlightKey = BuildHighlightKey(isNodeLoad, ids);

            if (string.Equals(_lastHighlightKey, highlightKey, StringComparison.Ordinal))
                return;

            if (_lastHighlightWasNodeLoad == null || _lastHighlightWasNodeLoad.Value != isNodeLoad)
            {
                if (isNodeLoad)
                    OpenStaadGeometryAdapter.TryClearMemberSelection(geometry, out _);
                else
                    OpenStaadGeometryAdapter.TryClearNodeSelection(geometry, out _);
            }

            bool highlighted = isNodeLoad
                ? OpenStaadGeometryAdapter.TryHighlightNodes(geometry, ids, out _)
                : OpenStaadGeometryAdapter.TryHighlightBeams(geometry, ids, out _);

            if (!highlighted)
            {
                throw new InvalidOperationException(
                    isNodeLoad
                        ? "Could not highlight assigned nodes in STAAD."
                        : "Could not highlight assigned members in STAAD.");
            }

            _lastHighlightWasNodeLoad = isNodeLoad;
            _lastHighlightKey = highlightKey;
        }

        private static void AssignUniformMemberLoad(int loadCaseNo, int[] beams, MemberLoadDirection direction, double force, string failureMessage)
        {
            var os = StaadManager.GetOpenSTAAD();
            if (os == null)
                return;

            object openStaad = os;
            if (!OpenStaadLoadAdapter.TryGetLoadObject(openStaad, out object loadObj, out string loadError))
                throw new InvalidOperationException(loadError);

            if (!TrySetLoadActive(loadObj, loadCaseNo, out string activeError))
                throw new InvalidOperationException("Could not activate load case " + loadCaseNo + ": " + activeError);

            if (!OpenStaadLoadAdapter.TryAddMemberUniformForce(loadObj, beams, (int)direction, force, out _))
                throw new InvalidOperationException(failureMessage);
        }

        private static void AssignNodalLoad(int loadCaseNo, int[] nodes, double fx, double fy, double fz, double mx, double my, double mz, string failureMessage)
        {
            var os = StaadManager.GetOpenSTAAD();
            if (os == null)
                return;

            object openStaad = os;
            if (!OpenStaadLoadAdapter.TryGetLoadObject(openStaad, out object loadObj, out string loadError))
                throw new InvalidOperationException(loadError);

            if (!TrySetLoadActive(loadObj, loadCaseNo, out string activeError))
                throw new InvalidOperationException("Could not activate load case " + loadCaseNo + ": " + activeError);

            if (!OpenStaadLoadAdapter.TryAddNodalLoad(loadObj, nodes, fx, fy, fz, mx, my, mz, out _))
                throw new InvalidOperationException(failureMessage);
        }

        private static int[] GetAssignmentListForLoadType(object loadObj, int loadType, int loadIndex)
        {
            return OpenStaadLoadAdapter.TryGetAssignmentListForLoadType(loadObj, loadType, loadIndex, out int[] assignments, out _)
                ? assignments
                : Array.Empty<int>();
        }

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

        private static string BuildDetails(int itemIndex, int typeCode, string typeName, IReadOnlyList<int> assignments, string parameterLabel, string parameterValue)
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

        private static bool TrySetLoadActive(object loadObj, int loadCaseNo, out string error)
        {
            return OpenStaadLoadAdapter.TrySetLoadActive(loadObj, loadCaseNo, out error);
        }
    }
}
