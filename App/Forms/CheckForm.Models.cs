namespace StaadSteelApp
{
    public partial class CheckForm
    {
        private class DesignResultInfo
        {
            public string DesignCode { get; set; }
            public string DesignStatus { get; set; }
            public double CriticalRatio { get; set; }
            public double AllowableRatio { get; set; }
            public int CriticalLoadCase { get; set; }
            public double CriticalSection { get; set; }
            public string CriticalClause { get; set; }
            public string DesignSection { get; set; }
            public double[] DesignForces { get; set; }
            public double KLByR { get; set; }
        }

        private class ForceEnvelopeEntry
        {
            public string Label { get; set; }
            public double Value { get; set; }
            public int LoadCase { get; set; }
            public string Station { get; set; }
            public string Unit { get; set; }
            public bool IsSampled { get; set; }
        }
    }
}
