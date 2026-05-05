namespace StaadSteelApp.Core.Interop
{
    internal static class OpenStaadAnalysisAdapter
    {
        internal static bool TryAnalyze(object openStaad, bool silent, bool hidden, bool wait, out string error)
        {
            error = string.Empty;
            return ComInvoke.TryInvoke(openStaad, "AnalyzeEx", out _, out error, new object[] { silent, hidden, wait });
        }
    }
}
