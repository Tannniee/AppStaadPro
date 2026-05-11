using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using StaadSteelApp.AISC360;
using StaadSteelApp.Core.Interop;

namespace StaadSteelApp.Core
{
    public partial class StaadToAiscMapper
    {
        private void MapMomentGradientParameters(int memberNo, int loadCase, AnalyzedMember am)
        {
            if (_os == null || am == null || am.Length <= 0) return;

            try
            {
                // OpenSTAAD member distances are in meters.
                double lengthM = am.Length / MToMm;
                double[] stations = new[]
                {
                    0.0,
                    0.25 * lengthM,
                    0.50 * lengthM,
                    0.75 * lengthM,
                    lengthM
                };

                double[] absMajorMoments = new double[stations.Length];
                for (int i = 0; i < stations.Length; i++)
                {
                    if (!OpenStaadOutputAdapter.TryGetIntermediateMemberForcesAtDistance(_os, memberNo, stations[i], loadCase, out double[] f, out string forceError))
                    {
                        WarnOnce($"CbForce:{memberNo}:{loadCase}:{i}", $"Member {memberNo} LC {loadCase}: failed to read intermediate forces for Cb. {forceError}");
                        return;
                    }

                    // Local Mz is used as major-axis moment in this app mapping.
                    absMajorMoments[i] = (f.Length > 5) ? Math.Abs(f[5]) : 0.0;
                }

                double mMax = 0.0;
                for (int i = 0; i < absMajorMoments.Length; i++)
                    if (absMajorMoments[i] > mMax) mMax = absMajorMoments[i];

                if (mMax <= 1e-9) return;

                double MA = absMajorMoments[1];
                double MB = absMajorMoments[2];
                double MC = absMajorMoments[3];
                double denominator = 2.5 * mMax + 3.0 * MA + 4.0 * MB + 3.0 * MC;
                if (denominator <= 1e-9) return;

                // AISC Cb = 12.5 Mmax / (2.5Mmax + 3MA + 4MB + 3MC)
                double cb = 12.5 * mMax / denominator;
                if (double.IsNaN(cb) || double.IsInfinity(cb) || cb <= 0) return;

                // Keep conservative lower bound at 1.0; AISC practical upper cap is 3.0.
                am.Cb = Math.Max(1.0, Math.Min(3.0, cb));
            }
            catch (Exception ex)
            {
                WarnOnce($"Cb:{memberNo}:{loadCase}", $"Member {memberNo} LC {loadCase}: failed to map moment gradient (Cb). Default Cb will be used.", ex);
            }
        }

        private void MapSectionProperties(int memberNo, AnalyzedMember am, double distMM)
        {
            // Use GetSectionPropertyValuesEx to handle Tapered Sections correctly
            // Params: (long RefNo, long* Type, double* Props)
            if (!OpenStaadPropertyAdapter.TryGetBeamSectionPropertyRefNo(_os, memberNo, out int refNo, out string refError))
            {
                WarnOnce($"BeamPropRef:{memberNo}", $"Member {memberNo}: GetBeamSectionPropertyRefNo failed. Section properties cannot be mapped. {refError}");
                return;
            }

            if (refNo <= 0)
            {
                WarnOnce($"BeamPropRef:{memberNo}", $"Member {memberNo}: GetBeamSectionPropertyRefNo returned {refNo}. Section properties cannot be mapped.");
                return;
            }

            int lRetType = 0;
            double[] arrProps = new double[25]; 

            if (OpenStaadPropertyAdapter.TryGetSectionPropertyValuesEx(_os, refNo, out lRetType, out arrProps, out string valuesError))
            {
                arrProps = arrProps ?? new double[25];
            }
            else
            {
                // Fallback if Ex fails or not supported
                WarnOnce($"GetSectionPropertyValuesEx:{refNo}", $"Member {memberNo} (ref {refNo}): GetSectionPropertyValuesEx failed. Section properties may be incomplete. {valuesError}");
                lRetType = 0; // Unknown
            }

            bool isTapered = (lRetType == 680);

            if (isTapered)
            {
                am.IsBuiltUp = true;
                
                // Tapered I params (Meters)
                double H1 = arrProps[0]; // Start Depth
                double Tw = arrProps[1]; // Web Thk
                double H2 = arrProps[2]; // End Depth
                double Bf = arrProps[3]; // Top Flange Width
                double Tf = arrProps[4]; // Top Flange Thk
                double Bfb = arrProps[5]; // Bot Flange Width
                double Tfb = arrProps[6]; // Bot Flange Thk

                if (H1 <= 0 || H2 <= 0 || Tw <= 0 || Bf <= 0 || Tf <= 0)
                {
                    WarnOnce($"TaperedDims:{refNo}",
                        $"Member {memberNo} (ref {refNo}): tapered section dims look invalid (H1={H1}, H2={H2}, Tw={Tw}, Bf={Bf}, Tf={Tf}).");
                }

                // Interpolate Depth at distMM
                // distMM is in mm, Length in mm
                double ratio = 0;
                if (am.Length > 0) ratio = Math.Max(0, Math.Min(1, distMM / am.Length));
                
                double H_at = H1 + (H2 - H1) * ratio;

                // Convert to mm
                am.d = H_at * MToMm;
                am.tw = Tw * MToMm;
                am.bf = Bf * MToMm;
                am.tf = Tf * MToMm;
                am.bfb = Bfb * MToMm;
                am.tfb = Tfb * MToMm;

                // Name
                // Format: I-{Depth}x{Width}x{WebThk}x{FlangeThk}x{BotWidth}x{BotThk}
                double h1_mm = H1 * MToMm;
                double h2_mm = H2 * MToMm;
                double bf_mm = Bf * MToMm;
                double tf_mm = Tf * MToMm;
                double tw_mm = Tw * MToMm;
                double bfb_mm = Bfb * MToMm;
                double tfb_mm = Tfb * MToMm;

                if (Math.Abs(h1_mm - h2_mm) < 1.0)
                {
                    // Prismatic-like Tapered
                    if (bfb_mm > 0 && tfb_mm > 0 && (Math.Abs(bf_mm - bfb_mm) > 0.1 || Math.Abs(tf_mm - tfb_mm) > 0.1))
                        am.SectionName = $"I-{h1_mm:F0}x{bf_mm:F0}x{tw_mm:F0}x{tf_mm:F0}x{bfb_mm:F0}x{tfb_mm:F0}";
                    else
                        am.SectionName = $"I-{h1_mm:F0}x{bf_mm:F0}x{tw_mm:F0}x{tf_mm:F0}";
                }
                else
                {
                    // True Tapered
                    if (bfb_mm > 0 && tfb_mm > 0 && (Math.Abs(bf_mm - bfb_mm) > 0.1 || Math.Abs(tf_mm - tfb_mm) > 0.1))
                        am.SectionName = $"I-({h1_mm:F0}-{h2_mm:F0})x{bf_mm:F0}x{tw_mm:F0}x{tf_mm:F0}x{bfb_mm:F0}x{tfb_mm:F0}";
                    else
                        am.SectionName = $"I-({h1_mm:F0}-{h2_mm:F0})x{bf_mm:F0}x{tw_mm:F0}x{tf_mm:F0}";
                }
            }
            else
            {
                // Non-tapered sections use GetSectionPropertyValues because it provides
                // reliable Ag/Ix/Iy values for catalog and table-based properties.
                
                if (!OpenStaadPropertyAdapter.TryGetSectionPropertyValues(_os, refNo, out double[] sectionValues, out string legacyValuesError))
                {
                    WarnOnce($"GetSectionPropertyValues:{refNo}", $"Member {memberNo} (ref {refNo}): GetSectionPropertyValues failed. Section properties may be incomplete. {legacyValuesError}");
                    return;
                }

                double widthM = sectionValues.Length > 0 ? sectionValues[0] : 0.0;
                double depthM = sectionValues.Length > 1 ? sectionValues[1] : 0.0;
                double areaM2 = sectionValues.Length > 2 ? sectionValues[2] : 0.0;
                double iyM4 = sectionValues.Length > 6 ? sectionValues[6] : 0.0;
                double izM4 = sectionValues.Length > 7 ? sectionValues[7] : 0.0;
                double tfM = sectionValues.Length > 8 ? sectionValues[8] : 0.0;
                double twM = sectionValues.Length > 9 ? sectionValues[9] : 0.0;

                if (widthM <= 0 || depthM <= 0)
                {
                    WarnOnce($"PrismaticDims:{refNo}",
                        $"Member {memberNo} (ref {refNo}): section dims returned from STAAD look invalid (W={widthM}, D={depthM}, Tf={tfM}, Tw={twM}).");
                }
                // Tf and Tw can be 0 for solid sections or generic PRISMATIC shapes.
                // We shouldn't throw a blocking warning for this.
                else if (tfM < 0 || twM < 0)
                {
                    WarnOnce($"PrismaticDimsThk:{refNo}",
                        $"Member {memberNo} (ref {refNo}): section thicknesses returned from STAAD are negative (Tf={tfM}, Tw={twM}).");
                }

                if (areaM2 <= 0 || iyM4 <= 0 || izM4 <= 0)
                {
                    WarnOnce($"PrismaticProps:{refNo}",
                        $"Member {memberNo} (ref {refNo}): section properties returned from STAAD look invalid (A={areaM2}, Iy={iyM4}, Iz={izM4}).");
                }

                // m -> mm conversion
                am.d = depthM * MToMm;
                am.bf = widthM * MToMm;
                am.tf = tfM * MToMm;
                am.tw = twM * MToMm;
                
                // Symmetric I-shape approximation for derived properties.
                am.bfb = am.bf; 
                am.tfb = am.tf;

                am.Ag = areaM2 * M2ToMm2; // m2 -> mm2
                am.Ix = izM4 * M4ToMm4; // m4 -> mm4 (Major is Iz in STAAD output usually, checked against Manual)
                am.Iy = iyM4 * M4ToMm4; // m4 -> mm4
            }

            // Recalculate derived properties from local dimensions so F2 checks use
            // the section modulus at the evaluated location.
            
            double d = am.d;
            double bf = am.bf;
            double tf = am.tf;
            double tw = am.tw;
            double h_web = d - tf - am.tfb;

            // I-shape derived properties require all primary dimensions.
            if (d > 0 && bf > 0 && tf > 0 && tw > 0)
            {
                double y_na = d / 2.0;
                double Iy_top = (tf > 0 && bf > 0) ? (tf * Math.Pow(bf, 3) / 12.0) : 0.0;
                double Iy_bot = (am.tfb > 0 && am.bfb > 0) ? (am.tfb * Math.Pow(am.bfb, 3) / 12.0) : 0.0;
                
                // Tapered sections do not provide dependable section properties through
                // OpenSTAAD, so derive them from plate dimensions. Non-tapered catalog
                // sections keep STAAD database values to preserve fillet effects.
                
                if (isTapered)
                {
                     am.Ag = (bf * tf) + (am.bfb * am.tfb) + (h_web * tw); // Ignore fillets
                     
                     // Neutral axis for singly symmetric sections, measured from the
                     // bottom of the bottom flange.
                     double y_bot_flange = am.tfb / 2.0;
                     double y_web = am.tfb + h_web / 2.0;
                     double y_top_flange = am.tfb + h_web + tf / 2.0;
                     
                     y_na = ((am.bfb * am.tfb * y_bot_flange) + (h_web * tw * y_web) + (bf * tf * y_top_flange)) / am.Ag;
                     
                     // Ix (Major) = Sum(I_yi + A_i * d_i^2)
                     double I_bot = (am.bfb * Math.Pow(am.tfb, 3) / 12.0) + (am.bfb * am.tfb * Math.Pow(y_na - y_bot_flange, 2));
                     double I_web = (tw * Math.Pow(h_web, 3) / 12.0) + (h_web * tw * Math.Pow(y_na - y_web, 2));
                     double I_top = (bf * Math.Pow(tf, 3) / 12.0) + (bf * tf * Math.Pow(y_top_flange - y_na, 2));
                     am.Ix = I_bot + I_web + I_top;
                     
                     // Iy (Minor)
                     // 1/12 tf bf^3 + 1/12 tfb bfb^3 + 1/12 hw tw^3
                     double Iy_web = (h_web * Math.Pow(tw, 3) / 12.0);
                     am.Iy = Iy_top + Iy_bot + Iy_web;
                }

                // Elastic Moduli
                am.Sx = (d > 0) ? am.Ix / Math.Max(y_na, d - y_na) : 0;
                am.Sy = (Math.Max(bf, am.bfb) > 0) ? am.Iy / (Math.Max(bf, am.bfb) / 2.0) : 0;

                // Plastic moduli approximation for singly symmetric I-shapes.
                // Zx = Sum(A_i * |y_i - PNA|)
                // The plastic neutral axis is estimated from equal area above and below.
                double A_half = am.Ag / 2.0;
                double A_bot = am.bfb * am.tfb;
                double y_pna = am.tfb;
                if (A_bot >= A_half) {
                    y_pna = (A_half) / am.bfb; 
                } else {
                    y_pna = am.tfb + (A_half - A_bot) / tw;
                }
                
                // Zx is first moment of area about PNA
                // Sum rectangular block first moments about the plastic neutral axis.
                double Z_bot_flange = am.bfb * am.tfb * Math.Abs(y_pna - am.tfb / 2.0);
                double Z_top_flange = bf * tf * Math.Abs((d - tf / 2.0) - y_pna);
                
                // web above and below PNA
                double hw_below = Math.Max(0, y_pna - am.tfb);
                double hw_above = Math.Max(0, d - tf - y_pna);
                double Z_web = (tw * hw_below * hw_below / 2.0) + (tw * hw_above * hw_above / 2.0);
                
                am.Zx = Z_bot_flange + Z_top_flange + Z_web;

                // Zy
                am.Zy = (tf * Math.Pow(bf, 2) / 4.0) + (am.tfb * Math.Pow(am.bfb, 2) / 4.0) + (h_web * Math.Pow(tw, 2) / 4.0);

                // Torsion Constant J
                am.J = (1.0/3.0 * bf * Math.Pow(tf, 3)) + (1.0/3.0 * am.bfb * Math.Pow(am.tfb, 3)) + (1.0/3.0 * h_web * Math.Pow(tw, 3));

                // Warping constant approximation for singly symmetric I-shapes.
                double h0 = d - (tf + am.tfb) / 2.0;
                am.ho = h0;
                // For doubly/singly symmetric I-shapes, use flange minor inertias to avoid
                // relying on unavailable legacy warping data. This keeps rolled sections from
                // falling back to ry when Cw would otherwise become NaN.
                double cwarDenominator = Iy_top + Iy_bot;
                if (cwarDenominator > 0 && h0 > 0)
                    am.Cw = (Iy_top * Iy_bot / cwarDenominator) * Math.Pow(h0, 2);
                else
                    am.Cw = 0.0;
            }
            else
            {
                // Fallback for non-standard shapes.
                 am.Sx = (d > 0) ? 2 * am.Ix / d : 0;
                 am.Sy = (bf > 0) ? 2 * am.Iy / bf : 0;
                 am.Zx = am.Sx * 1.1; 
                 am.Zy = am.Sy * 1.1;
                 am.J = 0; 
                 am.Cw = 0;

                 WarnOnce($"DerivedPropsFallback:{refNo}",
                     $"Member {memberNo} (ref {refNo}): section dimensions are not positive (d={am.d}, bf={am.bf}, tf={am.tf}, tw={am.tw}). Using fallback derived properties.");
            }
        }

    }
}
