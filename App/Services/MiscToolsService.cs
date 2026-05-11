using Microsoft.Office.Interop.Excel;
using StaadSteelApp.Core.Misc;

namespace StaadSteelApp.Core
{
    public static class MiscToolsService
    {
        public static ServiceOperationResult ApplyReleasesSelected(Worksheet ws)
        {
            return MemberReleaseService.ApplyReleasesSelected(ws);
        }

        public static ServiceOperationResult ApplyReleasesSelected(int[] startDof, int[] endDof)
        {
            return MemberReleaseService.ApplyReleasesSelected(startDof, endDof);
        }

        public static ServiceOperationResult ApplyBetaAngleSelected(Worksheet ws)
        {
            return MemberBetaService.ApplyBetaAngleSelected(ws);
        }

        public static ServiceOperationResult ApplyBetaAngleSelected(double angle)
        {
            return MemberBetaService.ApplyBetaAngleSelected(angle);
        }

        public static ServiceOperationResult ApplyNormalMemberSpecSelected()
        {
            return MemberSpecService.ApplyMemberSpecSelected(MiscMemberSpecType.Normal);
        }

        public static ServiceOperationResult ApplyTrussMemberSpecSelected()
        {
            return MemberSpecService.ApplyMemberSpecSelected(MiscMemberSpecType.Truss);
        }

        public static ServiceOperationResult ApplyTensionMemberSpecSelected()
        {
            return MemberSpecService.ApplyMemberSpecSelected(MiscMemberSpecType.Tension);
        }

        public static ServiceOperationResult ApplyCompressionMemberSpecSelected()
        {
            return MemberSpecService.ApplyMemberSpecSelected(MiscMemberSpecType.Compression);
        }

        public static ServiceOperationResult CopySelectedMemberIds(out string clipboardText)
        {
            return MemberIdClipboardService.CopySelectedMemberIds(out clipboardText);
        }

        public static ServiceOperationResult ResetDefaultSelected()
        {
            return MemberReleaseService.ResetDefaultSelected();
        }

        public static ServiceOperationResult ApplyFixedSupportSelected()
        {
            return SupportService.ApplyFixed_Selected();
        }

        public static ServiceOperationResult ApplyPinnedSupportSelected()
        {
            return SupportService.ApplyPinned_Selected();
        }

        public static ServiceOperationResult ApplyFixedButSupportSelected(int[] releaseSpec, double[] springSpec)
        {
            return SupportService.ApplyFixedBut_Selected(releaseSpec, springSpec);
        }

        public static ServiceOperationResult RemoveSupportSelected()
        {
            return SupportService.RemoveSupport_Selected();
        }
    }
}
