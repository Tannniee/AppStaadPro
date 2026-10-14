using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    internal static partial class SectionUptModule
    {
        private static int GetOrCreateUPTTableId(object os, int targetType)
        {
            if (!OpenStaadPropertyAdapter.TryGetPropertyObject(os, out object propObjForTables, out string propertyError))
                throw new SectionPropertyException(propertyError);

            int tableCount = OpenStaadPropertyAdapter.GetUserProvidedTableCount(propObjForTables, out string countError);
            if (tableCount < 0 || !string.IsNullOrWhiteSpace(countError))
                throw new SectionPropertyException("Could not read the existing UPT table count: " + countError);

            int[] existingTables = Array.Empty<int>();

            if (tableCount > 0)
            {
                if (!OpenStaadPropertyAdapter.TryGetUserProvidedTableList(
                        propObjForTables,
                        tableCount,
                        out int[] tableIds,
                        out string tableListError))
                {
                    throw new SectionPropertyException("Could not read the existing UPT table list: " + tableListError);
                }

                existingTables = tableIds
                    .Where(tableId => tableId > 0)
                    .Distinct()
                    .OrderBy(tableId => tableId)
                    .ToArray();

                if (existingTables.Length != tableCount)
                {
                    throw new SectionPropertyException(
                        $"OpenSTAAD reported {tableCount} UPT tables but returned {existingTables.Length} valid table IDs.");
                }

                foreach (int tableId in existingTables)
                {
                    if (!OpenStaadPropertyAdapter.TryGetUserProvidedTableSectionType(
                            propObjForTables,
                            tableId,
                            out int currentType,
                            out string tableTypeError))
                    {
                        throw new SectionPropertyException(
                            $"Could not read the section type of UPT table {tableId}: {tableTypeError}");
                    }

                    if (IsMatchingUPTTableType(currentType, targetType))
                        return tableId;
                }
            }

            int newTableId = GetNextUPTTableId(existingTables);
            if (!OpenStaadPropertyAdapter.TryCreateUPTTableEx(
                    propObjForTables,
                    newTableId,
                    targetType,
                    out string createError))
            {
                throw new SectionPropertyException(
                    $"Could not create UPT table {newTableId} for table type {targetType}: {createError}");
            }

            return newTableId;
        }

        private static bool IsMatchingUPTTableType(int reportedSectionType, int targetTableType)
        {
            // CreateUPTTableEx accepts compact table types 1..10, while
            // GetUserProvidedTableSectionType returns STAAD section codes
            // 412, 422, ... 502. Some OpenSTAAD versions may return the
            // compact value, so support both representations.
            if (reportedSectionType == targetTableType)
                return true;

            switch (targetTableType)
            {
                case 1: return reportedSectionType == 412; // Wide Flange
                case 2: return reportedSectionType == 422; // Channel
                case 3: return reportedSectionType == 432; // Angle
                case 4: return reportedSectionType == 442; // Double Angle
                case 5: return reportedSectionType == 452; // Tee
                case 6: return reportedSectionType == 462; // Pipe
                case 7: return reportedSectionType == 472; // Tube
                case 8: return reportedSectionType == 482; // General
                case 9: return reportedSectionType == 492; // ISECTION
                case 10: return reportedSectionType == 502; // Prismatic
                default: return false;
            }
        }

        private static int GetNextUPTTableId(int[] existingTables)
        {
            if (existingTables == null || existingTables.Length == 0)
                return 1;

            int maximumTableId = existingTables.Max();
            if (maximumTableId == int.MaxValue)
                throw new SectionPropertyException("Cannot create another UPT table because the table ID range is exhausted.");

            return maximumTableId + 1;
        }

        private static bool IsUptSectionAddSuccess(object ret)
        {
            if (ret == null) return false;
            if (ret is bool b) return !b;
            if (ret is int i) return i == 0 || i == -6045;
            if (ret is short s) return s == 0 || s == -6045;
            try
            {
                int value = Convert.ToInt32(ret);
                return value == 0 || value == -6045;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static int FindPropertyIdByNameWithRetry(
            object os,
            string propertyName,
            Func<object, string, int> checkPropertyExists,
            int attempts,
            int delayMs)
        {
            if (os == null || string.IsNullOrWhiteSpace(propertyName) || checkPropertyExists == null)
                return 0;

            string target = propertyName.Trim();
            for (int i = 0; i < attempts; i++)
            {
                int id = 0;
                try
                {
                    id = checkPropertyExists(os, target);
                }
                catch (Exception)
                {
                    id = 0;
                }

                if (id > 0) return id;

                if (i < attempts - 1)
                    Thread.Sleep(delayMs);
            }

            return 0;
        }

        private static int CreatePropertyFromUptTable(object property, int tableId, string sectionName)
        {
            if (property == null || tableId <= 0 || string.IsNullOrWhiteSpace(sectionName))
                return 0;

            string baseName = sectionName.Trim();
            string[] candidateNames = new[]
            {
                baseName,
                "UPT " + baseName
            };

            foreach (string candidate in candidateNames)
            {
                int propId = OpenStaadPropertyAdapter.CreatePropertyFromUPTTable(property, tableId, candidate, out _);
                if (propId > 0) return propId;
            }

            return 0;
        }

        private static double PositiveOrFallback(double value, double fallback, double minimum)
        {
            if (value > minimum) return value;
            if (fallback > minimum) return fallback;
            return minimum;
        }

        internal static double ComputeAnglePrincipalRadiusOfGyration(
            double depth,
            double width,
            double thickness)
        {
            if (depth <= 0.0 || width <= 0.0 || thickness <= 0.0 ||
                thickness >= Math.Min(depth, width))
            {
                return 0.0;
            }

            // Sharp-corner L-section = vertical leg + horizontal leg - overlap.
            // OpenSTAAD's R input is the radius of gyration about the principal
            // axis, so use the smaller principal centroidal moment: sqrt(Imin/A).
            double verticalArea = thickness * depth;
            double horizontalArea = width * thickness;
            double overlapArea = thickness * thickness;
            double area = verticalArea + horizontalArea - overlapArea;
            if (area <= 0.0)
                return 0.0;

            double verticalX = thickness * 0.5;
            double verticalY = depth * 0.5;
            double horizontalX = width * 0.5;
            double horizontalY = thickness * 0.5;
            double overlapX = thickness * 0.5;
            double overlapY = thickness * 0.5;

            double centroidX = ((verticalArea * verticalX) + (horizontalArea * horizontalX) - (overlapArea * overlapX)) / area;
            double centroidY = ((verticalArea * verticalY) + (horizontalArea * horizontalY) - (overlapArea * overlapY)) / area;

            double ixx =
                (thickness * Math.Pow(depth, 3) / 12.0) + (verticalArea * Math.Pow(verticalY - centroidY, 2))
                + (width * Math.Pow(thickness, 3) / 12.0) + (horizontalArea * Math.Pow(horizontalY - centroidY, 2))
                - ((thickness * Math.Pow(thickness, 3) / 12.0) + (overlapArea * Math.Pow(overlapY - centroidY, 2)));
            double iyy =
                (depth * Math.Pow(thickness, 3) / 12.0) + (verticalArea * Math.Pow(verticalX - centroidX, 2))
                + (thickness * Math.Pow(width, 3) / 12.0) + (horizontalArea * Math.Pow(horizontalX - centroidX, 2))
                - ((thickness * Math.Pow(thickness, 3) / 12.0) + (overlapArea * Math.Pow(overlapX - centroidX, 2)));
            double ixy =
                (verticalArea * (verticalX - centroidX) * (verticalY - centroidY))
                + (horizontalArea * (horizontalX - centroidX) * (horizontalY - centroidY))
                - (overlapArea * (overlapX - centroidX) * (overlapY - centroidY));

            double average = 0.5 * (ixx + iyy);
            double radius = Math.Sqrt(Math.Pow(0.5 * (ixx - iyy), 2) + (ixy * ixy));
            double minimumPrincipalMoment = average - radius;
            if (minimumPrincipalMoment <= 0.0)
                return 0.0;

            return Math.Sqrt(minimumPrincipalMoment / area);
        }

        private static double ComputeRectangularTorsionConstant(double depth, double width)
        {
            if (depth <= 0.0 || width <= 0.0)
                return 0.0;

            double a = Math.Max(depth, width);
            double b = Math.Min(depth, width);
            double ratio = b / a;
            double term = (1.0 / 3.0) - (0.21 * ratio * (1.0 - (Math.Pow(ratio, 4) / 12.0)));
            if (term <= 0.0) term = 1.0 / 12.0;

            return a * Math.Pow(b, 3) * term;
        }

        private static double ComputeThinWalledClosedBoxTorsionConstant(double depth, double width, double thickness)
        {
            if (depth <= 0.0 || width <= 0.0 || thickness <= 0.0)
                return 0.0;

            double meanDepth = depth - thickness;
            double meanWidth = width - thickness;
            if (meanDepth <= 0.0 || meanWidth <= 0.0)
                return 0.0;

            double meanArea = meanDepth * meanWidth;
            double meanPerimeter = 2.0 * (meanDepth + meanWidth);
            if (meanPerimeter <= 0.0)
                return 0.0;

            return (4.0 * meanArea * meanArea * thickness) / meanPerimeter;
        }
    }
}
