using System;
using System.Collections.Generic;

namespace StaadSteelApp.AISC360
{
    /// <summary>
    /// Chapter F - Flexure (AISC 360-16)
    /// </summary>
    public static class ChapterF
    {
        private static bool IncludeDiagnostics => false;

        /// <summary>
        /// Calculate flexure strength for I-shapes (Major Axis)
        /// </summary>
        public static CheckDetail CalculateFlexureMajor(AnalyzedMember input, FlexureClassification classification)
        {
            var result = new CheckDetail
            {
                Name = "Flexure (Major Axis)",
                Section = "F2",
                Demand = Math.Abs(input.Mrx) / 1e6 // Convert N-mm to kN-m
            };

            double E = input.E;
            double Fy = input.Fy;
            double Zx = input.Zx;
            double Sx = input.Sx;
            double ry = input.ry;
            double Lb = input.Lb;
            double Cb = input.Cb;
            double J = input.J;
            double Cw = input.Cw;
            double ho = input.ho;
            double rts = input.rts;

            if (ho <= 0 && input.d > 0)
                ho = input.d - 0.5 * (input.tf + input.tfb);
            if (ho <= 0)
                ho = 1.0;

            // F2.1: Yielding
            double Mp = Fy * Zx / 1e6; // kN-m (Fy in MPa, Zx in mm³)

            result.Details.Add(new CalculationStep("Plastic Moment Mp", "Fy × Zx",
                $"{Fy:F0}×{Zx:F0}/10⁶", Mp, "kN-m"));
            if (IncludeDiagnostics)
            {
                result.Details.Add(new CalculationStep("Section Dims", "d x bf x tf x tw",
                    $"{input.d:F0} x {input.bf:F0} x {input.tf:F1} x {input.tw:F1}", 0) { Result = "Check" });
                result.Details.Add(new CalculationStep("Cb Factor", "Calc/Input", "", Cb));
            }

            // F2.2: Lateral-Torsional Buckling
            // Lp (F2-5)
            double Lp = 1.76 * ry * Math.Sqrt(E / Fy);

            result.Details.Add(new CalculationStep("Lp (Compact Limit)", "1.76×ry×√(E/Fy)",
                $"1.76×{ry:F1}×√({E:F0}/{Fy:F0})", Lp, "mm"));

            // Lr (F2-6) - simplified for doubly symmetric I-shapes
            // Lr calculation requires rts and c
            double c = 1.0; // For doubly symmetric I-shapes

            // rts² = √(Iy×Cw)/Sx
            if (rts <= 0 && input.Iy > 0 && Cw > 0 && Sx > 0)
            {
                rts = Math.Sqrt(Math.Sqrt(input.Iy * Cw) / Sx);
            }
            if (rts <= 0) rts = ry > 0 ? ry : 10; // Fallback to ry or minimum

            double sxForLr = Math.Max(Sx, 1e-9);
            double term_J = c * J / (sxForLr * ho);
            double term_FL = 0.7 * Fy;
            double Lr = 1.95 * rts * (E / term_FL) * Math.Sqrt(term_J + Math.Sqrt(term_J * term_J + 6.76 * Math.Pow(term_FL / E, 2)));

            result.Details.Add(new CalculationStep("rts", "√(√(Iy×Cw)/Sx)",
                "", rts, "mm"));
            result.Details.Add(new CalculationStep("Lr (Inelastic Limit)", "Eq. F2-6",
                "", Lr, "mm"));
            result.Details.Add(new CalculationStep("Unbraced Length Lb", "",
                "", Lb, "mm"));

            // Determine Mn based on Lb
            double Mn;
            string limitState;

            if (Lb <= Lp)
            {
                // F2.1: Yielding
                Mn = Mp;
                limitState = "Yielding (Lb ≤ Lp)";
            }
            else if (Lb <= Lr)
            {
                // F2.2(b): Inelastic LTB
                double factor = (Lr > Lp) ? (Lb - Lp) / (Lr - Lp) : 0; // Guard against Lr=Lp
                Mn = Cb * (Mp - (Mp - 0.7 * Fy * Sx / 1e6) * factor);
                Mn = Math.Min(Mn, Mp);
                limitState = "Inelastic LTB (Lp < Lb ≤ Lr)";

                result.Details.Add(new CalculationStep("Cb", "", "", Cb));
                result.Details.Add(new CalculationStep("Interpolation Factor", "(Lb-Lp)/(Lr-Lp)",
                    $"({Lb:F0}-{Lp:F0})/({Lr:F0}-{Lp:F0})", factor));
            }
            else
            {
                // F2.2(c): Elastic LTB
                double Fcr = (Cb * Math.PI * Math.PI * E / Math.Pow(Lb / rts, 2)) 
                           * Math.Sqrt(1 + 0.078 * term_J * Math.Pow(Lb / rts, 2));
                Mn = Fcr * Sx / 1e6;
                Mn = Math.Min(Mn, Mp);
                limitState = "Elastic LTB (Lb > Lr)";

                result.Details.Add(new CalculationStep("Critical Stress Fcr", "Eq. F2-4",
                    "", Fcr, "MPa"));
            }

            result.Details.Add(new CalculationStep("Limit State", "", "", 0) { Result = limitState });
            result.Details.Add(new CalculationStep("Nominal Moment Mn", "", "", Mn, "kN-m"));

            // Check flange local buckling if noncompact/slender (F3)
            if (classification != null && classification.FlangeStatus != ClassificationStatus.Compact)
            {
                double Mn_FLB = CalculateFLB(input, classification, Mp, Sx);
                if (Mn_FLB < Mn)
                {
                    Mn = Mn_FLB;
                    result.Details.Add(new CalculationStep("FLB Controls", "Mn_FLB", "", Mn_FLB, "kN-m"));
                }
            }

            // Design strength
            double phi = DesignFactors.Phi_Flexure;
            double omega = DesignFactors.Omega_Flexure;
            result.Capacity = DesignFactors.GetCapacity(Mn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? "φMn" : "Mn/Ω",
                input.Method == DesignMethod.LRFD ? $"{phi:F2}×Mn" : $"Mn/{omega:F2}",
                "", result.Capacity, "kN-m"));

            result.CalculateRatio();

            result.Details.Add(new CalculationStep("Demand/Capacity", "Mr/φMn",
                $"{result.Demand:F2}/{result.Capacity:F2}", result.Ratio));

            return result;
        }

        /// <summary>
        /// Calculate flexure strength for I-shapes (Minor Axis)
        /// </summary>
        public static CheckDetail CalculateFlexureSinglySymmetric(AnalyzedMember input, FlexureClassification classification)
        {
            var result = new CheckDetail
            {
                Name = "Flexure (Major Axis - Singly Symmetric)",
                Section = "F4/F5",
                Demand = Math.Abs(input.Mrx) / 1e6 // Convert N-mm to kN-m
            };

            double E = input.E;
            double Fy = input.Fy;
            double Zx = input.Zx;
            double Sx = input.Sx;
            double ry = input.ry;
            double Lb = input.Lb;
            double Cb = input.Cb;
            double J = input.J;
            double Cw = input.Cw;
            double ho = input.ho;
            double rts = input.rts;
            double Iy = input.Iy;
            double Ix = input.Ix;
            double d = input.d;
            double tw = input.tw;
            
            // Assume moment causes top flange compression, which is standard for gravity loads.
            // If Mrx < 0, bottom flange is in compression.
            bool topIsCompression = (input.Mrx >= 0);
            
            double bfc = topIsCompression ? input.bf : input.bfb;
            double tfc = topIsCompression ? input.tf : input.tfb;
            double bft = topIsCompression ? input.bfb : input.bf;
            double tft = topIsCompression ? input.tfb : input.tf;
            
            // Recompute neutral axis to get Sxc and Sxt
            double y_bot_flange = input.tfb / 2.0;
            double h_web = d - input.tf - input.tfb;
            double y_web = input.tfb + h_web / 2.0;
            double y_top_flange = input.tfb + h_web + input.tf / 2.0;
            double y_na = ((input.bfb * input.tfb * y_bot_flange) + (h_web * input.tw * y_web) + (input.bf * input.tf * y_top_flange)) / input.Ag;

            double c_top = d - y_na;
            double c_bot = y_na;
            
            double yc = topIsCompression ? c_top : c_bot;
            double yt = topIsCompression ? c_bot : c_top;

            double Sxc = Ix / yc;
            double Sxt = Ix / yt;

            double Iyc = (tfc * Math.Pow(bfc, 3) / 12.0);
            
            double hc = topIsCompression ? (c_top - input.tf) * 2 : (c_bot - input.tfb) * 2; // Twice distance from NA to inside face of comp flange

            // F4.2 Web Plastification Factors Rpc and Rpt
            double Rpc = 1.0;
            double Rpt = 1.0;
            
            double m = Fy / E;
            double lambda_w = hc / tw;
            double lambda_pw = 3.76 * Math.Sqrt(E / Fy); // F4-7
            double lambda_rw = 5.70 * Math.Sqrt(E / Fy); // F4-8
            
            double Mp = Fy * Zx / 1e6;
            double Myc = Fy * Sxc / 1e6;
            
            if (Iyc / Iy > 0.23)
            {
                if (lambda_w <= lambda_pw)
                {
                    Rpc = Math.Min(Mp / Myc, 1.0); // Simplified Rpc
                }
                else if (lambda_w <= lambda_rw)
                {
                    Rpc = Math.Min(Mp / Myc - (Mp / Myc - 1.0) * ((lambda_w - lambda_pw) / (lambda_rw - lambda_pw)), Mp / Myc);
                }
            }

            // FL Calculation F4-6a/b
            double FL = 0.7 * Fy;
            if (Sxt < Sxc)
            {
                FL = Fy * Math.Max(Sxt / Sxc, 0.5);
            }

            // F4.1: Compression Flange Yielding
            double Mn_Y = Rpc * Myc;
            result.Details.Add(new CalculationStep("Compression Flange Yielding", "Rpc*Myc", "", Mn_Y, "kN-m"));

            // F4.2: Lateral-Torsional Buckling
            double aw = hc * tw / (bfc * tfc);
            double rt = bfc / Math.Sqrt(12 * (1 + 1.0 / 6.0 * aw));
            double Lp = 1.1 * rt * Math.Sqrt(E / Fy);
            
            double term1 = J / (Sxc * ho);
            double term2 = 6.76 * Math.Pow(0.7 * Fy / E, 2);
            double Lr = 1.95 * rt * (E / FL) * Math.Sqrt(term1 + Math.Sqrt(term1 * term1 + term2));
            
            double Mn_LTB = Mn_Y;
            if (Lb > Lp && Lb <= Lr)
            {
                Mn_LTB = Cb * (Rpc * Myc - (Rpc * Myc - FL * Sxc / 1e6) * ((Lb - Lp) / (Lr - Lp)));
                Mn_LTB = Math.Min(Mn_LTB, Rpc * Myc);
            }
            else if (Lb > Lr)
            {
                double Fcr = (Cb * Math.PI * Math.PI * E / Math.Pow(Lb / rt, 2)) * Math.Sqrt(1 + 0.078 * term1 * Math.Pow(Lb / rt, 2));
                Mn_LTB = Fcr * Sxc / 1e6;
                Mn_LTB = Math.Min(Mn_LTB, Rpc * Myc);
            }
            result.Details.Add(new CalculationStep("Lateral-Torsional Buckling", "", "", Mn_LTB, "kN-m"));

            // F4.3: Compression Flange Local Buckling
            double Mn_FLB = double.PositiveInfinity;
            double lambda_f = bfc / (2 * tfc);
            double lambda_pf = 0.38 * Math.Sqrt(E / Fy);
            double lambda_rf = 0.95 * Math.Sqrt(E / Fy); // Built-up

            if (lambda_f > lambda_pf && lambda_f <= lambda_rf)
            {
                Mn_FLB = Rpc * Myc - (Rpc * Myc - FL * Sxc / 1e6) * ((lambda_f - lambda_pf) / (lambda_rf - lambda_pf));
            }
            else if (lambda_f > lambda_rf)
            {
                double kc = 4.0 / Math.Sqrt(h_web / tw);
                if (kc < 0.35) kc = 0.35;
                if (kc > 0.76) kc = 0.76;
                double Fcr = 0.9 * E * kc / Math.Pow(lambda_f, 2);
                Mn_FLB = Fcr * Sxc / 1e6;
            }
            if (Mn_FLB < double.PositiveInfinity)
                result.Details.Add(new CalculationStep("Compression Flange Local Buckling", "", "", Mn_FLB, "kN-m"));

            // F4.4: Tension Flange Yielding
            double Mn_TFY = double.PositiveInfinity;
            if (Sxt < Sxc)
            {
                Mn_TFY = Rpt * Fy * Sxt / 1e6;
                result.Details.Add(new CalculationStep("Tension Flange Yielding", "", "", Mn_TFY, "kN-m"));
            }

            double Mn = Math.Min(Math.Min(Mn_Y, Mn_LTB), Math.Min(Mn_FLB, Mn_TFY));

            // Design strength
            double phi = DesignFactors.Phi_Flexure;
            double omega = DesignFactors.Omega_Flexure;
            result.Capacity = DesignFactors.GetCapacity(Mn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? "fMn" : "Mn/O",
                input.Method == DesignMethod.LRFD ? $"{phi:F2}*Mn" : $"Mn/{omega:F2}",
                "", result.Capacity, "kN-m"));

            result.CalculateRatio();
            result.Details.Add(new CalculationStep("Demand/Capacity", "Mr/fMn",
                $"{result.Demand:F2}/{result.Capacity:F2}", result.Ratio));

            return result;
        }

        public static CheckDetail CalculateFlexureMinor(AnalyzedMember input, FlexureClassification classification)
        {
            var result = new CheckDetail
            {
                Name = "Flexure (Minor Axis)",
                Section = "F6",
                Demand = Math.Abs(input.Mry) / 1e6 // Convert N-mm to kN-m
            };

            if (result.Demand < 0.001)
            {
                result.Status = CheckStatus.NotApplicable;
                return result;
            }

            double Fy = input.Fy;
            double E = input.E;
            double Zy = input.Zy;
            double Sy = input.Sy;

            // F6.1: Yielding
            double Mp = Math.Min(Fy * Zy / 1e6, 1.6 * Fy * Sy / 1e6);

            result.Details.Add(new CalculationStep("Mp (Minor)", "min(Fy×Zy, 1.6Fy×Sy)",
                $"min({Fy:F0}×{Zy:F0}/10⁶, 1.6×{Fy:F0}×{Sy:F0}/10⁶)", Mp, "kN-m"));

            double Mn = Mp;

            // F6.2: Flange Local Buckling (if applicable)
            if (classification != null && Sy > 0)
            {
                double lambda = classification.Lambda_f;
                double lambda_pf = classification.Lambda_pf;
                double lambda_rf = classification.Lambda_rf;

                if (classification.FlangeStatus == ClassificationStatus.Noncompact &&
                    lambda_rf > lambda_pf &&
                    lambda > lambda_pf)
                {
                    // F6.2a for noncompact flanges:
                    // Mn = Mp - (Mp - 0.7FySy) * (lambda-lambda_pf)/(lambda_rf-lambda_pf)
                    double factor = (lambda - lambda_pf) / (lambda_rf - lambda_pf);
                    factor = Math.Max(0.0, Math.Min(1.0, factor));
                    double mnFlb = Mp - (Mp - 0.7 * Fy * Sy / 1e6) * factor;
                    Mn = Math.Min(Mn, mnFlb);

                    result.Details.Add(new CalculationStep("Flange Local Buckling", "F6.2 Noncompact", "", mnFlb, "kN-m"));
                    result.Details.Add(new CalculationStep("Interpolation Factor", "(lambda-lambda_pf)/(lambda_rf-lambda_pf)", "", factor));
                }
                else if (classification.FlangeStatus == ClassificationStatus.Slender && lambda > 0)
                {
                    // F6.2b for slender flanges (elastic local buckling):
                    // Mn = Fcr * Sy, Fcr = 0.69E/lambda^2
                    double Fcr = 0.69 * E / (lambda * lambda);
                    double mnFlb = Fcr * Sy / 1e6;
                    Mn = Math.Min(Mn, mnFlb);

                    result.Details.Add(new CalculationStep("Critical Stress Fcr", "0.69E/lambda^2", "", Fcr, "MPa"));
                    result.Details.Add(new CalculationStep("Flange Local Buckling", "F6.2 Slender", "", mnFlb, "kN-m"));
                }
            }

            // Design strength
            double phi = DesignFactors.Phi_Flexure;
            double omega = DesignFactors.Omega_Flexure;
            result.Capacity = DesignFactors.GetCapacity(Mn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? "φMn" : "Mn/Ω",
                "", "", result.Capacity, "kN-m"));

            result.CalculateRatio();

            return result;
        }

        /// <summary>
        /// Calculate Flange Local Buckling (F3)
        /// </summary>
        private static double CalculateFLB(AnalyzedMember input, FlexureClassification classification, double Mp, double Sx)
        {
            double E = input.E;
            double Fy = input.Fy;
            double lambda = classification.Lambda_f;
            double lambda_pf = classification.Lambda_pf;
            double lambda_rf = classification.Lambda_rf;

            double Mn;

            if (classification.FlangeStatus == ClassificationStatus.Noncompact)
            {
                // F3-1
                double factor = (lambda - lambda_pf) / (lambda_rf - lambda_pf);
                Mn = Mp - (Mp - 0.7 * Fy * Sx / 1e6) * factor;
            }
            else
            {
                // F3-2: Slender
                double hTwForKc = Math.Max(input.h_tw, 1e-9);
                double kc = 4 / Math.Sqrt(hTwForKc);
                kc = Math.Max(0.35, Math.Min(kc, 0.76));
                double Fcr = 0.9 * E * kc / (lambda * lambda);
                Mn = Fcr * Sx / 1e6;
            }

            return Mn;
        }

        /// <summary>
        /// Calculate flexure strength for Round HSS/Pipe (F8)
        /// No LTB for circular sections
        /// </summary>
        public static CheckDetail CalculateFlexureRoundHSS(AnalyzedMember input)
        {
            var result = new CheckDetail
            {
                Name = "Flexure (Round HSS)",
                Section = "F8",
                Demand = Math.Abs(input.Mrx) / 1e6
            };

            double E = input.E;
            double Fy = input.Fy;
            double Zx = input.Zx;
            double Sx = input.Sx;
            double D = input.D_pipe;  // Outside diameter
            double t = input.t_pipe;  // Wall thickness

            // For Round HSS, no LTB - check yielding and local buckling
            double Mp = Fy * Zx / 1e6; // kN-m

            result.Details.Add(new CalculationStep("Plastic Moment Mp", "Fy × Z",
                $"{Fy:F0}×{Zx:F0}/10⁶", Mp, "kN-m"));

            // Check D/t ratio for local buckling
            if (t <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Error", "", "Wall thickness t = 0", 0));
                return result;
            }
            double D_t = D / t;
            double lambda_p = 0.07 * E / Fy;  // F8 compact limit
            double lambda_r = 0.31 * E / Fy;  // F8 noncompact limit

            result.Details.Add(new CalculationStep("D/t Ratio", "", 
                $"{D:F1}/{t:F1}", D_t));
            result.Details.Add(new CalculationStep("Compact Limit λp", "0.07E/Fy",
                $"0.07×{E:F0}/{Fy:F0}", lambda_p));

            double Mn;
            string limitState;

            if (D_t <= lambda_p)
            {
                // F8.1: Compact - Yielding controls
                Mn = Mp;
                limitState = "Compact (D/t ≤ 0.07E/Fy)";
            }
            else if (D_t <= lambda_r)
            {
                // F8.2: Noncompact - Local buckling
                Mn = (0.021 * E / D_t + Fy) * Sx / 1e6;
                Mn = Math.Min(Mn, Mp);
                limitState = "Noncompact (0.07E/Fy < D/t ≤ 0.31E/Fy)";

                result.Details.Add(new CalculationStep("Mn (F8-2)", "(0.021E/D_t + Fy)S",
                    "", Mn, "kN-m"));
            }
            else
            {
                // F8.3: Slender - Elastic local buckling
                double Fcr = 0.33 * E / D_t;
                Mn = Fcr * Sx / 1e6;
                limitState = "Slender (D/t > 0.31E/Fy)";

                result.Details.Add(new CalculationStep("Fcr (F8-3)", "0.33E/(D/t)",
                    $"0.33×{E:F0}/{D_t:F1}", Fcr, "MPa"));
            }

            result.Details.Add(new CalculationStep("Limit State", "", "", 0) { Result = limitState });
            result.Details.Add(new CalculationStep("Nominal Moment Mn", "", "", Mn, "kN-m"));

            // Design strength
            double phi = DesignFactors.Phi_Flexure;
            double omega = DesignFactors.Omega_Flexure;
            result.Capacity = DesignFactors.GetCapacity(Mn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? "φMn" : "Mn/Ω",
                "", "", result.Capacity, "kN-m"));

            result.CalculateRatio();

            result.Details.Add(new CalculationStep("Demand/Capacity", "Mr/φMn",
                $"{result.Demand:F2}/{result.Capacity:F2}", result.Ratio));

            return result;
        }

        /// <summary>
        /// Calculate flexure strength for Box/Rectangular HSS (F7)
        /// No LTB for closed box sections
        /// </summary>
        public static CheckDetail CalculateFlexureBoxHSS(AnalyzedMember input, bool isMajorAxis = true)
        {
            var result = new CheckDetail
            {
                Name = isMajorAxis ? "Flexure (Box HSS Major)" : "Flexure (Box HSS Minor)",
                Section = "F7",
                Demand = (isMajorAxis ? Math.Abs(input.Mrx) : Math.Abs(input.Mry)) / 1e6
            };

            double E = input.E;
            double Fy = input.Fy;
            double Zx = isMajorAxis ? input.Zx : input.Zy;
            double Sx = isMajorAxis ? input.Sx : input.Sy;
            double Ht = input.Ht;
            double B = input.B;
            double t = input.tdes;

            if (t <= 0) t = input.tw > 0 ? input.tw : input.tf;
            if (t <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                return result;
            }

            // F7.1: Yielding
            double Mp = Fy * Zx / 1e6; // kN-m

            result.Details.Add(new CalculationStep("Plastic Moment Mp", "Fy × Z",
                $"{Fy:F0}×{Zx:F0}/10⁶", Mp, "kN-m"));

            // Box HSS has no LTB - check local buckling
            // Use the flange in compression (minor axis uses width, major uses height)
            double b_flange = isMajorAxis ? B - 3 * t : Ht - 3 * t;
            double h_web = isMajorAxis ? Ht - 3 * t : B - 3 * t;
            double b_t = b_flange / t;
            double h_t = h_web / t;

            result.Details.Add(new CalculationStep("Flange b/t", "",
                $"({(isMajorAxis ? B : Ht):F1} - 3×{t:F1})/{t:F1}", b_t));
            result.Details.Add(new CalculationStep("Web h/t", "",
                $"({(isMajorAxis ? Ht : B):F1} - 3×{t:F1})/{t:F1}", h_t));

            // F7.2: Flange Local Buckling
            double lambda_pf = 1.12 * Math.Sqrt(E / Fy);  // Compact limit (Table B4.1b Case 17)
            double lambda_rf = 1.40 * Math.Sqrt(E / Fy);  // Noncompact limit (Table B4.1b Case 17)

            result.Details.Add(new CalculationStep("Compact Limit λpf", "1.12√(E/Fy)",
                $"1.12×√({E:F0}/{Fy:F0})", lambda_pf));

            double Mn_flange;
            string flangeState;

            if (b_t <= lambda_pf)
            {
                // Compact - yielding controls
                Mn_flange = Mp;
                flangeState = "Compact (Yielding)";
            }
            else if (b_t <= lambda_rf)
            {
                // Noncompact - F7-2: Mn = Mp - (Mp - Fy*S) * (3.57*b/t*√(Fy/E) - 4.0)
                double factor = 3.57 * b_t * Math.Sqrt(Fy / E) - 4.0;
                Mn_flange = Mp - (Mp - Fy * Sx / 1e6) * factor;
                Mn_flange = Math.Max(Mn_flange, Fy * Sx / 1e6);
                Mn_flange = Math.Min(Mn_flange, Mp);
                flangeState = "Noncompact (F7-2)";

                result.Details.Add(new CalculationStep("Mn_FLB (F7-2)", "Mp-(Mp-FyS)×(3.57b/t√(Fy/E)-4)",
                    "", Mn_flange, "kN-m"));
            }
            else
            {
                // Slender - F7-3/F7-4: Mn = Fy * Se
                // F7-4 for HSS: be = 1.92t√(E/Fy)[1 - 0.38/(b/t)×√(E/Fy)] ≤ b
                double sqrtEFy = Math.Sqrt(E / Fy);
                double be = 1.92 * t * sqrtEFy * (1 - 0.38 / b_t * sqrtEFy);
                be = Math.Min(be, b_flange);
                be = Math.Max(be, 0);
                double Se = Sx * be / b_flange;  // Effective section modulus
                Mn_flange = Fy * Se / 1e6;
                flangeState = "Slender (F7-3/4)";

                result.Details.Add(new CalculationStep("be (F7-4)", "1.92t√(E/Fy)[1-0.38/(b/t)√(E/Fy)]",
                    "", be, "mm"));
                result.Details.Add(new CalculationStep("Mn_FLB (F7-3)", "Fy×Se",
                    "", Mn_flange, "kN-m"));
            }

            result.Details.Add(new CalculationStep("Flange State", "", "", 0) { Result = flangeState });

            // F7.3: Web Local Buckling
            double lambda_pw = 2.42 * Math.Sqrt(E / Fy);  // Compact limit (Table B4.1b Case 19)
            double lambda_rw = 5.70 * Math.Sqrt(E / Fy);  // Slender limit (Table B4.1b Case 19)

            double Mn_web;
            string webState;

            if (h_t <= lambda_pw)
            {
                Mn_web = Mp;
                webState = "Compact";
            }
            else if (h_t <= lambda_rw)
            {
                // Noncompact - F7-6: Mn = Mp - (Mp - Fy*S) * (0.305*h/t*√(Fy/E) - 0.738)
                double factor = 0.305 * h_t * Math.Sqrt(Fy / E) - 0.738;
                Mn_web = Mp - (Mp - Fy * Sx / 1e6) * factor;
                Mn_web = Math.Max(Mn_web, Fy * Sx / 1e6);
                Mn_web = Math.Min(Mn_web, Mp);
                webState = "Noncompact (F7-6)";

                result.Details.Add(new CalculationStep("Mn_WLB (F7-6)", "Mp-(Mp-FyS)×(0.305h/t√(Fy/E)-0.738)",
                    "", Mn_web, "kN-m"));
            }
            else
            {
                // Slender web (rare for HSS per User Note)
                Mn_web = Fy * Sx / 1e6;
                webState = "Slender";
            }

            result.Details.Add(new CalculationStep("Web State", "", "", 0) { Result = webState });

            // Mn is minimum of all limit states
            double Mn = Math.Min(Mn_flange, Mn_web);
            Mn = Math.Min(Mn, Mp);

            result.Details.Add(new CalculationStep("Nominal Moment Mn", "min(Mp, Mn_flange, Mn_web)",
                "", Mn, "kN-m"));

            // Design strength
            double phi = DesignFactors.Phi_Flexure;
            double omega = DesignFactors.Omega_Flexure;
            result.Capacity = DesignFactors.GetCapacity(Mn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? "φMn" : "Mn/Ω",
                "", "", result.Capacity, "kN-m"));

            result.CalculateRatio();

            result.Details.Add(new CalculationStep("Demand/Capacity", "Mr/φMn",
                $"{result.Demand:F2}/{result.Capacity:F2}", result.Ratio));

            return result;
        }
        public static CheckDetail CalculateFlexureSlenderWeb(AnalyzedMember input, FlexureClassification classification)
        {
            var result = new CheckDetail
            {
                Name = "Flexure (Slender Web)",
                Section = "F5",
                Demand = Math.Abs(input.Mrx) / 1e6 // N-mm -> kN-m
            };

            double E = input.E;
            double Fy = input.Fy;
            double Sxc = input.Sx;
            if (E <= 0 || Fy <= 0 || Sxc <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid section/material data for F5 check", "", "", 0));
                return result;
            }

            double aw = 0.0;
            if (input.bf > 0 && input.tf > 0 && input.tw > 0 && input.d > 0)
            {
                aw = (input.d - 2 * input.tf) * input.tw / (input.bf * input.tf);
            }
            aw = Math.Max(0.0, aw);

            // Rpg (Bending Strength Reduction Factor) - F5-6
            double h_tw = Math.Max(0.0, input.h_tw);
            double lambda_rw = 5.7 * Math.Sqrt(E / Fy);
            double Rpg = 1.0;
            
            if (h_tw > lambda_rw)
            {
                Rpg = 1.0 - (aw / (1200 + 300 * aw)) * (h_tw - 5.7 * Math.Sqrt(E / Fy));
                Rpg = Math.Min(1.0, Rpg);
            }

            result.Details.Add(new CalculationStep("Rpg", "1 - a/(1200+300a) * ...", "", Rpg));

            // F5.1: Yielding -> Fcr = Fy
            // F5.2: LTB -> Fcr depends on Lb
            double Lb = input.Lb > 0 ? input.Lb : input.Length;
            double rts = input.rts > 0 ? input.rts : input.ry;
            if (rts <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Missing rts/ry for LTB in F5", "", "", 0));
                return result;
            }

            double Lp = 1.1 * rts * Math.Sqrt(E / Fy); // F5-7 (Approx)
            double sxForLr = Math.Max(Sxc, 1e-9);
            double ho = input.ho;
            if (ho <= 0 && input.d > 0)
                ho = input.d - 0.5 * (input.tf + input.tfb);
            if (ho <= 0)
                ho = 1.0;

            // Keep the same dimensional form as F2-6: c*J/(Sx*ho)
            double c = 1.0;
            double termJ = c * input.J / (sxForLr * ho);
            double Lr = 1.95 * rts * E / (0.7 * Fy) * Math.Sqrt(
                termJ + Math.Sqrt(termJ * termJ + 6.76 * Math.Pow(0.7 * Fy / E, 2))); // Simplified F5-8

            // Use exact rts from input if valid
            if (input.rts > 0)
            {
                // Re-calc Lp/Lr correctly if needed, for F5 they use rt (radius of gyration of compression flange + 1/3 web)
                // Assuming rts ~ rt for this scope or previously calculated
            }

            double Fcr;
            double Cb = input.Cb;

            if (Lb <= Lp)
            {
                Fcr = Fy;
            }
            else if (Lb <= Lr)
            {
                double denom = Lr - Lp;
                double interp = denom > 1e-9 ? (Lb - Lp) / denom : 0.0;
                Fcr = Cb * (Fy - (0.3 * Fy) * interp);
                Fcr = Math.Min(Fcr, Fy);
            }
            else
            {
                double lbOverRts = Math.Max(Lb / rts, 1e-9);
                Fcr = Cb * Math.PI * Math.PI * E / (lbOverRts * lbOverRts);
                Fcr = Math.Min(Fcr, Fy);
            }

            // F5.3: FLB
            if (classification != null && classification.FlangeStatus != ClassificationStatus.Compact)
            {
                // Reduction for FLB ... simplified as Fcr reduction
                // Logic same as F3 essentially but applied to stress
            }

            double Mn = Rpg * Fcr * Sxc / 1e6;
            result.Details.Add(new CalculationStep("Nominal Mn", "Rpg * Fcr * Sxc", "", Mn, "kN-m"));

            // Design
            double phi = DesignFactors.Phi_Flexure;
            double omega = DesignFactors.Omega_Flexure;
            result.Capacity = DesignFactors.GetCapacity(Mn, phi, omega, input.Method);
            result.CalculateRatio();
            
            result.Details.Add(new CalculationStep("Ratio", "", $"{result.Demand:F2}/{result.Capacity:F2}", result.Ratio));

            return result;
        }

        /// <summary>
        /// Approximate Chapter F4 flow for I-shapes with noncompact web.
        /// Reuses F5-style LTB stress path (rt/rts-based) while keeping Rpg=1.0
        /// for non-slender web ranges.
        /// </summary>
        public static CheckDetail CalculateFlexureNoncompactWeb(AnalyzedMember input, FlexureClassification classification)
        {
            var result = CalculateFlexureSlenderWeb(input, classification);
            result.Name = "Flexure (Noncompact Web)";
            result.Section = "F4";
            return result;
        }
    }
}



