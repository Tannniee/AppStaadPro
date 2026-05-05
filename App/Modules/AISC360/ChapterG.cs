using System;
using System.Collections.Generic;

namespace StaadSteelApp.AISC360
{
    /// <summary>
    /// Chapter G - Shear (AISC 360-16)
    /// </summary>
    public static class ChapterG
    {
        /// <summary>
        /// Calculate shear strength for I-shapes (G2)
        /// </summary>
        public static CheckDetail CalculateShearMajor(AnalyzedMember input)
        {
            var result = new CheckDetail
            {
                Name = "Shear (Major Axis)",
                Section = "G2",
                Demand = Math.Abs(input.Vrx) / 1000.0 // N -> kN
            };

            if (result.Demand < 0.001)
            {
                result.Status = CheckStatus.NotApplicable;
                return result;
            }

            double E = input.E;
            double Fy = input.Fy;
            double d = input.d;
            double tw = input.tw;
            double h = input.hc;
            if (h <= 0 && d > 0) h = d;

            if (E <= 0 || Fy <= 0 || tw <= 0 || (input.IsBuiltUp ? h <= 0 : d <= 0))
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid section data for shear check", "", "", 0));
                return result;
            }

            // Web area
            // For Rolled shapes: Aw = d * tw (G2.1b)
            // For Built-up/Welded: Aw = h * tw (G2.1b) where h is clear distance
            double Aw = input.IsBuiltUp ? h * tw : d * tw; 

            result.Details.Add(new CalculationStep("Web Area Aw", 
                input.IsBuiltUp ? "h × tw" : "d × tw",
                input.IsBuiltUp ? $"{h:F1}×{tw:F1}" : $"{d:F1}×{tw:F1}", Aw, "mm²"));

            // Web slenderness
            double h_tw = h / tw;
            double kv = 5.0; // Unstiffened webs

            result.Details.Add(new CalculationStep("Web Slenderness h/tw", "",
                $"{h:F1}/{tw:F1}", h_tw));

            // Calculate Cv1 (G2.1)
            double Cv1;
            double limit1 = 2.24 * Math.Sqrt(E / Fy);
            double limit2 = 1.10 * Math.Sqrt(kv * E / Fy);

            result.Details.Add(new CalculationStep("Limit 2.24√(E/Fy)", "",
                $"2.24×√({E:F0}/{Fy:F0})", limit1));

            if (h_tw <= limit1)
            {
                // G2.1(a): For most rolled I-shapes
                Cv1 = 1.0;
                result.Details.Add(new CalculationStep("Cv1 (G2.1a)", 
                    "h/tw ≤ 2.24√(E/Fy)", "Cv1 = 1.0", Cv1));
            }
            else if (h_tw <= limit2)
            {
                // G2.1(b)(i)
                Cv1 = 1.0;
                result.Details.Add(new CalculationStep("Cv1 (G2.1b)", 
                    "h/tw ≤ 1.10√(kv×E/Fy)", "", Cv1));
            }
            else if (h_tw <= 1.37 * Math.Sqrt(kv * E / Fy))
            {
                // G2.1(b)(ii): Inelastic web buckling
                Cv1 = 1.10 * Math.Sqrt(kv * E / Fy) / h_tw;
                result.Details.Add(new CalculationStep("Cv1 (Inelastic)", 
                    "1.10√(kv×E/Fy)/(h/tw)", "", Cv1));
            }
            else
            {
                // G2.1(b)(iii): Elastic web buckling
                Cv1 = 1.51 * kv * E / (h_tw * h_tw * Fy);
                result.Details.Add(new CalculationStep("Cv1 (Elastic)", 
                    "1.51kv×E/((h/tw)²×Fy)", "", Cv1));
            }

            // Nominal shear strength
            double Vn = 0.6 * Fy * Aw * Cv1 / 1000; // kN

            result.Details.Add(new CalculationStep("Nominal Shear Vn", "0.6FyAwCv1",
                $"0.6×{Fy:F0}×{Aw:F0}×{Cv1:F3}/1000", Vn, "kN"));

            // Design strength (AISC 360-16, Chapter G):
            // use phi_v = 1.00 (LRFD) and omega_v = 1.50 (ASD) for shear checks.
            double phi = DesignFactors.Phi_Shear;
            double omega = DesignFactors.Omega_Shear;

            result.Capacity = DesignFactors.GetCapacity(Vn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? $"φVn (φ={phi:F2})" : $"Vn/Ω (Ω={omega:F2})",
                "", "", result.Capacity, "kN"));

            result.CalculateRatio();

            result.Details.Add(new CalculationStep("Demand/Capacity", "Vr/φVn",
                $"{result.Demand:F1}/{result.Capacity:F1}", result.Ratio));

            return result;
        }

        /// <summary>
        /// Calculate shear strength for weak axis (G7)
        /// </summary>
        public static CheckDetail CalculateShearMinor(AnalyzedMember input)
        {
            var result = new CheckDetail
            {
                Name = "Shear (Minor Axis)",
                Section = "G7",
                Demand = Math.Abs(input.Vry) / 1000.0 // N -> kN
            };

            if (result.Demand < 0.001)
            {
                result.Status = CheckStatus.NotApplicable;
                return result;
            }

            double Fy = input.Fy;
            double bf = input.bf;
            double tf = input.tf;

            if (Fy <= 0 || bf <= 0 || tf <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid section data for minor shear check", "", "", 0));
                return result;
            }

            double bfb = input.bfb > 0 ? input.bfb : bf;
            double tfb = input.tfb > 0 ? input.tfb : tf;

            // Flange shear area (both flanges)
            double Aw = bf * tf + bfb * tfb; // mm²
            double Cv2 = 1.0; // Usually compact flanges

            result.Details.Add(new CalculationStep("Flange Shear Area", "bf×tf + bfb×tfb",
                $"{bf:F1}×{tf:F1} + {bfb:F1}×{tfb:F1}", Aw, "mm²"));

            // Nominal shear strength
            double Vn = 0.6 * Fy * Aw * Cv2 / 1000.0; // kN

            result.Details.Add(new CalculationStep("Nominal Shear Vn", "0.6FyAwCv2",
                $"0.6×{Fy:F0}×{Aw:F0}×{Cv2:F3}/1000", Vn, "kN"));

            // Design strength
            // Design strength - φv = 0.90 for shear in flanges (Minor Axis) (G1)
            double phi = 0.90;
            double omega = 1.67;
            result.Capacity = DesignFactors.GetCapacity(Vn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? "φVn (φ=0.90)" : "Vn/Ω (Ω=1.67)",
                "", "", result.Capacity, "kN"));

            result.CalculateRatio();

            return result;
        }

        /// <summary>
        /// Calculate shear strength for Box/Rectangular HSS (G4)
        /// </summary>
        public static CheckDetail CalculateShearBoxHSS(AnalyzedMember input, bool isMajorAxis = true)
        {
            var result = new CheckDetail
            {
                Name = isMajorAxis ? "Shear (Box HSS Major)" : "Shear (Box HSS Minor)",
                Section = "G4",
                Demand = (isMajorAxis ? Math.Abs(input.Vrx) : Math.Abs(input.Vry)) / 1000.0 // N -> kN
            };

            if (result.Demand < 0.001)
            {
                result.Status = CheckStatus.NotApplicable;
                return result;
            }

            double E = input.E;
            double Fy = input.Fy;
            double Ht = input.Ht;
            double B = input.B;
            double t = input.tdes;

            if (t <= 0) t = input.tw > 0 ? input.tw : input.tf;
            if (E <= 0 || Fy <= 0 || Ht <= 0 || B <= 0 || t <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid box section data for shear check", "", "", 0));
                return result;
            }

            // For major axis shear: webs are the sides (height H)
            // For minor axis shear: webs are the top/bottom (width B)
            double h = isMajorAxis ? Ht - 3 * t : B - 3 * t;  // Clear height of web
            double d = isMajorAxis ? Ht : B;  // Overall depth in direction of shear
            if (h <= 0 || d <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid clear web depth for G4 check", "", "", 0));
                return result;
            }

            // G4: Shear area = 2 webs × h × t
            double Aw = 2 * h * t; // mm² (2 webs using clear web depth)

            result.Details.Add(new CalculationStep("Shear Area Aw", "2 × h × t",
                $"2×{h:F1}×{t:F1}", Aw, "mm²"));

            // Web slenderness
            double h_t = h / t;
            double kv = 5.0; // Unstiffened webs

            result.Details.Add(new CalculationStep("Web Slenderness h/t", "",
                $"{h:F1}/{t:F1}", h_t));

            // G4: Same Cv approach as G2.1(b)
            double Cv2;
            double limit1 = 1.10 * Math.Sqrt(kv * E / Fy);
            double limit2 = 1.37 * Math.Sqrt(kv * E / Fy);

            if (h_t <= limit1)
            {
                Cv2 = 1.0;
                result.Details.Add(new CalculationStep("Cv2 (G4)", 
                    "h/t ≤ 1.10√(kv×E/Fy)", "", Cv2));
            }
            else if (h_t <= limit2)
            {
                // Inelastic web buckling
                Cv2 = 1.10 * Math.Sqrt(kv * E / Fy) / h_t;
                result.Details.Add(new CalculationStep("Cv2 (Inelastic)", 
                    "1.10√(kv×E/Fy)/(h/t)", "", Cv2));
            }
            else
            {
                // Elastic web buckling
                Cv2 = 1.51 * kv * E / (h_t * h_t * Fy);
                result.Details.Add(new CalculationStep("Cv2 (Elastic)", 
                    "1.51kv×E/((h/t)²×Fy)", "", Cv2));
            }

            // Nominal shear strength
            double Vn = 0.6 * Fy * Aw * Cv2 / 1000; // kN

            result.Details.Add(new CalculationStep("Nominal Shear Vn", "0.6FyAwCv2",
                $"0.6×{Fy:F0}×{Aw:F0}×{Cv2:F3}/1000", Vn, "kN"));

            // Design strength - φv = 0.90 for HSS (G4)
            double phi = 0.90;
            double omega = 1.67;
            result.Capacity = DesignFactors.GetCapacity(Vn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? "φVn (φ=0.90)" : "Vn/Ω (Ω=1.67)",
                "", "", result.Capacity, "kN"));

            result.CalculateRatio();

            result.Details.Add(new CalculationStep("Demand/Capacity", "Vr/φVn",
                $"{result.Demand:F1}/{result.Capacity:F1}", result.Ratio));

            return result;
        }

        /// <summary>
        /// Calculate shear strength for Round HSS/Pipe (G5)
        /// </summary>
        public static CheckDetail CalculateShearRoundHSS(AnalyzedMember input)
        {
            var result = new CheckDetail
            {
                Name = "Shear (Round HSS)",
                Section = "G5",
                Demand = Math.Max(Math.Abs(input.Vrx), Math.Abs(input.Vry)) / 1000.0 // N -> kN
            };

            if (result.Demand < 0.001)
            {
                result.Status = CheckStatus.NotApplicable;
                return result;
            }

            double E = input.E;
            double Fy = input.Fy;
            double Ag = input.Ag;
            double D = input.D_pipe;
            double t = input.t_pipe;
            double Lv = input.Length; // Use frame length as shear span

            if (E <= 0 || Fy <= 0 || Ag <= 0 || D <= 0 || t <= 0 || Lv <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid round HSS data for shear check", "", "", 0));
                return result;
            }

            // Calculate Fcr based on D/t ratio
            double D_t = D / t;
            double Lv_D = Lv / D;

            result.Details.Add(new CalculationStep("D/t Ratio", "",
                $"{D:F1}/{t:F1}", D_t));

            // G5 limits
            double limit1 = 1.60 * E / Fy;
            double limit2Sqrt = Math.Sqrt(E / Fy);

            double Fcr;
            string condition;

            if (D_t <= limit1)
            {
                // G5-1: Fcr = max of G5-2a and G5-2b
                double Fcr_a = 1.60 * E / (limit2Sqrt * Math.Pow(Lv_D, 0.5) * Math.Pow(D_t, 1.25));
                double Fcr_b = 0.78 * E / Math.Pow(D_t, 1.5);
                Fcr = Math.Max(Fcr_a, Fcr_b);
                Fcr = Math.Min(Fcr, 0.6 * Fy); // Cap at yield
                condition = "D/t ≤ 1.60E/Fy";
            }
            else
            {
                // Beyond limit - use minimum
                Fcr = 0.78 * E / Math.Pow(D_t, 1.5);
                Fcr = Math.Min(Fcr, 0.6 * Fy);
                condition = "D/t > 1.60E/Fy";
            }

            result.Details.Add(new CalculationStep("Limit 1.60E/Fy", "",
                $"1.60×{E:F0}/{Fy:F0}", limit1));
            result.Details.Add(new CalculationStep("Fcr (G5)", condition,
                "", Fcr, "MPa"));

            // G5 shear area for round HSS = Ag/2
            double Aw = Ag / 2;
            result.Details.Add(new CalculationStep("Shear Area Ag/2", "",
                $"{Ag:F0}/2", Aw, "mm²"));

            // Nominal shear strength
            double Vn = Fcr * Aw / 1000; // kN

            result.Details.Add(new CalculationStep("Nominal Shear Vn", "Fcr × Ag/2",
                $"{Fcr:F1}×{Aw:F0}/1000", Vn, "kN"));

            // Design strength - φv = 0.90 for Round HSS (G5)
            double phi = 0.90;
            double omega = 1.67;
            result.Capacity = DesignFactors.GetCapacity(Vn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? "φVn (φ=0.90)" : "Vn/Ω (Ω=1.67)",
                "", "", result.Capacity, "kN"));

            result.CalculateRatio();

            result.Details.Add(new CalculationStep("Demand/Capacity", "Vr/φVn",
                $"{result.Demand:F1}/{result.Capacity:F1}", result.Ratio));

            return result;
        }
    }
}
