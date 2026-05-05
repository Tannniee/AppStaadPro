using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        private static readonly object _usedSectionsCacheSync = new object();
        private static string _usedSectionsCacheModelKey = "";
        private static Dictionary<int, UsedSectionEntry> _usedSectionsPropertyCache = new Dictionary<int, UsedSectionEntry>();
        private static bool _usedSectionsCacheInitialized;

        public static List<UsedSectionEntry> GetUsedSections(dynamic os)
        {
            if (os == null)
                return new List<UsedSectionEntry>();

            lock (_usedSectionsCacheSync)
            {
                EnsureUsedSectionsCache(os);
                return BuildUsedSectionsListFromCache();
            }
        }

        public static void UpdateUsedSectionsCacheAfterAssignment(dynamic os, IEnumerable<int> beamIds, int targetPropertyId)
        {
            if (os == null || targetPropertyId <= 0)
                return;

            int[] assignedBeamIds = (beamIds ?? Enumerable.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            if (assignedBeamIds.Length == 0)
                return;

            lock (_usedSectionsCacheSync)
            {
                string modelKey = GetUsedSectionsModelKey(os);
                if (!_usedSectionsCacheInitialized || !string.Equals(_usedSectionsCacheModelKey, modelKey, StringComparison.OrdinalIgnoreCase))
                    return;

                var removeIds = new List<int>();
                foreach (var kvp in _usedSectionsPropertyCache)
                {
                    int[] remaining = (kvp.Value.BeamIds ?? Array.Empty<int>())
                        .Where(id => !assignedBeamIds.Contains(id))
                        .Distinct()
                        .OrderBy(id => id)
                        .ToArray();

                    if (remaining.Length == 0)
                    {
                        removeIds.Add(kvp.Key);
                    }
                    else
                    {
                        kvp.Value.BeamIds = remaining;
                        kvp.Value.BeamCount = remaining.Length;
                    }
                }

                foreach (int propertyId in removeIds)
                    _usedSectionsPropertyCache.Remove(propertyId);

                if (!_usedSectionsPropertyCache.TryGetValue(targetPropertyId, out UsedSectionEntry targetEntry))
                {
                    targetEntry = BuildUsedSectionsCacheEntry(os.Property, targetPropertyId, assignedBeamIds);
                    if (targetEntry == null)
                        return;

                    _usedSectionsPropertyCache[targetPropertyId] = targetEntry;
                }
                else
                {
                    targetEntry.BeamIds = (targetEntry.BeamIds ?? Array.Empty<int>())
                        .Concat(assignedBeamIds)
                        .Where(id => id > 0)
                        .Distinct()
                        .OrderBy(id => id)
                        .ToArray();
                    targetEntry.BeamCount = targetEntry.BeamIds.Length;
                }
            }
        }

        private static void EnsureUsedSectionsCache(dynamic os)
        {
            string modelKey = GetUsedSectionsModelKey(os);
            if (_usedSectionsCacheInitialized &&
                string.Equals(_usedSectionsCacheModelKey, modelKey, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            RebuildUsedSectionsCache(os, modelKey);
        }

        private static void RebuildUsedSectionsCache(dynamic os, string modelKey)
        {
            var cache = new Dictionary<int, UsedSectionEntry>();
            if (os == null)
            {
                _usedSectionsPropertyCache = cache;
                _usedSectionsCacheModelKey = modelKey ?? "";
                _usedSectionsCacheInitialized = true;
                return;
            }

            object propObj = os.Property;
            int count = 0;

            if (!OpenStaadPropertyAdapter.TryGetSectionPropertyCount(propObj, out count, out string countError))
                Debug.WriteLine("GetSectionPropertyCount failed: " + countError);

            if (count > 0)
            {
                int[] propIds = GetSectionPropertyIds(propObj, count);
                foreach (int propId in propIds)
                {
                    UsedSectionEntry entry = BuildUsedSectionsCacheEntry(propObj, propId);
                    if (entry != null)
                        cache[propId] = entry;
                }
            }

            _usedSectionsPropertyCache = cache;
            _usedSectionsCacheModelKey = modelKey ?? "";
            _usedSectionsCacheInitialized = true;
        }

        private static UsedSectionEntry BuildUsedSectionsCacheEntry(object propObj, int propId, int[] beamIdsOverride = null)
        {
            if (propObj == null || propId <= 0)
                return null;

            int[] beamIds = beamIdsOverride ?? Array.Empty<int>();
            if (beamIds.Length == 0)
            {
                int beamCount = TryGetAssignedBeamCount(propObj, propId);
                if (beamCount <= 0)
                    return null;

                beamIds = TryGetAssignedBeamList(propObj, propId, beamCount);
                if (beamIds.Length == 0)
                    return null;
            }
            else
            {
                beamIds = beamIds
                    .Where(x => x > 0)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToArray();
                if (beamIds.Length == 0)
                    return null;
            }

            string rawName = "";
            TryGetSectionPropertyNameById(propObj, propId, out rawName);

            int typeCode = TryGetSectionPropertyTypeCode(propObj, propId);
            ResolveSortMetadata(typeCode, out int sortGroup, out int sortType, out string sourceLabel);
            string displayName = BuildUsedSectionDisplayName(propObj, propId, typeCode, rawName);

            return new UsedSectionEntry
            {
                PropertyId = propId,
                PropertyIds = new[] { propId },
                BeamIds = beamIds,
                Name = string.IsNullOrWhiteSpace(rawName) ? ("PROP-" + propId) : rawName.Trim(),
                DisplayName = string.IsNullOrWhiteSpace(displayName) ? rawName : displayName,
                SourceLabel = sourceLabel,
                SortGroup = sortGroup,
                SortType = sortType,
                BeamCount = beamIds.Length
            };
        }

        private static List<UsedSectionEntry> BuildUsedSectionsListFromCache()
        {
            return _usedSectionsPropertyCache.Values
                .Select(CloneUsedSectionEntry)
                .GroupBy(
                    x => string.IsNullOrWhiteSpace(x.SelectionKey) ? ("PROP-" + x.PropertyId) : x.SelectionKey,
                    StringComparer.OrdinalIgnoreCase)
                .Select(group =>
                {
                    UsedSectionEntry first = group
                        .OrderBy(x => x.SortGroup)
                        .ThenBy(x => x.SortType)
                        .ThenBy(x => x.PropertyId)
                        .First();

                    return new UsedSectionEntry
                    {
                        PropertyId = group.Min(x => x.PropertyId),
                        PropertyIds = group.SelectMany(x => x.PropertyIds ?? new[] { x.PropertyId }).Distinct().OrderBy(x => x).ToArray(),
                        BeamIds = group.SelectMany(x => x.BeamIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().OrderBy(x => x).ToArray(),
                        Name = first.Name,
                        DisplayName = first.DisplayName,
                        SourceLabel = first.SourceLabel,
                        SortGroup = first.SortGroup,
                        SortType = first.SortType,
                        BeamCount = group.SelectMany(x => x.BeamIds ?? Array.Empty<int>()).Where(x => x > 0).Distinct().Count()
                    };
                })
                .OrderBy(x => x.SortGroup)
                .ThenBy(x => x.SortType)
                .ThenBy(x => string.IsNullOrWhiteSpace(x.DisplayName) ? x.Name : x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(x => x.PropertyId)
                .ToList();
        }

        private static UsedSectionEntry CloneUsedSectionEntry(UsedSectionEntry source)
        {
            if (source == null)
                return null;

            return new UsedSectionEntry
            {
                PropertyId = source.PropertyId,
                PropertyIds = (source.PropertyIds ?? Array.Empty<int>()).ToArray(),
                BeamIds = (source.BeamIds ?? Array.Empty<int>()).ToArray(),
                Name = source.Name,
                DisplayName = source.DisplayName,
                SourceLabel = source.SourceLabel,
                SortGroup = source.SortGroup,
                SortType = source.SortType,
                BeamCount = source.BeamCount
            };
        }

        private static bool TryGetCachedBeamIds(dynamic os, IEnumerable<int> propertyIds, out int[] beamIds)
        {
            beamIds = Array.Empty<int>();
            if (os == null)
                return false;

            int[] validPropertyIds = (propertyIds ?? Enumerable.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            if (validPropertyIds.Length == 0)
                return false;

            lock (_usedSectionsCacheSync)
            {
                EnsureUsedSectionsCache(os);

                var beamSet = new HashSet<int>();
                foreach (int propertyId in validPropertyIds)
                {
                    if (!_usedSectionsPropertyCache.TryGetValue(propertyId, out UsedSectionEntry entry))
                        return false;

                    foreach (int beamId in entry.BeamIds ?? Array.Empty<int>())
                    {
                        if (beamId > 0)
                            beamSet.Add(beamId);
                    }
                }

                beamIds = beamSet.OrderBy(x => x).ToArray();
                return beamIds.Length > 0;
            }
        }

        private static string GetUsedSectionsModelKey(dynamic os)
        {
            string staadFilePath = "";
            if (os != null && TryGetStaadFilePath(os, out staadFilePath) && !string.IsNullOrWhiteSpace(staadFilePath))
                return staadFilePath.Trim();

            return "session:" + RuntimeHelpers.GetHashCode((object)os);
        }

        private static bool TryGetStaadFilePath(dynamic os, out string staadFilePath)
        {
            staadFilePath = "";
            if (os == null)
                return false;

            foreach (object fullPathFlag in new object[] { "TRUE", true, 1 })
            {
                if (OpenStaadModelAdapter.TryGetStaadFile((object)os, fullPathFlag, out string path, out string error))
                {
                    staadFilePath = path;
                    return true;
                }
                else if (!string.IsNullOrWhiteSpace(error))
                {
                    Debug.WriteLine("GetSTAADFile failed: " + error);
                }
            }

            return false;
        }

        public static bool HighlightUsedSection(dynamic os, int propertyId, out string error)
        {
            return HighlightUsedSections(os, new[] { propertyId }, out error);
        }

        public static bool HighlightBeamIds(dynamic os, IEnumerable<int> beamIds, out string error)
        {
            error = "";
            if (os == null)
            {
                error = "Not connected to STAAD.";
                return false;
            }

            int[] validBeamIds = (beamIds ?? Enumerable.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .OrderBy(x => x)
                .ToArray();

            if (validBeamIds.Length == 0)
            {
                error = "Could not read assigned beam list for selected section.";
                return false;
            }

            object geometryObj = os.Geometry;
            if (OpenStaadGeometryAdapter.TryHighlightBeams(geometryObj, validBeamIds, out string highlightError))
                return true;

            if (!string.IsNullOrWhiteSpace(highlightError))
                Debug.WriteLine("Highlight used section beams failed: " + highlightError);

            error = "Selected section was found, but STAAD selection could not be updated.";
            return false;
        }

        public static bool HighlightUsedSections(dynamic os, IEnumerable<int> propertyIds, out string error)
        {
            error = "";
            if (os == null)
            {
                error = "Not connected to STAAD.";
                return false;
            }

            int[] validPropertyIds = (propertyIds ?? Enumerable.Empty<int>())
                .Where(x => x > 0)
                .Distinct()
                .ToArray();

            if (validPropertyIds.Length == 0)
            {
                error = "Invalid property id.";
                return false;
            }

            if (TryGetCachedBeamIds(os, validPropertyIds, out int[] cachedBeamIds))
                return HighlightBeamIds(os, cachedBeamIds, out error);

            object propObj = os.Property;
            var beamSet = new HashSet<int>();
            foreach (int propertyId in validPropertyIds)
            {
                int count = TryGetAssignedBeamCount(propObj, propertyId);
                if (count <= 0)
                    continue;

                int[] ids = TryGetAssignedBeamList(propObj, propertyId, count);
                foreach (int beamId in ids)
                {
                    if (beamId > 0)
                        beamSet.Add(beamId);
                }
            }

            int[] beamIds = beamSet.OrderBy(x => x).ToArray();
            return HighlightBeamIds(os, beamIds, out error);
        }

        private static int[] GetSectionPropertyIds(object propObj, int count)
        {
            if (propObj == null || count <= 0)
                return Array.Empty<int>();

            if (OpenStaadPropertyAdapter.TryGetSectionPropertyList(propObj, out int[] propertyIds, out string error))
                return propertyIds;

            Debug.WriteLine("GetSectionPropertyList failed: " + error);

            return Array.Empty<int>();
        }

        private static int TryGetAssignedBeamCount(object propObj, int propertyId)
        {
            if (propObj == null || propertyId <= 0)
                return 0;

            if (OpenStaadPropertyAdapter.TryGetSectionPropertyAssignedBeamCount(propObj, propertyId, out int count, out string error))
                return count;

            Debug.WriteLine($"GetSectionPropertyAssignedBeamCount failed for {propertyId}: {error}");

            return 0;
        }

        private static int[] TryGetAssignedBeamList(object propObj, int propertyId, int count)
        {
            if (propObj == null || propertyId <= 0 || count <= 0)
                return Array.Empty<int>();

            if (OpenStaadPropertyAdapter.TryGetSectionPropertyAssignedBeamList(propObj, propertyId, count, out int[] beamIds, out string error))
                return beamIds;

            Debug.WriteLine($"GetSectionPropertyAssignedBeamList failed for {propertyId}: {error}");

            return Array.Empty<int>();
        }

        private static int TryGetSectionPropertyTypeCode(object propObj, int propertyId)
        {
            if (propObj == null || propertyId <= 0)
                return 0;

            if (OpenStaadPropertyAdapter.TryGetSectionPropertyType(propObj, propertyId, out int typeCode, out string error))
                return typeCode;

            Debug.WriteLine($"GetSectionPropertyType failed for {propertyId}: {error}");

            return 0;
        }

        private static string BuildUsedSectionDisplayName(object propObj, int propertyId, int typeCode, string rawName)
        {
            if (propObj == null || propertyId <= 0)
                return rawName ?? "";

            ReadSectionShapeData(propObj, propertyId, out int actualTypeCode, out double[] props, out double width, out double depth, out double tf, out double tw);
            if (actualTypeCode > 0)
                typeCode = actualTypeCode;

            double h1mm;
            double h2mm;
            double twmm;
            double bfmm;
            double tfmm;
            double bfbmm;
            double tfbmm;

            switch (typeCode)
            {
                case 680:
                    h1mm = GetAt(props, 0) * 1000.0;
                    h2mm = GetAt(props, 2) * 1000.0;
                    twmm = GetAt(props, 1) * 1000.0;
                    bfmm = GetAt(props, 3) * 1000.0;
                    tfmm = GetAt(props, 4) * 1000.0;
                    bfbmm = GetAt(props, 5, GetAt(props, 3)) * 1000.0;
                    tfbmm = GetAt(props, 6, GetAt(props, 4)) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.I, h1mm, h2mm, twmm, bfmm, tfmm, bfbmm, tfbmm, 0),
                        rawName);

                case 610:
                case 611:
                case 612:
                case 616:
                case 670:
                case 698:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    bfmm = GetAt(props, 2, width) * 1000.0;
                    twmm = GetAt(props, 4, tw) * 1000.0;
                    tfmm = GetAt(props, 3, tf) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.I, h1mm, h1mm, twmm, bfmm, tfmm, bfmm, tfmm, 0),
                        rawName);

                case 620:
                case 673:
                case 694:
                case 61100:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    bfmm = GetAt(props, 2, width) * 1000.0;
                    twmm = GetAt(props, 4, tw) * 1000.0;
                    tfmm = GetAt(props, 3, tf) * 1000.0;
                    return FirstNonEmpty(
                        "T-" + SectionNamingService.FmtNum(h1mm) + "x" + SectionNamingService.FmtNum(bfmm) + "x" + SectionNamingService.FmtNum(twmm) + "x" + SectionNamingService.FmtNum(tfmm),
                        rawName);

                case 630:
                case 631:
                case 691:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    bfmm = GetAt(props, 2, width) * 1000.0;
                    twmm = GetAt(props, 4, tw) * 1000.0;
                    tfmm = GetAt(props, 3, tf) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Channel, h1mm, 0, twmm, bfmm, tfmm, 0, 0, 0),
                        rawName);

                case 640:
                case 692:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    bfmm = GetAt(props, 2, width) * 1000.0;
                    tfmm = GetAt(props, 3, tf > 0 ? tf : tw) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Angle, h1mm, 0, 0, bfmm, tfmm, 0, 0, 0),
                        rawName);

                case 693:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    bfmm = GetAt(props, 2, width) * 1000.0;
                    tfmm = GetAt(props, 3, tf > 0 ? tf : tw) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.DblAngle, h1mm, 0, 0, bfmm, tfmm, 0, 0, 0),
                        rawName);

                case 650:
                case 654:
                case 675:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    h2mm = GetAt(props, 2, width) * 1000.0;
                    tfmm = GetAt(props, 3, tf > 0 ? tf : tw) * 1000.0;
                    twmm = GetAt(props, 4, tw > 0 ? tw : tf) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Rectangular, h1mm, h2mm, twmm, 0, tfmm, 0, 0, 0),
                        rawName);

                case 696:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    h2mm = GetAt(props, 2, width) * 1000.0;
                    tfmm = GetAt(props, 3, tf > 0 ? tf : tw) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Rectangular, h1mm, h2mm, tfmm, 0, tfmm, 0, 0, 0),
                        rawName);

                case 672:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    h2mm = GetAt(props, 2, width) * 1000.0;
                    if (h1mm > 0 && h2mm > 0)
                        return "Rect-" + SectionNamingService.FmtNum(h1mm) + "x" + SectionNamingService.FmtNum(h2mm);
                    return rawName ?? "";

                case 655:
                case 660:
                case 661:
                    h1mm = GetAt(props, 1, depth > 0 ? depth : width) * 1000.0;
                    double tmm = GetAt(props, 2, tw > 0 ? tw : tf) * 1000.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Pipe, h1mm, 0, tmm, 0, 0, 0, 0, 0),
                        rawName);

                case 695:
                    h1mm = GetAt(props, 1, depth > 0 ? depth : width) * 1000.0;
                    double idmm = GetAt(props, 2, tw > 0 ? tw : tf) * 1000.0;
                    double uptPipeThickness = (h1mm - idmm) / 2.0;
                    return FirstNonEmpty(
                        SectionNamingService.GenerateSectionName(eFramePropType.Pipe, h1mm, 0, uptPipeThickness, 0, 0, 0, 0, 0),
                        rawName);

                case 671:
                    h1mm = GetAt(props, 1, depth) * 1000.0;
                    if (h1mm > 0)
                        return "Circle-" + SectionNamingService.FmtNum(h1mm);
                    return rawName ?? "";

                default:
                    return rawName ?? "";
            }
        }

        private static void ReadSectionShapeData(object propObj, int propertyId, out int typeCode, out double[] props, out double width, out double depth, out double tf, out double tw)
        {
            typeCode = 0;
            props = new double[25];
            width = 0;
            depth = 0;
            tf = 0;
            tw = 0;

            if (propObj == null || propertyId <= 0)
                return;

            if (!OpenStaadPropertyAdapter.TryGetSectionPropertyValuesExFromProperty(propObj, propertyId, out typeCode, out props, out string exError))
                Debug.WriteLine($"GetSectionPropertyValuesEx failed for property {propertyId}: {exError}");

            if (OpenStaadPropertyAdapter.TryGetSectionPropertyValuesFromProperty(propObj, propertyId, out double[] values, out string valuesError))
            {
                width = GetAt(values, 0);
                depth = GetAt(values, 1);
                tf = GetAt(values, 8);
                tw = GetAt(values, 9);
            }
            else
            {
                Debug.WriteLine($"GetSectionPropertyValues failed for property {propertyId}: {valuesError}");
            }
        }

        private static string FirstNonEmpty(string primary, string fallback)
        {
            if (!string.IsNullOrWhiteSpace(primary))
                return primary;

            return fallback ?? "";
        }

        private static double ToDouble(object value)
        {
            try
            {
                return Convert.ToDouble(value);
            }
            catch
            {
                return 0.0;
            }
        }

        private static double GetAt(double[] values, int index, double fallback = 0)
        {
            if (values != null && index >= 0 && index < values.Length)
            {
                double value = values[index];
                if (Math.Abs(value) > 0.0000001)
                    return value;
            }

            return fallback;
        }

        private static void ResolveSortMetadata(int typeCode, out int sortGroup, out int sortType, out string sourceLabel)
        {
            sortGroup = 3;
            sortType = 9;
            sourceLabel = "Other";

            if (typeCode == 680)
            {
                sortGroup = 0;
                sortType = 0;
                sourceLabel = "Tapered I";
                return;
            }

            if (typeCode >= 670 && typeCode <= 679)
            {
                sortGroup = 0;
                sortType = 1;
                sourceLabel = "Prismatic";
                return;
            }

            if (typeCode >= 610 && typeCode <= 669)
            {
                sortGroup = 1;
                sortType = 0;
                sourceLabel = "Legacy";
                return;
            }

            if (typeCode >= 690 && typeCode <= 699)
            {
                sortGroup = 2;
                sortType = 0;
                sourceLabel = "UPT";
            }
        }
    }
}
