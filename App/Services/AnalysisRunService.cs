using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    internal sealed class AnalysisRunResult
    {
        private AnalysisRunResult(bool succeeded, string message)
        {
            Succeeded = succeeded;
            Message = message ?? string.Empty;
        }

        internal bool Succeeded { get; }
        internal string Message { get; }

        internal static AnalysisRunResult Success()
        {
            return new AnalysisRunResult(true, "Analysis Execution Finished.");
        }

        internal static AnalysisRunResult Failure(string error)
        {
            return new AnalysisRunResult(false, "Failed to execute AnalyzeEx: " + (error ?? string.Empty));
        }
    }

    internal static class AnalysisRunService
    {
        internal static AnalysisRunResult RunAnalysis(object openStaad)
        {
            if (openStaad == null)
                return AnalysisRunResult.Failure("OpenSTAAD object is not available.");

            return OpenStaadAnalysisAdapter.TryAnalyze(openStaad, false, false, true, out string analyzeError)
                ? AnalysisRunResult.Success()
                : AnalysisRunResult.Failure(analyzeError);
        }
    }
}
