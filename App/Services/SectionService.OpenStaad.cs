using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public static partial class SectionService
    {
        private static bool TryGetSectionOpenStaad(out object openStaad, out string error)
        {
            return StaadManager.TryGetOpenSTAAD(out openStaad, out error);
        }

        private static bool TryGetSectionGeometry(object openStaad, out object geometryObj, out string error)
        {
            return OpenStaadAdapterHelpers.TryGetSubObject(openStaad, "Geometry", out geometryObj, out error);
        }

        private static bool TryGetSectionProperty(object openStaad, out object propObj, out string error)
        {
            return OpenStaadPropertyAdapter.TryGetPropertyObject(openStaad, out propObj, out error);
        }
    }
}
