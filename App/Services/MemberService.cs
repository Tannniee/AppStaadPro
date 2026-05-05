using Microsoft.Office.Interop.Excel;

namespace StaadSteelApp.Core
{
    public static class MemberService
    {
        public enum MemberSpecType
        {
            Normal,
            Truss,
            Tension,
            Compression
        }

        public static void ApplyReleases_Selected(Worksheet ws)
        {
            MiscToolsService.ApplyReleasesSelected(ws);
        }

        public static void ApplyReleases_Selected(int[] arrStartDOF, int[] arrEndDOF)
        {
            MiscToolsService.ApplyReleasesSelected(arrStartDOF, arrEndDOF);
        }

        public static void ApplyBetaAngle_Selected(Worksheet ws)
        {
            MiscToolsService.ApplyBetaAngleSelected(ws);
        }

        public static void ApplyBetaAngle_Selected(double angle)
        {
            MiscToolsService.ApplyBetaAngleSelected(angle);
        }

        public static void ApplyMemberSpec_Selected(MemberSpecType specType)
        {
            switch (specType)
            {
                case MemberSpecType.Normal:
                    MiscToolsService.ApplyNormalMemberSpecSelected();
                    return;
                case MemberSpecType.Truss:
                    MiscToolsService.ApplyTrussMemberSpecSelected();
                    return;
                case MemberSpecType.Tension:
                    MiscToolsService.ApplyTensionMemberSpecSelected();
                    return;
                case MemberSpecType.Compression:
                    MiscToolsService.ApplyCompressionMemberSpecSelected();
                    return;
                default:
                    return;
            }
        }

        public static void ResetDefault_Selected()
        {
            MiscToolsService.ResetDefaultSelected();
        }
    }
}
