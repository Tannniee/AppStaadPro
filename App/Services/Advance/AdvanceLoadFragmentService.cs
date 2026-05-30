using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Advance
{
    internal static class AdvanceLoadFragmentService
    {
        private const int UniformForceType = 3210;
        private const int PhysicalUniformForceType = 3275;
        private const double DistanceTolerance = 1e-6;

        internal static bool TryPreviewLoadFragments(double forceTolerance, out AdvanceLoadFragmentPreview preview, out string message)
        {
            preview = null;
            message = string.Empty;

            if (forceTolerance < 0)
                forceTolerance = 0;

            if (!AdvanceOpenStaadService.TryGetOpenStaad(out object openStaad, out message))
                return false;

            if (!OpenStaadLoadAdapter.TryGetLoadObject(openStaad, out object loadObj, out message))
                return false;

            if (!OpenStaadLoadAdapter.TryGetPrimaryLoadCaseNumbers(loadObj, out int[] loadCases, out message))
                return false;

            Array.Sort(loadCases);
            preview = new AdvanceLoadFragmentPreview
            {
                TotalPrimaryLoadCases = loadCases.Length
            };

            int originalActiveLoad = TryGetActiveLoad(loadObj);
            var items = new List<UniformLoadItem>();

            try
            {
                foreach (int loadCaseNo in loadCases)
                {
                    items.AddRange(ReadUniformLoadItems(loadObj, loadCaseNo));
                }
            }
            finally
            {
                if (originalActiveLoad > 0)
                    OpenStaadLoadAdapter.TrySetLoadActive(loadObj, originalActiveLoad, out _);
            }

            preview.TotalUniformLoadItems = items.Count;
            BuildGroups(items, forceTolerance, preview);
            message = BuildPreviewText(preview, forceTolerance);
            return true;
        }

        internal static bool TryPreviewLoadFragments(int loadCaseNumber, double forceTolerance, out AdvanceLoadFragmentPreview preview, out string message)
        {
            preview = null;
            message = string.Empty;

            if (loadCaseNumber <= 0)
            {
                message = "Please select a load case.";
                return false;
            }

            if (forceTolerance < 0)
                forceTolerance = 0;

            if (!AdvanceOpenStaadService.TryGetOpenStaad(out object openStaad, out message))
                return false;

            if (!OpenStaadLoadAdapter.TryGetLoadObject(openStaad, out object loadObj, out message))
                return false;

            preview = new AdvanceLoadFragmentPreview
            {
                TotalPrimaryLoadCases = 1
            };

            int originalActiveLoad = TryGetActiveLoad(loadObj);
            List<UniformLoadItem> items;

            try
            {
                items = ReadUniformLoadItems(loadObj, loadCaseNumber).ToList();
            }
            finally
            {
                if (originalActiveLoad > 0)
                    OpenStaadLoadAdapter.TrySetLoadActive(loadObj, originalActiveLoad, out _);
            }

            preview.TotalUniformLoadItems = items.Count;
            BuildGroups(items, forceTolerance, preview);
            message = BuildPreviewText(preview, forceTolerance);
            return true;
        }

        internal static string BuildPreviewText(AdvanceLoadFragmentPreview preview, double forceTolerance)
        {
            if (preview == null)
                return "No preview data.";

            var sb = new StringBuilder();
            sb.AppendLine("Fragmented UNI member loads:");

            if (preview.Groups.Count == 0)
            {
                sb.AppendLine("- None");
            }
            else
            {
                foreach (AdvanceLoadFragmentGroup group in preview.Groups
                    .OrderBy(item => item.LoadCaseNumber)
                    .ThenBy(item => item.TypeCode)
                    .ThenBy(item => item.Direction)
                    .ThenBy(item => item.RepresentativeForce))
                {
                    string caseText = string.IsNullOrWhiteSpace(group.LoadCaseTitle)
                        ? group.LoadCaseNumber.ToString(CultureInfo.InvariantCulture)
                        : group.LoadCaseNumber + ": " + group.LoadCaseTitle;

                    sb.AppendLine(
                        $"- LC {caseText} | {group.TypeName} {group.DirectionText} {FormatNumber(group.RepresentativeForce)} kN/m: {group.Items.Count} items, {group.MemberIds.Count} members, {group.DistancePatternCount} distance patterns");

                    foreach (AdvanceLoadFragmentItem item in group.Items.Take(6))
                    {
                        sb.AppendLine(
                            $"  item {item.LoadItemIndex} / type index {item.TypeIndex}: W={FormatNumber(item.Force)}, D={FormatDistances(item.Distances)}, members={FormatIds(item.Assignments, 8)}");
                    }

                    if (group.Items.Count > 6)
                        sb.AppendLine($"  ... {group.Items.Count - 6} more items");
                }
            }

            sb.AppendLine();
            sb.AppendLine("Scan:");
            sb.AppendLine($"- Primary load cases: {preview.TotalPrimaryLoadCases}");
            sb.AppendLine($"- UNI member load items: {preview.TotalUniformLoadItems}");
            sb.AppendLine($"- Fragmented groups: {preview.FragmentedGroupCount}");
            sb.AppendLine($"- Unique/non-fragmented groups: {preview.UniqueUniformLoadGroups}");
            sb.AppendLine($"- Force tolerance: {FormatNumber(forceTolerance)} kN/m");
            return sb.ToString().TrimEnd();
        }

        private static IEnumerable<UniformLoadItem> ReadUniformLoadItems(object loadObj, int loadCaseNo)
        {
            if (!OpenStaadLoadAdapter.TrySetLoadActive(loadObj, loadCaseNo, out string activeError))
                throw new InvalidOperationException("Could not activate load case " + loadCaseNo + ": " + activeError);

            OpenStaadLoadAdapter.TryGetLoadCaseTitle(loadObj, loadCaseNo, out string title, out _);

            if (!OpenStaadLoadAdapter.TryGetLoadItemsCount(loadObj, loadCaseNo, out int itemCount, out string countError))
                throw new InvalidOperationException("Could not read load item count for load case " + loadCaseNo + ": " + countError);

            var result = new List<UniformLoadItem>();
            var typeIndexes = new Dictionary<int, int>();

            for (int itemIndex = 0; itemIndex < itemCount; itemIndex++)
            {
                if (!OpenStaadLoadAdapter.TryGetLoadItemType(loadObj, loadCaseNo, itemIndex, out int typeCode, out _))
                    continue;

                int typeIndex = typeIndexes.TryGetValue(typeCode, out int currentTypeIndex) ? currentTypeIndex : 0;
                typeIndexes[typeCode] = typeIndex + 1;

                if (!IsUniformMemberLoad(typeCode))
                    continue;

                if (!OpenStaadLoadAdapter.TryGetMemberLoadInfo(loadObj, itemIndex, out int direction, out double[] force, out double[] distance, out _))
                    continue;

                int[] assignments = OpenStaadLoadAdapter.TryGetAssignmentListForLoadType(loadObj, typeCode, typeIndex, out int[] ids, out _)
                    ? NormalizeIds(ids)
                    : Array.Empty<int>();

                result.Add(new UniformLoadItem
                {
                    LoadCaseNumber = loadCaseNo,
                    LoadCaseTitle = title ?? string.Empty,
                    LoadItemIndex = itemIndex,
                    TypeIndex = typeIndex,
                    TypeCode = typeCode,
                    Direction = direction,
                    Force = GetAt(force, 0),
                    Distance = NormalizeDistances(distance),
                    Assignments = assignments
                });
            }

            return result;
        }

        private static void BuildGroups(IReadOnlyList<UniformLoadItem> items, double forceTolerance, AdvanceLoadFragmentPreview preview)
        {
            foreach (IGrouping<string, UniformLoadItem> baseGroup in items.GroupBy(BuildBaseKey))
            {
                List<UniformLoadItem> sortedItems = baseGroup
                    .OrderBy(item => item.Force)
                    .ThenBy(item => item.LoadItemIndex)
                    .ToList();

                int index = 0;
                while (index < sortedItems.Count)
                {
                    var cluster = new List<UniformLoadItem> { sortedItems[index] };
                    double referenceForce = sortedItems[index].Force;
                    index++;

                    while (index < sortedItems.Count && Math.Abs(sortedItems[index].Force - referenceForce) <= forceTolerance)
                    {
                        cluster.Add(sortedItems[index]);
                        index++;
                    }

                    if (cluster.Count <= 1)
                    {
                        preview.UniqueUniformLoadGroups++;
                        continue;
                    }

                    preview.AddGroup(BuildPreviewGroup(cluster));
                }
            }
        }

        private static AdvanceLoadFragmentGroup BuildPreviewGroup(IReadOnlyList<UniformLoadItem> items)
        {
            UniformLoadItem first = items[0];
            double minForce = items.Min(item => item.Force);
            double maxForce = items.Max(item => item.Force);

            return new AdvanceLoadFragmentGroup
            {
                LoadCaseNumber = first.LoadCaseNumber,
                LoadCaseTitle = first.LoadCaseTitle,
                TypeCode = first.TypeCode,
                TypeName = GetLoadTypeName(first.TypeCode),
                Direction = first.Direction,
                DirectionText = GetDirectionText(first.Direction),
                RepresentativeForce = items.Average(item => item.Force),
                MinForce = minForce,
                MaxForce = maxForce,
                DistancePatternCount = items.Select(item => BuildDistanceKey(item.Distance)).Distinct(StringComparer.Ordinal).Count(),
                MemberIds = NormalizeIds(items.SelectMany(item => item.Assignments)),
                Items = items.Select(item => new AdvanceLoadFragmentItem
                {
                    LoadItemIndex = item.LoadItemIndex,
                    TypeIndex = item.TypeIndex,
                    Force = item.Force,
                    Distances = item.Distance,
                    Assignments = item.Assignments
                }).ToArray()
            };
        }

        private static int TryGetActiveLoad(object loadObj)
        {
            return OpenStaadLoadAdapter.TryGetActiveLoad(loadObj, out int activeLoad, out _)
                ? activeLoad
                : 0;
        }

        private static bool IsUniformMemberLoad(int typeCode)
        {
            return typeCode == UniformForceType || typeCode == PhysicalUniformForceType;
        }

        private static string BuildBaseKey(UniformLoadItem item)
        {
            return item.LoadCaseNumber.ToString(CultureInfo.InvariantCulture) + "|" +
                   item.TypeCode.ToString(CultureInfo.InvariantCulture) + "|" +
                   item.Direction.ToString(CultureInfo.InvariantCulture);
        }

        private static string BuildDistanceKey(IEnumerable<double> values)
        {
            return string.Join("|", values.Select(value => Quantize(value, DistanceTolerance).ToString(CultureInfo.InvariantCulture)));
        }

        private static double Quantize(double value, double tolerance)
        {
            if (tolerance <= 0)
                return value;

            return Math.Round(value / tolerance) * tolerance;
        }

        private static double[] NormalizeDistances(IReadOnlyList<double> values)
        {
            return new[]
            {
                GetAt(values, 0),
                GetAt(values, 1),
                GetAt(values, 2)
            };
        }

        private static double GetAt(IReadOnlyList<double> values, int index)
        {
            return values != null && index >= 0 && index < values.Count
                ? values[index]
                : 0;
        }

        private static int[] NormalizeIds(IEnumerable<int> ids)
        {
            return ids == null
                ? Array.Empty<int>()
                : ids.Where(id => id > 0).Distinct().OrderBy(id => id).ToArray();
        }

        private static string GetLoadTypeName(int typeCode)
        {
            switch (typeCode)
            {
                case UniformForceType: return "UNI";
                case PhysicalUniformForceType: return "UNI Physical";
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

        private static string FormatDistances(IReadOnlyList<double> values)
        {
            if (values == null || values.Count == 0)
                return "0,0,0";

            return string.Join(",", values.Select(FormatNumber));
        }

        private static string FormatIds(IReadOnlyList<int> ids, int maxShown)
        {
            if (ids == null || ids.Count == 0)
                return "(none)";

            string text = string.Join(",", ids.Take(maxShown));
            if (ids.Count > maxShown)
                text += ",...";
            return text;
        }

        private sealed class UniformLoadItem
        {
            public int LoadCaseNumber { get; set; }
            public string LoadCaseTitle { get; set; } = string.Empty;
            public int LoadItemIndex { get; set; }
            public int TypeIndex { get; set; }
            public int TypeCode { get; set; }
            public int Direction { get; set; }
            public double Force { get; set; }
            public IReadOnlyList<double> Distance { get; set; } = Array.Empty<double>();
            public IReadOnlyList<int> Assignments { get; set; } = Array.Empty<int>();
        }
    }
}
