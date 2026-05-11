using System;
using System.Collections.Generic;

namespace StaadSteelApp.AISC360
{
    /// <summary>
    /// Section classification per AISC 360-16 Table B4.1
    /// </summary>
    public static class Classification
    {
        /// <summary>
        /// Classify I-Shape for flexure (Table B4.1b)
        /// </summary>
        public static FlexureClassification ClassifyFlexureForIShape(AnalyzedMember input)
        {
            var result = new FlexureClassification();
            double E = input.E;
            double Fy = input.Fy > 0 ? input.Fy : 345; // Guard against Fy=0

            // Flange: bf/(2*tf) - Case 10 for rolled I-shapes
            // Flange: bf/(2*tf)
            double lambda_f = input.bf_2tf >= 0 ? input.bf_2tf : 0;
            double lambda_pf = 0.38 * Math.Sqrt(E / Fy);
            
            // Noncompact Limit (lambda_rf) depends on Rolled (Case 10) vs Built-up (Case 11)
            double lambda_rf;
            double kc = 1.0; 
            
            if (input.IsBuiltUp)
            {
                // Case 11: Built-up I-Shape
                // kc = 4/sqrt(h/tw), 0.35 <= kc <= 0.76
                double h_tw = Math.Max(input.h_tw, 0.1);
                kc = 4.0 / Math.Sqrt(h_tw);
                kc = Math.Max(0.35, Math.Min(kc, 0.76));
                
                // FL = 0.7Fy for typical major axis bending (Table B4.1b Note [b])
                // limit = 0.95 * sqrt(kc * E / FL)
                lambda_rf = 0.95 * Math.Sqrt((kc * E) / (0.7 * Fy));
            }
            else
            {
                // Case 10: Rolled I-Shape
                lambda_rf = 1.0 * Math.Sqrt(E / Fy);
            }

            result.Lambda_f = lambda_f;
            result.Lambda_pf = lambda_pf;
            result.Lambda_rf = lambda_rf;
            result.FlangeStatus = Classify(lambda_f, lambda_pf, lambda_rf);

            result.Details.Add(new CalculationStep
            {
                Description = "Flange Slenderness (bf/2tf)",
                Formula = "λf",
                Substitution = $"{input.bf:F1}/(2×{input.tf:F1})",
                Result = lambda_f.ToString("F2")
            });
            result.Details.Add(new CalculationStep
            {
                Description = "Compact Limit (Table B4.1b Case 10/11)",
                Formula = "λpf = 0.38√(E/Fy)",
                Substitution = $"0.38×√({E:F0}/{Fy:F0})",
                Result = lambda_pf.ToString("F2")
            });
            if (input.IsBuiltUp)
            {
               result.Details.Add(new CalculationStep("Factor kc (Built-up)", "4/√(h/tw)", "", kc));
            }
            result.Details.Add(new CalculationStep
            {
                Description = "Non-Compact Limit (λrf)",
                Formula = input.IsBuiltUp ? "0.95√(kc E / 0.7Fy)" : "1.0√(E/Fy)",
                Result = lambda_rf.ToString("F2")
            });

            // Web: h/tw - Case 15 for doubly symmetric I-shapes
            double lambda_w = input.h_tw;
            double lambda_pw = 3.76 * Math.Sqrt(E / Fy);
            double lambda_rw = 5.70 * Math.Sqrt(E / Fy);

            result.Lambda_w = lambda_w;
            result.Lambda_pw = lambda_pw;
            result.Lambda_rw = lambda_rw;
            result.WebStatus = Classify(lambda_w, lambda_pw, lambda_rw);

            result.Details.Add(new CalculationStep
            {
                Description = "Web Slenderness (h/tw)",
                Formula = "λw",
                Substitution = $"{input.hc:F1}/{input.tw:F1}",
                Result = lambda_w.ToString("F2")
            });
            result.Details.Add(new CalculationStep
            {
                Description = "Compact Limit (Table B4.1b Case 15)",
                Formula = "λpw = 3.76√(E/Fy)",
                Substitution = $"3.76×√({E:F0}/{Fy:F0})",
                Result = lambda_pw.ToString("F2")
            });

            // Overall section classification
            if (result.FlangeStatus == ClassificationStatus.Compact && result.WebStatus == ClassificationStatus.Compact)
                result.SectionStatus = ClassificationStatus.Compact;
            else if (result.FlangeStatus == ClassificationStatus.Slender || result.WebStatus == ClassificationStatus.Slender)
                result.SectionStatus = ClassificationStatus.Slender;
            else
                result.SectionStatus = ClassificationStatus.Noncompact;

            result.Details.Add(new CalculationStep
            {
                Description = "Flange Classification",
                Result = result.FlangeStatus.ToString()
            });
            result.Details.Add(new CalculationStep
            {
                Description = "Web Classification",
                Result = result.WebStatus.ToString()
            });
            result.Details.Add(new CalculationStep
            {
                Description = "Section Classification",
                Result = result.SectionStatus.ToString()
            });

            return result;
        }

        /// <summary>
        /// Classify I-Shape for compression (Table B4.1a)
        /// </summary>
        public static CompressionClassification ClassifyCompressionForIShape(AnalyzedMember input)
        {
            var result = new CompressionClassification();
            double E = input.E;
            double Fy = input.Fy > 0 ? input.Fy : 345;

            // Flange: bf/(2*tf) - Case 1 for flanges of rolled I-shapes
            double lambda_f = input.bf_2tf;
            double lambda_rf = 0.56 * Math.Sqrt(E / Fy);

            result.Lambda_f = lambda_f;
            result.Lambda_rf = lambda_rf;
            result.FlangeStatus = lambda_f <= lambda_rf ? ClassificationStatus.Compact : ClassificationStatus.Slender;

            result.Details.Add(new CalculationStep
            {
                Description = "Flange Slenderness (bf/2tf)",
                Formula = "λf",
                Substitution = $"{input.bf:F1}/(2×{input.tf:F1})",
                Result = lambda_f.ToString("F2")
            });
            result.Details.Add(new CalculationStep
            {
                Description = "Nonslender Limit (Table B4.1a Case 1)",
                Formula = "λr = 0.56√(E/Fy)",
                Substitution = $"0.56×√({E:F0}/{Fy:F0})",
                Result = lambda_rf.ToString("F2")
            });

            // Web: h/tw - Case 5 for webs of I-shape
            double lambda_w = input.h_tw;
            double lambda_rw = 1.49 * Math.Sqrt(E / Fy);

            result.Lambda_w = lambda_w;
            result.Lambda_rw = lambda_rw;
            result.WebStatus = lambda_w <= lambda_rw ? ClassificationStatus.Compact : ClassificationStatus.Slender;

            result.Details.Add(new CalculationStep
            {
                Description = "Web Slenderness (h/tw)",
                Formula = "λw",
                Substitution = $"{input.hc:F1}/{input.tw:F1}",
                Result = lambda_w.ToString("F2")
            });
            result.Details.Add(new CalculationStep
            {
                Description = "Nonslender Limit (Table B4.1a Case 5)",
                Formula = "λr = 1.49√(E/Fy)",
                Substitution = $"1.49×√({E:F0}/{Fy:F0})",
                Result = lambda_rw.ToString("F2")
            });

            // Explicitly report status
            result.Details.Add(new CalculationStep("Flange Status", "", "", 0) { Result = result.FlangeStatus.ToString() });
            result.Details.Add(new CalculationStep("Web Status", "", "", 0) { Result = result.WebStatus.ToString() });

            // Is section slender?
            result.IsSectionSlender = result.FlangeStatus == ClassificationStatus.Slender || 
                                       result.WebStatus == ClassificationStatus.Slender;

            // E7 reduction factors for slender compression elements.
            if (result.IsSectionSlender)
            {
                CalculateQ(input, result);
            }

            result.Details.Add(new CalculationStep
            {
                Description = "Reduction Factor Q",
                Result = result.Q.ToString("F3")
            });

            return result;
        }

        /// <summary>
        /// Classify Box/Rect HSS for compression (Table B4.1a Case 6)
        /// </summary>
        public static CompressionClassification ClassifyCompressionForBoxHSS(AnalyzedMember input)
        {
            var result = new CompressionClassification();
            double E = input.E;
            double Fy = input.Fy > 0 ? input.Fy : 345;
            
            // Flanges AND Webs: b/t - Table B4.1a Case 6
            // Limit lambda_r = 1.40 * sqrt(E/Fy)
            double lambda_r = 1.40 * Math.Sqrt(E / Fy);
            
            // Flange (bf_2tf for Box is (B-3t)/t)
            double lambda_f = input.bf_2tf; // This is actually b/t for flange
            result.Lambda_f = lambda_f;
            result.Lambda_rf = lambda_r; // Same limit for flange and web in Box HSS
            result.FlangeStatus = lambda_f <= lambda_r ? ClassificationStatus.Compact : ClassificationStatus.Slender;

            // Web (h_tw for Box is (H-3t)/t)
            double lambda_w = input.h_tw;
            result.Lambda_w = lambda_w;
            result.Lambda_rw = lambda_r;
            result.WebStatus = lambda_w <= lambda_r ? ClassificationStatus.Compact : ClassificationStatus.Slender;

            // Overall Slenderness
            result.IsSectionSlender = result.FlangeStatus == ClassificationStatus.Slender || result.WebStatus == ClassificationStatus.Slender;

            result.Details.Add(new CalculationStep("Flange b/t", "Box", "", lambda_f));
            result.Details.Add(new CalculationStep("Web h/t", "Box", "", lambda_w));
            result.Details.Add(new CalculationStep("Limit λr", "1.40√(E/Fy)", "", lambda_r));

            if (result.IsSectionSlender) CalculateQ(input, result);
            
            // Fill details
            result.Details.Add(new CalculationStep("Section Slender?", "", result.IsSectionSlender ? "Yes" : "No", 0));
            if (result.IsSectionSlender) result.Details.Add(new CalculationStep("Reduction Factor Q", "", "", result.Q));

            return result;
        }

        /// <summary>
        /// Classify Round HSS/Pipe for compression (Table B4.1a Case 9)
        /// </summary>
        public static CompressionClassification ClassifyCompressionForPipe(AnalyzedMember input)
        {
            var result = new CompressionClassification();
            double E = input.E;
            double Fy = input.Fy > 0 ? input.Fy : 345;

            if (input.t_pipe <= 0 || input.D_pipe <= 0)
            {
                result.Details.Add(new CalculationStep("Invalid round HSS dimensions for compression classification", "", "", 0));
                return result;
            }

            // D/t - Case 9
            double D_t = input.D_pipe / input.t_pipe;
            double lambda_r = 0.11 * E / Fy; // Note: 0.11 E/Fy NOT Sqrt

            result.Lambda_f = D_t; // Treat D/t as 'flange' param for simplicity
            result.Lambda_rf = lambda_r;
            // Note: Pipe only has one element check
            result.IsSectionSlender = D_t > lambda_r;
            result.FlangeStatus = result.IsSectionSlender ? ClassificationStatus.Slender : ClassificationStatus.Compact;
            result.WebStatus = ClassificationStatus.Compact;

            result.Details.Add(new CalculationStep("Ratio D/t", "", $"{input.D_pipe:F1}/{input.t_pipe:F1}", D_t));
            result.Details.Add(new CalculationStep("Limit 0.11E/Fy", "", $"0.11×{E:F0}/{Fy:F0}", lambda_r));

            if (result.IsSectionSlender) CalculateQ(input, result);

            result.Details.Add(new CalculationStep("Section Slender?", "", result.IsSectionSlender ? "Yes" : "No", 0));
            if (result.IsSectionSlender) result.Details.Add(new CalculationStep("Reduction Factor Q", "", "", result.Q));

            return result;
        }

        /// <summary>
        /// Classify Channel for compression
        /// </summary>
        public static CompressionClassification ClassifyCompressionForChannel(AnalyzedMember input)
        {
            // Similar to I-shape: Flanges (Case 1), Web (Case 5)
            return ClassifyCompressionForIShape(input);
        }
        
        /// <summary>
        /// Calculate Q factor for slender elements (E7)
        /// Updated to handle I-Shape, Box HSS, Pipe
        /// </summary>
        private static void CalculateQ(AnalyzedMember input, CompressionClassification result)
        {
            double E = input.E;
            double Fy = input.Fy;
            if (E <= 0 || Fy <= 0 || input.Ag <= 0)
            {
                result.Qs = 1.0;
                result.Qa = 1.0;
                result.Q = 1.0;
                return;
            }
            double Qs = 1.0;
            double Qa = 1.0;

            // Round HSS (pipe) uses Qa per E7.2(c).
            // E7.2 (c) Round HSS: Q = Q_a. 
            // Equation E7-19: Q = Q_a = (0.038 E)/(Fy(D/t)) + 2/3
            if (input.IsPipe)
            {
                if (input.t_pipe <= 0 || input.D_pipe <= 0)
                {
                    result.Qa = 1.0;
                    result.Qs = 1.0;
                    result.Q = 1.0;
                    return;
                }
                double D_t = input.D_pipe / input.t_pipe;
                // E7-19 applies when 0.11 E/Fy < D/t < 0.45 E/Fy
                double limit_lower = 0.11 * E / Fy;
                double limit_upper = 0.45 * E / Fy;
                
                if (D_t > limit_lower && D_t < limit_upper)
                {
                    Qa = (0.038 * E) / (Fy * D_t) + 2.0/3.0;
                }
                else if (D_t >= limit_upper)
                {
                     // E7.2 identifies members above 0.45E/Fy as unsuitable for compression.
                     // Keep the formula value so the caller can report the governing warning.
                     Qa = (0.038 * E) / (Fy * D_t) + 2.0/3.0; 
                }
                
                result.Details.Add(new CalculationStep("Qa (Pipe E7.2)", "E7-19", "", Qa));
                result.Qa = Qa;
                result.Qs = 1.0;
                result.Q = Qa;
                return; // Done for pipe
            }

            // B. Stiffened vs Unstiffened Logic
            // I-Shape: Flanges (Unstiffened -> Qs), Web (Stiffened -> Qa)
            // Box HSS: Flanges (Stiffened -> Qa), Webs (Stiffened -> Qa) -> Only Qa calculated
            // Channel: Flanges (Unstiffened -> Qs), Web (Stiffened -> Qa)
            
            bool hasUnstiffenedElem = (input.IsAngle || input.IsChannel || (!input.IsBoxHSS && !input.IsPipe)); // I-shape, Channel, Angle
            bool hasStiffenedElem = (input.IsBoxHSS || !input.IsAngle); // I-shape Web, Channel Web, Box Walls

            // 1. Qs for Slender Unstiffened Elements (Flanges of I/C/L) - E7.1
            if (hasUnstiffenedElem && result.FlangeStatus == ClassificationStatus.Slender)
            {
                double b_t = input.bf_2tf; // Defined as b/t
                // Constants depend on element type. 
                // For I-shape/Channel flanges (Rolled): kc = 0.35 <= 4/sqrt(h/tw) <= 0.76 ? No that's Flexure.
                // E7.1 (b) Flanges of Rolled I-shapes/Channels:
                // Limit1 = 0.56 sqrt(E/Fy), Limit2 = 1.03 sqrt(E/Fy)
                // Eqs E7-4, E7-5, E7-6.
                
                double limit1 = 0.56 * Math.Sqrt(E / Fy);
                double limit2 = 1.03 * Math.Sqrt(E / Fy);
                
                if (b_t >= limit1 && b_t < limit2)
                    Qs = 1.415 - 0.74 * b_t * Math.Sqrt(Fy / E); // E7-4
                else if (b_t >= limit2)
                    Qs = 0.69 * E / (Fy * b_t * b_t); // E7-5
                    
                result.Details.Add(new CalculationStep("Qs (Unstiffened E7.1)", "E7-4/5", "", Qs));
            }

            // 2. Qa for Slender Stiffened Elements (Webs of I/C, Flanges/Webs of Box) - E7.2
            // Used for Box HSS and I/C Webs.
            
            // Calculate effective area A_eff
            double Ag_calc = input.Ag; // Start with full area
            double Area_lost = 0;
            
            // For Box HSS, check both Flanges and Webs as Stiffened Elements (Table E7.1 Case b)
            if (input.IsBoxHSS)
            {
                double f = Fy; 
                double c1 = 0.20; // Table E7.1 Case (b) Walls of HSS
                
                // Box Flange (Width B - 3t)
                if (result.FlangeStatus == ClassificationStatus.Slender)
                {
                    double b = input.bf_2tf * input.tdes; // recover clear width b
                    double t = input.tdes;
                    double be = CalculateEffectiveWidth(b, t, E, f, c1);
                    Area_lost += 2 * (b - be) * t; // 2 flanges
                }
                
                // Box Web (Height H - 3t)
                if (result.WebStatus == ClassificationStatus.Slender)
                {
                    double h = input.h_tw * input.tdes; // recover clear height h
                    double t = input.tdes;
                    double he = CalculateEffectiveWidth(h, t, E, f, c1);
                    Area_lost += 2 * (h - he) * t; // 2 webs
                }
            }
            else
            {
                 // I-Shape / Channel Web (Table E7.1 Case a)
                 if (result.WebStatus == ClassificationStatus.Slender)
                 {
                    double h = input.hc;
                    double t = input.tw;
                    double f = Fy; 
                    double c1 = 0.18; // Table E7.1 Case (a) Stiffened elements except HSS
                    double he = CalculateEffectiveWidth(h, t, E, f, c1);
                    Area_lost += (h - he) * t; // 1 web
                 }
            }
            
            if (Area_lost > 0)
            {
               Qa = (input.Ag - Area_lost) / input.Ag;
               result.Details.Add(new CalculationStep("Qa (Stiffened E7.2)", "Aeff/Ag", "", Qa));
            }

            result.Qs = Qs;
            result.Qa = Qa;
            result.Q = Qs * Qa;
        }

        private static double CalculateEffectiveWidth(double b, double t, double E, double f, double c1)
        {
            // Eq E7-17 (Modified for 360-16 structure using c1)
            // be = b * [ 1 - c1 * sqrt(E/f) / (b/t) ] * ... ??
            // Using standard derived form:
            // be = 1.92 * t * sqrt(E/f) * [ 1 - c1 * sqrt(E/f) / (b/t) ] <= b
             
            double b_t = b / t;
            double sqrt_E_f = Math.Sqrt(E / f);
            
            // E7-17
            double term = 1.0 - (c1 * sqrt_E_f) / b_t;
            double be = 1.92 * t * sqrt_E_f * term;
            
            return Math.Max(0, Math.Min(be, b));
        }

        private static ClassificationStatus Classify(double lambda, double lambda_p, double lambda_r)
        {
            if (lambda <= lambda_p) return ClassificationStatus.Compact;
            if (lambda <= lambda_r) return ClassificationStatus.Noncompact;
            return ClassificationStatus.Slender;
        }
    }

}
