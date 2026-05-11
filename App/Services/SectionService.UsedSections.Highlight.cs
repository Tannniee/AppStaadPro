using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        public static bool HighlightUsedSection(object os, int propertyId, out string error)
        {
            return HighlightUsedSections(os, new[] { propertyId }, out error);
        }

        public static bool HighlightBeamIds(object os, IEnumerable<int> beamIds, out string error)
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

            object openStaad = os;
            if (!TryGetSectionGeometry(openStaad, out object geometryObj, out string geometryError))
            {
                Debug.WriteLine("Geometry object is not available: " + geometryError);
                error = "Selected section was found, but STAAD selection could not be updated.";
                return false;
            }

            if (OpenStaadGeometryAdapter.TryHighlightBeams(geometryObj, validBeamIds, out string highlightError))
                return true;

            if (!string.IsNullOrWhiteSpace(highlightError))
                Debug.WriteLine("Highlight used section beams failed: " + highlightError);

            error = "Selected section was found, but STAAD selection could not be updated.";
            return false;
        }

        public static bool HighlightUsedSections(object os, IEnumerable<int> propertyIds, out string error)
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

            object openStaad = os;
            if (!TryGetSectionProperty(openStaad, out object propObj, out string propertyError))
            {
                Debug.WriteLine("Property object is not available: " + propertyError);
                error = "Could not read assigned beam list for selected section.";
                return false;
            }

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

        private static bool TryGetCachedBeamIds(object os, IEnumerable<int> propertyIds, out int[] beamIds)
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
    }
}
