using System;
using System.Diagnostics;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        private static string TryGetMaterialNameFromProperty(object os, int propertyId)
        {
            if (os == null || propertyId <= 0)
                return "";

            try
            {
                object openStaad = os;
                if (!TryGetSectionProperty(openStaad, out object propObj, out string propertyError))
                {
                    Debug.WriteLine("Property object is not available: " + propertyError);
                    return "";
                }

                int count = TryGetAssignedBeamCount(propObj, propertyId);
                if (count <= 0)
                    return "";

                int firstBeamId = 0;
                foreach (int beamId in TryGetAssignedBeamList(propObj, propertyId, count))
                {
                    if (beamId > 0)
                    {
                        firstBeamId = beamId;
                        break;
                    }
                }

                if (firstBeamId <= 0)
                    return "";

                return OpenStaadPropertyAdapter.TryGetBeamMaterialName(openStaad, firstBeamId, out string materialName, out _)
                    ? materialName
                    : "";
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TryGetMaterialNameFromProperty failed for prop {propertyId}: {ex.Message}");
                return "";
            }
        }
    }
}
