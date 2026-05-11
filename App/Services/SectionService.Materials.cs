using System;
using System.Collections.Generic;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        public static List<string> GetIsotropicMaterialNames(object os)
        {
            var materials = new List<string>();
            if (os == null) return materials;

            object openStaad = os;
            if (!TryGetSectionProperty(openStaad, out object propObj, out string propertyError))
            {
                WarnOnce("IsotropicMaterialPropertyObject", "Could not read STAAD property object. Material dropdown may be incomplete. " + propertyError);
                return materials;
            }

            if (!OpenStaadPropertyAdapter.TryGetIsotropicMaterialCount(propObj, out int count, out string countError))
            {
                WarnOnce("IsotropicMaterialCount", "Could not read isotropic material count from STAAD. Material dropdown may be incomplete. " + countError);
                return materials;
            }

            if (count <= 0) return materials;

            var uniqueNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void ReadMaterials(int startInclusive, int endExclusive)
            {
                for (int i = startInclusive; i < endExclusive; i++)
                {
                    if (OpenStaadPropertyAdapter.TryGetIsotropicMaterialName(propObj, i, out string name, out string readError))
                    {
                        if (!string.IsNullOrWhiteSpace(name) && uniqueNames.Add(name))
                            materials.Add(name);
                    }
                    else
                    {
                        if (materials.Count == 0 && i <= startInclusive)
                            WarnOnce("IsotropicMaterials:Read", "Could not read isotropic material properties from STAAD. Material dropdown may be incomplete. " + readError);
                    }
                }
            }

            // OpenSTAAD doc: varMatNo is zero-based index.
            ReadMaterials(0, count);

            // Compatibility fallback for versions that unexpectedly behave as one-based.
            if (materials.Count == 0)
            {
                ReadMaterials(1, count + 1);
                if (materials.Count == 0)
                    WarnOnce("IsotropicMaterials:Empty", "STAAD reported isotropic materials, but none could be read. Material dropdown may be incomplete.");
            }

            return materials;
        }
    }
}
