using Microsoft.Office.Interop.Excel;
using StaadSteelApp.Core.Misc;

namespace StaadSteelApp.Core
{
    public static class MiscToolsService
    {
        public static void ApplyReleasesSelected(Worksheet ws)
        {
            MemberReleaseService.ApplyReleasesSelected(ws);
        }

        public static void ApplyReleasesSelected(int[] startDof, int[] endDof)
        {
            MemberReleaseService.ApplyReleasesSelected(startDof, endDof);
        }

        public static void ApplyBetaAngleSelected(Worksheet ws)
        {
            MemberBetaService.ApplyBetaAngleSelected(ws);
        }

        public static void ApplyBetaAngleSelected(double angle)
        {
            MemberBetaService.ApplyBetaAngleSelected(angle);
        }

        public static void ApplyNormalMemberSpecSelected()
        {
            MemberSpecService.ApplyMemberSpecSelected(MiscMemberSpecType.Normal);
        }

        public static void ApplyTrussMemberSpecSelected()
        {
            MemberSpecService.ApplyMemberSpecSelected(MiscMemberSpecType.Truss);
        }

        public static void ApplyTensionMemberSpecSelected()
        {
            MemberSpecService.ApplyMemberSpecSelected(MiscMemberSpecType.Tension);
        }

        public static void ApplyCompressionMemberSpecSelected()
        {
            MemberSpecService.ApplyMemberSpecSelected(MiscMemberSpecType.Compression);
        }

        public static void CopySelectedMemberIds()
        {
            MemberIdClipboardService.CopySelectedMemberIds();
        }

        public static void ResetDefaultSelected()
        {
            MemberReleaseService.ResetDefaultSelected();
        }

        public static void ApplyFixedSupportSelected()
        {
            SupportService.ApplyFixed_Selected();
        }

        public static void ApplyPinnedSupportSelected()
        {
            SupportService.ApplyPinned_Selected();
        }

        public static void ApplyFixedButSupportSelected(int[] releaseSpec, double[] springSpec)
        {
            SupportService.ApplyFixedBut_Selected(releaseSpec, springSpec);
        }

        public static void RemoveSupportSelected()
        {
            SupportService.RemoveSupport_Selected();
        }
    }
}
