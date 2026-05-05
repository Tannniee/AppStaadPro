using System;
using System.Collections.Generic;

namespace StaadSteelApp.AISC360
{
    /// <summary>
    /// Chapter L - Serviceability Design (AISC 360-16)
    /// </summary>
    public static class ChapterL
    {
        public static CheckDetail CheckDeflection(AnalyzedMember input)
        {
            var result = new CheckDetail
            {
                Name = "Serviceability (Deflection)",
                Section = "L2",
                Demand = input.MaxDeflection
            };

            if (input.Length <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid member length for deflection check", "", "", 0));
                return result;
            }

            // L/360 limit (standard typically used, though user dependent)
            double limitL = 360.0; 
            double capacity = input.Length / limitL;

            result.Capacity = capacity;
            result.Status = result.Demand <= capacity ? CheckStatus.Pass : CheckStatus.Fail;

            result.Details.Add(new CalculationStep("Max Deflection", "", "", result.Demand, "mm"));
            result.Details.Add(new CalculationStep("Limit L/360", "L/360", 
                $"{input.Length:F0}/360", capacity, "mm"));
            
            result.CalculateRatio();
            
            result.Details.Add(new CalculationStep("Ratio", "Δ/Limit", 
                $"{result.Demand:F2}/{capacity:F2}", result.Ratio));

            return result;
        }
    }
}
