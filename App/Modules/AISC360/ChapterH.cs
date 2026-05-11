using System;
using System.Collections.Generic;

namespace StaadSteelApp.AISC360
{
    /// <summary>
    /// Chapter H - Combined Forces (AISC 360-16)
    /// </summary>
    public static class ChapterH
    {
        /// <summary>
        /// Calculate interaction ratio for combined axial and bending (H1)
        /// </summary>
        public static CheckDetail CalculateInteraction(
            AnalyzedMember input,
            CheckDetail axialCheck,
            CheckDetail flexureMajorCheck,
            CheckDetail flexureMinorCheck)
        {
            var result = new CheckDetail
            {
                Name = "Combined Forces",
                Section = "H1.1"
            };

            double Pr = Math.Abs(input.Pr) / 1000.0; // N -> kN
            double Pc = axialCheck?.Capacity ?? double.MaxValue;
            double Mrx = Math.Abs(input.Mrx) / 1e6; // N-mm -> kN-m
            double Mcx = flexureMajorCheck?.Capacity ?? double.MaxValue;
            double Mry = Math.Abs(input.Mry) / 1e6; // N-mm -> kN-m
            double Mcy = flexureMinorCheck?.Capacity ?? double.MaxValue;

            if (Pc <= 0) Pc = double.MaxValue;
            if (Mcx <= 0) Mcx = double.MaxValue;
            if (Mcy <= 0) Mcy = double.MaxValue;

            if (Pr < 0.001 && Mrx < 0.001 && Mry < 0.001)
            {
                result.Status = CheckStatus.NotApplicable;
                return result;
            }

            double pr_pc = Pr / Pc;
            double mrx_mcx = Mrx / Mcx;
            double mry_mcy = Mry / Mcy;

            bool isCompression = input.Pr < -1e-9;
            bool isTension = input.Pr > 1e-9;

            string pc_str = Pc > 1e12 ? "INF" : $"{Pc:F1}";
            string mcx_str = Mcx > 1e12 ? "INF" : $"{Mcx:F2}";
            string mcy_str = Mcy > 1e12 ? "INF" : $"{Mcy:F2}";

            result.Details.Add(new CalculationStep("Axial Ratio Pr/Pc", "",
                $"{Pr:F1}/{pc_str}", pr_pc));
            result.Details.Add(new CalculationStep("Flexure Ratio Mrx/Mcx", "",
                $"{Mrx:F2}/{mcx_str}", mrx_mcx));
            result.Details.Add(new CalculationStep("Flexure Ratio Mry/Mcy", "",
                $"{Mry:F2}/{mcy_str}", mry_mcy));

            double ratio;
            string equation;

            if (isTension)
            {
                // H1-2: Pr/Pt + 8/9(Mrx/Mcx + Mry/Mcy) <= 1.0
                ratio = pr_pc + (8.0 / 9.0) * (mrx_mcx + mry_mcy);
                equation = "H1-2: Pr/Pt + 8/9(Mrx/Mcx + Mry/Mcy)";
                result.Section = "H1-2";

                result.Details.Add(new CalculationStep("Condition", "Axial Tension (Pr > 0)",
                    "", 0) { Result = "Use Eq. H1-2" });
                result.Details.Add(new CalculationStep("Interaction Ratio", equation,
                    $"{pr_pc:F3} + (8/9)*({mrx_mcx:F3} + {mry_mcy:F3})", ratio));
            }
            else if (!isCompression || pr_pc >= 0.2)
            {
                // H1-1a: Pr/Pc + 8/9(Mrx/Mcx + Mry/Mcy) <= 1.0
                ratio = pr_pc + (8.0 / 9.0) * (mrx_mcx + mry_mcy);
                equation = "H1-1a: Pr/Pc + 8/9(Mrx/Mcx + Mry/Mcy)";
                result.Section = "H1-1a";

                result.Details.Add(new CalculationStep("Condition", "Pr/Pc >= 0.2",
                    "", 0) { Result = "Use Eq. H1-1a" });
                result.Details.Add(new CalculationStep("Interaction Ratio", equation,
                    $"{pr_pc:F3} + (8/9)*({mrx_mcx:F3} + {mry_mcy:F3})", ratio));
            }
            else
            {
                // H1-1b: Pr/(2Pc) + (Mrx/Mcx + Mry/Mcy) <= 1.0
                ratio = pr_pc / 2.0 + (mrx_mcx + mry_mcy);
                equation = "H1-1b: Pr/(2Pc) + (Mrx/Mcx + Mry/Mcy)";
                result.Section = "H1-1b";

                result.Details.Add(new CalculationStep("Condition", "Pr/Pc < 0.2",
                    "", 0) { Result = "Use Eq. H1-1b" });
                result.Details.Add(new CalculationStep("Interaction Ratio", equation,
                    $"{pr_pc:F3}/2 + ({mrx_mcx:F3} + {mry_mcy:F3})", ratio));
            }

            result.Demand = ratio;
            result.Capacity = 1.0;
            result.Ratio = ratio;
            result.Status = ratio <= 1.0 ? CheckStatus.Pass : CheckStatus.Fail;

            result.Details.Add(new CalculationStep("Status", "",
                ratio <= 1.0 ? "PASS" : "FAIL", 0) { Result = ratio <= 1.0 ? "<= 1.0" : "> 1.0" });

            return result;
        }

        /// <summary>
        /// Determine if Torsion Interaction H3 is required
        /// </summary>
        public static CheckDetail CalculateTorsionInteraction(
            AnalyzedMember input, 
            CheckDetail axialCheck, 
            CheckDetail flexureMajor, 
            CheckDetail flexureMinor, 
            CheckDetail shearMajor, 
            CheckDetail shearMinor)
        {
            var result = new CheckDetail
            {
                Name = "Torsion Interaction",
                Section = "H3-6",
                Demand = Math.Abs(input.Tr) / 1e6 // N-mm -> kN-m
            };

            // Calculate Torsional Capacity Tn
            double Tn = CalculateTorsionalStrength(input, result);
            double phi = DesignFactors.Phi_Torsion; // usually same as shear 0.9
            double omega = DesignFactors.Omega_Torsion; // 1.67
            double Tr = Math.Abs(input.Tr) / 1e6; // N-mm -> kN-m
            
            double Tc = DesignFactors.GetCapacity(Tn, phi, omega, input.Method);
            result.Capacity = Tc;
            if (double.IsNaN(Tc) || double.IsInfinity(Tc) || Tc <= 1e-9)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Warning", "", "Invalid torsional capacity Tc.", 0));
                return result;
            }

            // Threshold: T <= 20% Tc usually ignored, but let's check strict H3
            bool significantTorsion = (Tr / Tc) > 0.01; // Should check if negligible

            if (!significantTorsion && Tr < 0.1)
            {
                 result.Status = CheckStatus.NotApplicable;
                 return result; // negligible
            }

            // H3: HSS Only
            if (input.IsPipe || input.IsBoxHSS)
            {
                 double SafeRatio(double num, double den)
                 {
                     if (double.IsNaN(den) || double.IsInfinity(den) || den <= 1e-9) return 0.0;
                     return num / den;
                 }

                 // Eq H3-6: (Pr/Pc + Mr/Mc) + (Vr/Vc + Tr/Tc)^2 <= 1.0.
                 // Use direct component ratios rather than the H1 interaction result.

                 double ratio_axial = SafeRatio(axialCheck?.Demand ?? 0.0, axialCheck?.Capacity ?? 0.0);
                 double ratio_flex = SafeRatio(flexureMajor?.Demand ?? 0.0, flexureMajor?.Capacity ?? 0.0) +
                                     SafeRatio(flexureMinor?.Demand ?? 0.0, flexureMinor?.Capacity ?? 0.0);
                 
                 double ratio_PM_interaction = ratio_axial + ratio_flex;

                 // Use the larger shear demand component for the V/T interaction term.
                 
                 double Vr = Math.Max(Math.Abs(input.Vrx), Math.Abs(input.Vry)) / 1000.0; // N -> kN
                 double Vc = Math.Max((shearMajor?.Capacity ?? 0.0), (shearMinor?.Capacity ?? 0.0));
                 if (Vc <= 1e-9)
                 {
                     // No valid shear capacity available -> keep only P-M + torsion term.
                     Vc = double.PositiveInfinity;
                 }
                 
                 double shearRatio = Vr / Vc; 
                 double torsionRatio = Tr / Tc;
                 
                 // H3-6: (Pr/Pc + Mr/Mc) + (Vr/Vc + Tr/Tc)^2 <= 1.0
                 // Note: The terms in first () are effectively H1-1 ratio, BUT H1-1 handles the 8/9 factor. 
                 // Strictly, H3-6 is: (Pr/Pc + Mr/Mc) + ((Vr/Vc)+(Tr/Tc))^2 <= 1.0
                 
                 double interaction = ratio_PM_interaction + Math.Pow(shearRatio + torsionRatio, 2);
                 
                 result.Details.Add(new CalculationStep("Torsional Capacity Tn", "", "", Tn, "kN-m"));
                 result.Details.Add(new CalculationStep("Torsion Ratio Tr/Tc", "", $"{Tr:F2}/{Tc:F2}", torsionRatio));
                 result.Details.Add(new CalculationStep("Shear Ratio Vr/Vc", "", $"{Vr:F2}/{Vc:F2}", shearRatio));
                 result.Details.Add(new CalculationStep("P-M Interaction", "", "", ratio_PM_interaction));
                 
                 result.Ratio = interaction;
                 result.Status = interaction <= 1.0 ? CheckStatus.Pass : CheckStatus.Fail;
                 
                 result.Details.Add(new CalculationStep("Interaction H3-6", "(P/Pc + M/Mc) + (V/Vc + T/Tc)^2",
                     $"{ratio_PM_interaction:F2} + ({shearRatio:F2}+{torsionRatio:F2})^2", result.Ratio));
            }
            else
            {
                // Open Sections - Warning Only
                 result.Details.Add(new CalculationStep("Warning", "", "Torsion check for Open Sections (I/Channel) requires Stress Analysis (H3-1). Not implemented.", 0));
                 result.Status = CheckStatus.NotApplicable; // Or Warning
                 result.Messages.Add("Warning: Significant Torsion detected on Open Section. Check manually (H3-1).");
            }

            return result;
        }

        private static double CalculateTorsionalStrength(AnalyzedMember input, CheckDetail check)
        {
            double E = input.E;
            double Fy = input.Fy;
            double Tn = 0;
            if (E <= 0 || Fy <= 0)
                return 0;

            if (input.IsPipe)
            {
                // H3.1 round HSS torsion. Recalculate Fcr with the same terms used by G5.
                double D = input.D_pipe;
                double t = input.t_pipe; 
                double Lv = input.Length;
                if (D <= 0 || t <= 0 || Lv <= 0)
                    return 0;
                double D_t = D/t;
                double limit1 = 1.60 * E / Fy;
                double Fcr;
                
                // Simplified G5 calc reuse
                 double limit2Sqrt = Math.Sqrt(E / Fy);
                 double Lv_D = Lv / D;
                 if (D_t <= limit1) {
                    double Fcr_a = 1.60 * E / (limit2Sqrt * Math.Pow(Lv_D, 0.5) * Math.Pow(D_t, 1.25));
                    double Fcr_b = 0.78 * E / Math.Pow(D_t, 1.5);
                    Fcr = Math.Max(Fcr_a, Fcr_b);
                    Fcr = Math.Min(Fcr, 0.6 * Fy); 
                 } else {
                    Fcr = 0.78 * E / Math.Pow(D_t, 1.5);
                    Fcr = Math.Min(Fcr, 0.6 * Fy);
                 }
                 
                 double C = (Math.PI * Math.Pow(D - t, 2) * t) / 2.0; // Standard C for thin walled tube
                 if (C <= 0) return 0;
                 Tn = Fcr * C / 1e6; // kN-m
            }
            else if (input.IsBoxHSS)
            {
                // H3.2 Rectangular HSS: Tn = Fcr * C
                // C = 2(B-t)(H-t)t - 4.5t^3 ? (Conservative: 2(B-t)(H-t)t)
                // Fcr = 0.6Fy (Yielding) or Buckling (G4)
                
                double H = input.Ht;
                double B = input.B;
                double t = input.tdes;
                if (t <= 0) t = input.tw > 0 ? input.tw : input.tf;
                if (H <= 0 || B <= 0 || t <= 0)
                    return 0;
                double h_t = Math.Max( (H-3*t)/t, (B-3*t)/t ); // Max slenderness
                
                // Fcr from G4 (using h_t)
                double kv = 5.0; 
                double Cv2;
                double limit1 = 1.10 * Math.Sqrt(kv * E / Fy);
                double limit2 = 1.37 * Math.Sqrt(kv * E / Fy);
                
                if (h_t <= limit1) Cv2 = 1.0;
                else if (h_t <= limit2) Cv2 = 1.10 * Math.Sqrt(kv * E / Fy) / h_t;
                 else Cv2 = 1.51 * kv * E / (h_t * h_t * Fy);
                 
                 double Fcr = 0.6 * Fy * Cv2;
                 double C = 2 * (B - t) * (H - t) * t; // Approx C
                 if (C <= 0) return 0;
                 Tn = Fcr * C / 1e6;
            }
            
            return Tn;
        }

        public static bool IsInteractionRequired(AnalyzedMember input)
        {
            double prKn = Math.Abs(input.Pr) / 1000.0;
            double mrxKnM = Math.Abs(input.Mrx) / 1e6;
            double mryKnM = Math.Abs(input.Mry) / 1e6;
            return prKn > 0.001 && (mrxKnM > 0.001 || mryKnM > 0.001);
        }
    }
}
