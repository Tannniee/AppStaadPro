using System;
using System.Collections.Generic;

namespace StaadSteelApp.AISC360
{
    /// <summary>
    /// Chapter E - Compression (AISC 360-16)
    /// </summary>
    public static class ChapterE
    {
        /// <summary>
        /// Calculate compression strength for I-shapes
        /// </summary>
        public static CheckDetail CalculateCompression(AnalyzedMember input, CompressionClassification classification)
        {
            var result = new CheckDetail
            {
                Name = "Axial Compression",
                Section = "E3/E7",
                Demand = Math.Abs(input.Pr) / 1000.0 // N -> kN
            };

            double E = input.E;
            double Fy = input.Fy;
            double Ag = input.Ag;
            double Q = classification?.Q ?? 1.0;

            // Report Classification Details
            if (classification != null)
            {
                foreach(var step in classification.Details)
                {
                    result.Details.Add(step);
                }
            }
            
            if (E <= 0 || Fy <= 0 || Ag <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid section/material data for E3/E7 check", "", "", 0));
                return result;
            }

            // Guard against zero radius of gyration
            double rx = input.rx > 0 ? input.rx : 10; // Fallback to minimum
            double ry = input.ry > 0 ? input.ry : 10;

            // Calculate effective lengths
            double KxLx = input.Kx * input.Lx;
            double KyLy = input.Ky * input.Ly;
            double KL_rx = rx > 0 ? KxLx / rx : 0;
            double KL_ry = ry > 0 ? KyLy / ry : 0;
            double KL_r_max = Math.Max(KL_rx, KL_ry);
            if (KL_r_max <= 0) KL_r_max = 1; // Prevent division by zero
            string governing = KL_rx >= KL_ry ? "X-axis" : "Y-axis";

            result.Details.Add(new CalculationStep("Slenderness KxLx/rx", "KxLx/rx", 
                $"{input.Kx:F2}×{input.Lx:F0}/{rx:F1}", KL_rx));
            result.Details.Add(new CalculationStep("Slenderness KyLy/ry", "KyLy/ry",
                $"{input.Ky:F2}×{input.Ly:F0}/{ry:F1}", KL_ry));
            result.Details.Add(new CalculationStep("Governing Slenderness", "",
                governing, KL_r_max));

            // E3-4: Elastic buckling stress
            double Fe = Math.PI * Math.PI * E / (KL_r_max * KL_r_max);
            if (Fe <= 0 || double.IsInfinity(Fe) || double.IsNaN(Fe))
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid elastic buckling stress Fe", "", "", 0));
                return result;
            }

            result.Details.Add(new CalculationStep("Elastic Buckling Stress Fe", "π²E/(KL/r)²",
                $"π²×{E:F0}/({KL_r_max:F1})²", Fe, "MPa"));

            // Calculate Fcr based on slenderness
            double Fcr;
            double QFy_Fe = Q * Fy / Fe;
            string equation;

            if (QFy_Fe <= 2.25)
            {
                // E7-2: Inelastic buckling
                Fcr = Q * Math.Pow(0.658, QFy_Fe) * Fy;
                equation = "E7-2 (Inelastic)";

                result.Details.Add(new CalculationStep("QFy/Fe", "",
                    $"{Q:F3}×{Fy:F0}/{Fe:F1}", QFy_Fe));
                result.Details.Add(new CalculationStep("Condition", "", "QFy/Fe ≤ 2.25", 0) { Result = "Inelastic Buckling" });
            }
            else
            {
                // E7-3: Elastic buckling
                Fcr = 0.877 * Fe;
                equation = "E7-3 (Elastic)";

                result.Details.Add(new CalculationStep("QFy/Fe", "",
                    $"{Q:F3}×{Fy:F0}/{Fe:F1}", QFy_Fe));
                result.Details.Add(new CalculationStep("Condition", "", "QFy/Fe > 2.25", 0) { Result = "Elastic Buckling" });
            }

            result.Details.Add(new CalculationStep($"Critical Stress Fcr ({equation})", "",
                "", Fcr, "MPa"));

            // Nominal strength
            double Pn = Fcr * Ag / 1000; // kN

            result.Details.Add(new CalculationStep("Nominal Strength Pn", "Fcr × Ag",
                $"{Fcr:F1}×{Ag:F0}/1000", Pn, "kN"));

            // Design strength
            double phi = DesignFactors.Phi_Compression;
            double omega = DesignFactors.Omega_Compression;
            result.Capacity = DesignFactors.GetCapacity(Pn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? "φPn" : "Pn/Ω",
                input.Method == DesignMethod.LRFD ? $"{phi:F2}×Pn" : $"Pn/{omega:F2}",
                "", result.Capacity, "kN"));

            result.CalculateRatio();

            result.Details.Add(new CalculationStep("Demand/Capacity", "Pr/φPn",
                $"{result.Demand:F1}/{result.Capacity:F1}", result.Ratio));

            return result;
        }

        /// <summary>
        /// Check slenderness limit (E2: KL/r ≤ 200)
        /// </summary>
        public static CheckDetail CheckSlendernessLimit(AnalyzedMember input)
        {
            if (input.rx <= 0 || input.ry <= 0)
            {
                return new CheckDetail
                {
                    Name = "Slenderness Limit",
                    Section = "E2",
                    Status = CheckStatus.NotApplicable
                };
            }

            double KxLx = input.Kx * input.Lx;
            double KyLy = input.Ky * input.Ly;
            double KL_r_max = Math.Max(KxLx / input.rx, KyLy / input.ry);

            var result = new CheckDetail
            {
                Name = "Slenderness Limit",
                Section = "E2",
                Demand = KL_r_max,
                Capacity = 200
            };

            result.Ratio = KL_r_max / 200;
            result.Status = KL_r_max <= 200 ? CheckStatus.Pass : CheckStatus.Warning;

            result.Details.Add(new CalculationStep("Max KL/r", "", "", KL_r_max));
            result.Details.Add(new CalculationStep("Limit", "", "Recommended ≤ 200", 200));

            return result;
        }
        /// <summary>
        /// Determine nominal compression strength for Torsional and Flexural-Torsional Buckling (E4)
        /// Applicable to Singly Symmetric and Asymmetric Members, and certain Doubly Symmetric members (e.g. Cruciform, Built-up)
        /// Effectively required for Angles and Tees.
        /// </summary>
        public static CheckDetail CalculateTorsionalBuckling(AnalyzedMember input, CompressionClassification classification)
        {
            var result = new CheckDetail
            {
                Name = "Torsional/Flexural-Torsional Buckling",
                Section = "E4",
                Demand = Math.Abs(input.Pr) / 1000.0 // N -> kN
            };

            // Needs x0, y0, ro, H
            double x0 = input.x0;
            double y0 = input.y0;
            double ro = input.ro;
            double H = input.H;
            double Cw = input.Cw;
            double J = input.J;
            double E = input.E;
            double G = input.G;
            double Fy = input.Fy;
            double Ag = input.Ag;




            double Ix = input.Ix;
            double Iy = input.Iy;
            double KxLx = input.Kx * input.Lx;
            double KyLy = input.Ky * input.Ly;

            if (E <= 0 || G <= 0 || Fy <= 0 || Ag <= 0 || input.Length <= 0 || ro <= 0 || input.rx <= 0 || input.ry <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid section data for E4 check", "", "", 0));
                return result;
            }
            
            // Critical Fe calculation
            double Fe = 0;
            string eq = "";
            
            // Calculate elastic flexural buckling stresses needed for E4-5
            double klrx = Math.Max(KxLx / input.rx, 1e-9);
            double klry = Math.Max(KyLy / input.ry, 1e-9);
            double Fex = Math.PI * Math.PI * E / Math.Pow(klrx, 2);
            double Fey = Math.PI * Math.PI * E / Math.Pow(klry, 2);
            
            // Torsional Unbraced Length (Lz)
            // If explicit KxLx/KyLy is used for flexure, we need KzLz for torsion.
            // STAAD maps UNT/UNB to Lb usually. 
            // For now, use Lb (LTB unbraced length) as a proxy for Torsional unbraced length Lz, 
            // because lateral bracing usually restrains twist too.
            // If unavailable, fall back to KyLy (weak axis) or Length.
            
            double Lz = input.Lb > 0 ? input.Lb : (input.Ly > 0 ? input.Ly : input.Length);
            // Default Kz=1.0 unless we map it (we haven't mapped Kz explicitly yet, assuming Kz=1)
            double KzLz = 1.0 * Lz; 
            
            // Fez = (pi^2 E Cw / Lz^2 + G J) * (1 / (Ag ro^2))
            double Fez = (Math.PI*Math.PI*E*Cw/Math.Pow(KzLz, 2) + G*J) / (Ag * ro*ro); 
            
            if (Double.IsInfinity(Fex)) Fex = 1e9;
            if (Double.IsInfinity(Fey)) Fey = 1e9;
            
            bool isSinglySymmetric = input.IsAngle || input.IsTee || input.IsChannel || input.IsSinglySymmetricI;
            
            if (isSinglySymmetric)
            {
                 // E4-5: Fe = ( (Fe_sym + Fez) / 2H ) * ...
                 // Need to identify Fe_sym (Elastic buckling stress about axis of symmetry)
                 // Note: Flexural-Torsional buckling involves flexure ABOUT the symmetry axis interacting with torsion.
                 
                double Fe_sym = 0;
                
                if (input.IsAngle)
                {
                    // Principal-axis properties are not available in this path. Use the
                    // smaller geometric-axis elastic buckling stress as a conservative
                    // approximation for the angle symmetry-axis term.
                    Fe_sym = Math.Min(Fex, Fey); // Conservative assumption
                }
                else if (input.IsTee)
                {
                    // Tee: Symmetry axis is Axis 2 (Minor/Vertical).
                    // We need Flexural Stress ABOUT Symmetry Axis -> Fey.
                    Fe_sym = Fey;
                }
                else if (input.IsSinglySymmetricI)
                {
                    // Singly Symmetric I-Shape: Symmetry axis is Y (Minor).
                    // We need Flexural Stress ABOUT Symmetry Axis -> Fey.
                    Fe_sym = Fey;
                }
                else
                {
                    // Channel: Symmetry axis is Axis 3 (Major/Perp to web).
                    // We need Flexural Stress ABOUT Symmetry Axis -> Fex.
                    Fe_sym = Fex;
                }

                
                if (H == 0) H = 1.0; // Safety
                
                double term1 = (Fe_sym + Fez) / (2 * H);
                double term2_inner = 1 - 4 * Fe_sym * Fez * H / Math.Pow(Fe_sym + Fez, 2);
                double term2 = 1.0 - Math.Sqrt(Math.Max(0, term2_inner));
                
                Fe = term1 * term2;
                eq = "E4-5 (Flexural-Torsional)";
                
                result.Details.Add(new CalculationStep("Fez", "π²ECw/Lz² + GJ...", "", Fez, "MPa"));
                result.Details.Add(new CalculationStep("Fe_sym", "Axis of Sym Elastic Stress", "", Fe_sym, "MPa"));
                result.Details.Add(new CalculationStep("H", "1 - (x0²+y0²)/ro²", "", H));
            }
            else
            {
                // Doubly Symmetric (E4-4): Fe = Fez (Torsional Buckling only)
                // (Assuming weak axis flexural buckling is checked separately in E3, this handles the Torsional mode)
                Fe = Fez;
                eq = "E4-4 (Doubly Symmetric Torsion)";
                result.Details.Add(new CalculationStep("Fez", "π²ECw/Lz² + GJ...", "", Fez, "MPa"));
            }

            result.Details.Add(new CalculationStep("Elastic Stress Fe (FT)", eq, "", Fe, "MPa"));
            if (double.IsNaN(Fe) || double.IsInfinity(Fe) || Fe <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid elastic buckling stress Fe for E4 check", "", "", 0));
                return result;
            }

            // Calculate Fcr from Fe (Same as E3)
            double Fcr;
            double Q = classification?.Q ?? 1.0;
            double QFy_Fe = Q * Fy / Fe;
            
            if (QFy_Fe <= 2.25)
            {
                Fcr = Q * Math.Pow(0.658, QFy_Fe) * Fy;
                eq = "E7-2";
            }
            else
            {
                Fcr = 0.877 * Fe;
                eq = "E7-3";
            }
            result.Details.Add(new CalculationStep("Critical Stress Fcr", eq, "", Fcr, "MPa"));

            double Pn = Fcr * Ag / 1000;
            
            result.Details.Add(new CalculationStep("Nominal Strength Pn", "Fcr × Ag", "", Pn, "kN"));

            double phi = DesignFactors.Phi_Compression;
            double omega = DesignFactors.Omega_Compression;
            result.Capacity = DesignFactors.GetCapacity(Pn, phi, omega, input.Method);
            result.CalculateRatio();
            
            result.Details.Add(new CalculationStep("Ratio", "", $"{result.Demand:F2}/{result.Capacity:F2}", result.Ratio));

            return result;
        }
        /// <summary>
        /// Determine nominal compression strength for Single Angles (E5)
        /// Uses effective slenderness (KL/r)eff to account for eccentricity
        /// </summary>
        public static CheckDetail CalculateSingleAngleCompression(
            AnalyzedMember input,
            CompressionClassification classification = null)
        {
            var result = new CheckDetail
            {
                Name = "Axial Compression (Single Angle)",
                Section = "E5",
                Demand = Math.Abs(input.Pr) / 1000.0 // N -> kN
            };

            // E5 requires geometric axis radius of gyration (rx)
            // Assuming input.rx is geometric axis (parallel to leg) as confirmed against STAAD output
            double rx = input.rx > 0 ? input.rx : 10;
            double L = input.Length; // Member length between supports
            double L_rx = L / rx;

            double KL_r_eff;
            string calcNote = "";

            // E5 equations for effective slenderness
            // Assuming standard case: Connected through one leg (most common)
            // Eq E5-1 and E5-2
            if (L_rx <= 80)
            {
                // Eq E5-1: 72 + 0.75(L/rx)
                KL_r_eff = 72 + 0.75 * L_rx;
                calcNote = "E5-1 (L/r ≤ 80)";
            }
            else
            {
                // Eq E5-2: 32 + 1.25(L/rx)
                KL_r_eff = 32 + 1.25 * L_rx;
                calcNote = "E5-2 (L/r > 80)";
            }

            // Limit KL/r <= 200 is checked separately in Slenderness Limit, but Fcr behaves well
            
            result.Details.Add(new CalculationStep("Geometric Slenderness L/rx", "L/rx", 
                $"{L:F0}/{rx:F1}", L_rx));
            result.Details.Add(new CalculationStep("Effective Slenderness (KL/r)eff", calcNote, 
                "", KL_r_eff));

            // Now Calculate Fcr using this effective slenderness (same logical flow as E3)
            // E3-4: Elastic buckling stress Fe
            double E = input.E;
            double Fy = input.Fy;
            double Ag = input.Ag;
            if (E <= 0 || Fy <= 0 || Ag <= 0 || L <= 0)
            {
                result.Status = CheckStatus.NotApplicable;
                result.Details.Add(new CalculationStep("Invalid section/material data for E5 check", "", "", 0));
                return result;
            }
            
            double Fe = Math.PI * Math.PI * E / (KL_r_eff * KL_r_eff);
            
            result.Details.Add(new CalculationStep("Elastic Buckling Stress Fe", "π²E/((KL/r)eff)²",
                $"π²×{E:F0}/({KL_r_eff:F1})²", Fe, "MPa"));

            // Calculate Fcr
            double Fcr;
            // AISC E5: local buckling reduction Q should follow Section E7 when applicable.
            double Q = classification?.Q ?? 1.0;
            if (Q <= 0) Q = 1.0;
            result.Details.Add(new CalculationStep(
                "Reduction Factor Q",
                "From compression classification (E7)",
                "",
                Q));

            double QFy_Fe = Q * Fy / Fe;
            string eqStr;

            if (QFy_Fe <= 2.25)
            {
                // E7-2 / E3-2
                Fcr = Q * Math.Pow(0.658, QFy_Fe) * Fy;
                eqStr = "E3-2 (Inelastic)";
            }
            else
            {
                // E7-3 / E3-3
                Fcr = 0.877 * Fe;
                eqStr = "E3-3 (Elastic)";
            }

            result.Details.Add(new CalculationStep($"Critical Stress Fcr", eqStr,
                "", Fcr, "MPa"));

            // Nominal Strength
            double Pn = Fcr * Ag / 1000; // kN
            
            result.Details.Add(new CalculationStep("Nominal Strength Pn", "Fcr × Ag",
                $"{Fcr:F1}×{Ag:F0}/1000", Pn, "kN"));

            // Design Strength
            double phi = DesignFactors.Phi_Compression; // 0.90
            double omega = DesignFactors.Omega_Compression; // 1.67
            result.Capacity = DesignFactors.GetCapacity(Pn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? "φPn" : "Pn/Ω",
                input.Method == DesignMethod.LRFD ? $"{phi:F2}×Pn" : $"Pn/{omega:F2}",
                "", result.Capacity, "kN"));

            result.CalculateRatio();
            
            return result;
        }
    }
}
