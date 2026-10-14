using System;
using System.Collections.Generic;
using System.Globalization;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class LoadService
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
            if (!TryGetOpenStaad(out object openStaad, out _))
                return Array.Empty<LoadCaseItem>();

            return GetPrimaryLoadCases(openStaad);
        }

        internal static IReadOnlyList<LoadCaseItem> GetPrimaryLoadCases(object openStaad)
        {
            if (!TryGetLoadObject(openStaad, out object loadObj, out _))
                return Array.Empty<LoadCaseItem>();

            if (!OpenStaadLoadAdapter.TryGetPrimaryLoadCaseNumbers(loadObj, out int[] numbers, out _) ||
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

        public static IReadOnlyList<string> GetGroupNames(GroupType groupType)
        {
            if (!TryGetOpenStaad(out object openStaad, out _))
                return Array.Empty<string>();

            return GetGroupNames(openStaad, groupType);
        }

        internal static IReadOnlyList<string> GetGroupNames(object openStaad, GroupType groupType)
        {

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

            if (!TryGetOpenStaad(out object openStaad, out _))
                return Array.Empty<int>();

            return OpenStaadGeometryAdapter.TryGetGroupEntities(openStaad, groupName, out int[] entities, out _)
                ? entities
                : Array.Empty<int>();
        }

        public static void AssignUniformMemberLoad(int loadCaseNo, MemberLoadDirection direction, double force)
        {
            if (!TryGetOpenStaad(out object openStaad, out string connectionError))
                throw new InvalidOperationException(connectionError);

            AssignUniformMemberLoad(openStaad, loadCaseNo, direction, force);
        }

        internal static void AssignUniformMemberLoad(object openStaad, int loadCaseNo, MemberLoadDirection direction, double force)
        {

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

            AssignUniformMemberLoad(openStaad, loadCaseNo, beams, direction, force, "OpenSTAAD rejected the member load command.");
        }

        public static void AssignUniformMemberLoadToGroup(int loadCaseNo, string groupName, MemberLoadDirection direction, double force)
        {
            if (!TryGetOpenStaad(out object openStaad, out string connectionError))
                throw new InvalidOperationException(connectionError);

            AssignUniformMemberLoadToGroup(openStaad, loadCaseNo, groupName, direction, force);
        }

        internal static void AssignUniformMemberLoadToGroup(
            object openStaad,
            int loadCaseNo,
            string groupName,
            MemberLoadDirection direction,
            double force)
        {

            int[] members = GetGroupEntitiesOrThrow(openStaad, groupName);
            if (members.Length == 0)
                throw new InvalidOperationException("The selected member group does not contain any members.");

            AssignUniformMemberLoad(openStaad, loadCaseNo, members, direction, force, "OpenSTAAD rejected the member group load command.");
        }

        public static void AssignNodalLoad(int loadCaseNo, double fx, double fy, double fz, double mx, double my, double mz)
        {
            if (!TryGetOpenStaad(out object openStaad, out string connectionError))
                throw new InvalidOperationException(connectionError);

            AssignNodalLoad(openStaad, loadCaseNo, fx, fy, fz, mx, my, mz);
        }

        internal static void AssignNodalLoad(object openStaad, int loadCaseNo, double fx, double fy, double fz, double mx, double my, double mz)
        {

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

            AssignNodalLoad(openStaad, loadCaseNo, nodes, fx, fy, fz, mx, my, mz, "OpenSTAAD rejected the nodal load command.");
        }

        public static void AssignNodalLoadToGroup(int loadCaseNo, string groupName, double fx, double fy, double fz, double mx, double my, double mz)
        {
            if (!TryGetOpenStaad(out object openStaad, out string connectionError))
                throw new InvalidOperationException(connectionError);

            AssignNodalLoadToGroup(openStaad, loadCaseNo, groupName, fx, fy, fz, mx, my, mz);
        }

        internal static void AssignNodalLoadToGroup(
            object openStaad,
            int loadCaseNo,
            string groupName,
            double fx,
            double fy,
            double fz,
            double mx,
            double my,
            double mz)
        {

            int[] nodes = GetGroupEntitiesOrThrow(openStaad, groupName);
            if (nodes.Length == 0)
                throw new InvalidOperationException("The selected node group does not contain any nodes.");

            AssignNodalLoad(openStaad, loadCaseNo, nodes, fx, fy, fz, mx, my, mz, "OpenSTAAD rejected the nodal group load command.");
        }

        public static void HighlightLoadAssignments(LoadTreeItem item)
        {
            if (!TryGetOpenStaad(out object openStaad, out string connectionError))
                throw new InvalidOperationException(connectionError);

            HighlightLoadAssignments(openStaad, item);
        }

        internal static void HighlightLoadAssignments(object openStaad, LoadTreeItem item)
        {
            if (item == null)
                return;

            if (!OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out object geometry, out string geometryError))
                throw new InvalidOperationException(geometryError);

            int[] ids = CopyIds(item.Assignments);
            if (ids.Length == 0)
                throw new InvalidOperationException("This load item does not contain any assigned entities.");

            bool isNodeLoad = item.TypeCode == 3110 || item.TypeCode == 3120;
            string highlightKey = StaadManager.GetModelCacheKey(openStaad) + "|" + BuildHighlightKey(isNodeLoad, ids);

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

        private static void AssignUniformMemberLoad(
            object openStaad,
            int loadCaseNo,
            int[] beams,
            MemberLoadDirection direction,
            double force,
            string failureMessage)
        {
            if (!TryGetLoadObject(openStaad, out object loadObj, out string loadError))
                throw new InvalidOperationException(loadError);

            if (!TrySetLoadActive(loadObj, loadCaseNo, out string activeError))
                throw new InvalidOperationException("Could not activate load case " + loadCaseNo + ": " + activeError);

            if (!OpenStaadLoadAdapter.TryAddMemberUniformForce(loadObj, beams, (int)direction, force, out _))
                throw new InvalidOperationException(failureMessage);
        }

        private static void AssignNodalLoad(
            object openStaad,
            int loadCaseNo,
            int[] nodes,
            double fx,
            double fy,
            double fz,
            double mx,
            double my,
            double mz,
            string failureMessage)
        {
            if (!TryGetLoadObject(openStaad, out object loadObj, out string loadError))
                throw new InvalidOperationException(loadError);

            if (!TrySetLoadActive(loadObj, loadCaseNo, out string activeError))
                throw new InvalidOperationException("Could not activate load case " + loadCaseNo + ": " + activeError);

            if (!OpenStaadLoadAdapter.TryAddNodalLoad(loadObj, nodes, fx, fy, fz, mx, my, mz, out _))
                throw new InvalidOperationException(failureMessage);
        }

        private static bool TryGetOpenStaad(out object openStaad, out string error)
        {
            if (StaadManager.TryGetOpenSTAAD(out openStaad, out error))
                return true;

            if (string.IsNullOrWhiteSpace(error))
                error = "STAAD is not available.";

            return false;
        }

        private static bool TryGetLoadObject(out object loadObj, out string error)
        {
            loadObj = null;

            if (!TryGetOpenStaad(out object openStaad, out error))
                return false;

            return OpenStaadLoadAdapter.TryGetLoadObject(openStaad, out loadObj, out error);
        }

        private static bool TryGetLoadObject(object openStaad, out object loadObj, out string error)
        {
            loadObj = null;
            if (openStaad == null)
            {
                error = "OpenSTAAD connection is not available.";
                return false;
            }

            return OpenStaadLoadAdapter.TryGetLoadObject(openStaad, out loadObj, out error);
        }

        private static int[] GetGroupEntitiesOrThrow(object openStaad, string groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName))
                throw new InvalidOperationException("Please select a group.");

            if (!OpenStaadGeometryAdapter.TryGetGroupEntities(openStaad, groupName, out int[] entities, out string error))
            {
                throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                    ? "Could not read selected group entities."
                    : "Could not read selected group entities: " + error);
            }

            return entities ?? Array.Empty<int>();
        }

        private static int[] GetAssignmentListForLoadType(object loadObj, int loadType, int loadIndex)
        {
            return OpenStaadLoadAdapter.TryGetAssignmentListForLoadType(loadObj, loadType, loadIndex, out int[] assignments, out _)
                ? assignments
                : Array.Empty<int>();
        }

        private static bool TrySetLoadActive(object loadObj, int loadCaseNo, out string error)
        {
            return OpenStaadLoadAdapter.TrySetLoadActive(loadObj, loadCaseNo, out error);
        }
    }
}
