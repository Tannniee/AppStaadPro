using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core.Check
{
    internal sealed class CheckTaperedSectionInfo
    {
        internal double H1 { get; set; }
        internal double H2 { get; set; }
        internal double Tw { get; set; }
        internal double Bf { get; set; }
        internal double Tf { get; set; }
        internal double Bfb { get; set; }
        internal double Tfb { get; set; }
    }

    internal static class CheckMemberInfoService
    {
        internal static bool TryGetBeamSectionName(object openStaad, int memberNo, out string sectionName, out string error)
        {
            return OpenStaadPropertyAdapter.TryGetBeamSectionName(openStaad, memberNo, out sectionName, out error);
        }

        internal static bool TryGetBeamMaterialName(object openStaad, int memberNo, out string materialName, out string error)
        {
            return OpenStaadPropertyAdapter.TryGetBeamMaterialName(openStaad, memberNo, out materialName, out error);
        }

        internal static bool TryGetTaperedSectionData(object openStaad, int memberNo, out CheckTaperedSectionInfo data, out string error)
        {
            data = null;
            if (!OpenStaadPropertyAdapter.TryGetTaperedSectionDataForMember(openStaad, memberNo, out OpenStaadTaperedSectionData tapered, out error))
                return false;

            data = new CheckTaperedSectionInfo
            {
                H1 = tapered.H1,
                H2 = tapered.H2,
                Tw = tapered.Tw,
                Bf = tapered.Bf,
                Tf = tapered.Tf,
                Bfb = tapered.Bfb,
                Tfb = tapered.Tfb
            };
            return true;
        }
    }
}
