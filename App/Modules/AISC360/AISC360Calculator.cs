 using System;
using StaadSteelApp.Core;

namespace StaadSteelApp.AISC360
{
    /// <summary>
    /// Main calculator for AISC 360-16 design checks
    /// </summary>
    public class AISC360Calculator
    {
        /// <summary>
        /// Perform all AISC 360-16 checks on a member
        /// </summary>
        public static DesignResults PerformAllChecks(AnalyzedMember input)
        {
            var results = new DesignResults
            {
                FrameName = input.FrameName,
                Member = input
            };

            // Internal convention in this engine:
            //   Forces  : N
            //   Moments : N-mm
            // Convert once for threshold checks.
            double mrxKnM = Math.Abs(input.Mrx) / 1e6;
            double mryKnM = Math.Abs(input.Mry) / 1e6;
            double vrxKn = Math.Abs(input.Vrx) / 1000.0;
            double vryKn = Math.Abs(input.Vry) / 1000.0;

            // Calculate derived properties
            input.CalculateDerived();

            // 2. Classification (B4)
            var flexureClass = Classification.ClassifyFlexureForIShape(input); // Simplified for now
            CompressionClassification compClass;
            
            if (input.IsPipe)
            {
                compClass = Classification.ClassifyCompressionForPipe(input);
            }
            else if (input.IsBoxHSS)
            {
                compClass = Classification.ClassifyCompressionForBoxHSS(input);
            }
            else if (input.IsChannel)
            {
                 compClass = Classification.ClassifyCompressionForChannel(input);
            }
            else
            {
                // Default to I-Shape logic for now (covers I, Angle, Tee approx)
                compClass = Classification.ClassifyCompressionForIShape(input);
            }

            // Assign to results
            results.FlexureClass = flexureClass;
            results.CompressionClass = compClass;

            // Compression Check (Chapter E)
            if (input.Pr < 0) // Negative = compression
            {
                if (input.IsAngle)
                {
                    // Run E5 (Single Angle Eccentric)
                    var e5Check = ChapterE.CalculateSingleAngleCompression(input, results.CompressionClass);
                    
                    // Run E4 (Flexural-Torsional) - usually higher capacity (Concentric assumption)
                    var e4Check = ChapterE.CalculateTorsionalBuckling(input, results.CompressionClass);
                    
                    // Logic: The governing check determines the capacity.
                    // We should use the LOWER capacity (Higher Ratio).
                    // User feedback indicates STAAD.Pro behaves closer to a penalized E5-style check here, which can govern over pure E4.
                    
                    if (e4Check.Capacity > 0 && e4Check.Capacity < e5Check.Capacity)
                    {
                        // E4 is more critical (lower capacity)
                        results.CompressionCheck = e4Check;
                        results.CompressionCheck.SubChecks.Add(e5Check);
                        results.CompressionCheck.Messages.Add($"Governed by E4 (Concentric). E5 Capacity: {e5Check.Capacity:F1}");
                    }
                    else
                    {
                        // E5 is more critical (lower capacity) - Standard for single angles
                        results.CompressionCheck = e5Check;
                        if (e4Check.Capacity > 0)
                        {
                            results.CompressionCheck.SubChecks.Add(e4Check);
                            results.CompressionCheck.Messages.Add($"Governed by E5 (Eccentric). E4 Capacity: {e4Check.Capacity:F1}");
                        }
                    }
                }
                else
                {
                    results.CompressionCheck = ChapterE.CalculateCompression(input, results.CompressionClass);
                    
                    // For Tees, Channels, AND I-Shapes (Doubly Symmetric), check Torsional/Flexural-Torsional Buckling (E4)
                    // AISC E4 applies to doubly symmetric members if KzLz > KyLy (torsion governs over flexure)
                    if (!input.IsBoxHSS && !input.IsPipe)
                    {
                        var e4Check = ChapterE.CalculateTorsionalBuckling(input, results.CompressionClass);
                        if (e4Check.Capacity > 0 && e4Check.Capacity < results.CompressionCheck.Capacity)
                        {
                            // E4 governs
                            var e3Check = results.CompressionCheck;
                            results.CompressionCheck = e4Check;
                            results.CompressionCheck.SubChecks.Add(e3Check); // Keep E3 as reference
                            results.CompressionCheck.Messages.Add("Governed by Torsional/Flexural-Torsional Buckling (E4)");
                        }
                    }
                }

                AppendCompressionSlendernessWarning(input, results.CompressionCheck);
            }


            // Tension Check (Chapter D)
            if (input.Pr > 0) // Positive = tension
            {
                results.TensionCheck = CalculateTension(input);
            }


            // Flexure Check (Chapter F)
            if (input.IsPipe)
            {
                // F8 for Round HSS - no LTB, same capacity for both axes
                var pipeFlexure = ChapterF.CalculateFlexureRoundHSS(input);
                if (mrxKnM > 0.001 || mryKnM > 0.001)
                {
                    results.FlexureCheckMajor = pipeFlexure;
                    // For Round HSS, minor axis capacity = major axis capacity
                    if (mryKnM > 0.001)
                    {
                        results.FlexureCheckMinor = new CheckDetail
                        {
                            Name = pipeFlexure.Name + " (Minor)",
                            Section = pipeFlexure.Section,
                            Capacity = pipeFlexure.Capacity,
                            Demand = mryKnM,
                            Status = pipeFlexure.Capacity > 0 && (mryKnM / pipeFlexure.Capacity <= 1.0)
                                ? CheckStatus.Pass
                                : CheckStatus.Fail
                        };
                        results.FlexureCheckMinor.CalculateRatio();
                    }
                }
            }
            else if (input.IsBoxHSS)
            {
                // F7 for Box/Rectangular HSS - no LTB
                if (mrxKnM > 0.001)
                {
                    results.FlexureCheckMajor = ChapterF.CalculateFlexureBoxHSS(input, true);
                }
                if (mryKnM > 0.001)
                {
                    results.FlexureCheckMinor = ChapterF.CalculateFlexureBoxHSS(input, false);
                }
            }
            else
            {
                // F2/F3/F4/F6 (or F5 for slender web) for I-shapes
                if (mrxKnM > 0.001)
                {
                    if (input.IsSinglySymmetricI)
                    {
                        // F4/F5 for Singly Symmetric I-shapes
                        results.FlexureCheckMajor = ChapterF.CalculateFlexureSinglySymmetric(input, results.FlexureClass);
                    }
                    else if (results.FlexureClass != null &&
                        results.FlexureClass.WebStatus == ClassificationStatus.Slender)
                    {
                        results.FlexureCheckMajor = ChapterF.CalculateFlexureSlenderWeb(input, results.FlexureClass);
                    }
                    else if (results.FlexureClass != null &&
                             results.FlexureClass.WebStatus == ClassificationStatus.Noncompact)
                    {
                        results.FlexureCheckMajor = ChapterF.CalculateFlexureNoncompactWeb(input, results.FlexureClass);
                    }
                    else
                    {
                        results.FlexureCheckMajor = ChapterF.CalculateFlexureMajor(input, results.FlexureClass);
                    }
                }
                if (mryKnM > 0.001)
                {
                    results.FlexureCheckMinor = ChapterF.CalculateFlexureMinor(input, results.FlexureClass);
                }
            }


            // Shear Check (Chapter G)
            if (vrxKn > 0.001 || vryKn > 0.001)
            {
                if (input.IsPipe)
                {
                    // G5 for Round HSS
                    results.ShearCheckMajor = ChapterG.CalculateShearRoundHSS(input);
                }
                else if (input.IsBoxHSS)
                {
                    // G4 for Box/Rectangular HSS
                    if (vrxKn > 0.001)
                        results.ShearCheckMajor = ChapterG.CalculateShearBoxHSS(input, true);
                    if (vryKn > 0.001)
                        results.ShearCheckMinor = ChapterG.CalculateShearBoxHSS(input, false);
                }
                else
                {
                    // G2/G7 for I-shapes and Channels
                    if (vrxKn > 0.001)
                        results.ShearCheckMajor = ChapterG.CalculateShearMajor(input);
                    if (vryKn > 0.001)
                        results.ShearCheckMinor = ChapterG.CalculateShearMinor(input);
                }
            }

            // Interaction Check (Chapter H)
            var axialCheck = results.CompressionCheck ?? results.TensionCheck;
            double trKnM = Math.Abs(input.Tr) / 1e6; // N-mm -> kN-m
            
            // 1. Calculate Standard P-M Interaction (H1-1)
            var pmCheck = ChapterH.CalculateInteraction(
                input, axialCheck, results.FlexureCheckMajor, results.FlexureCheckMinor);
            
            results.InteractionCheck = pmCheck;
            
            // 2. Combined Torsion Interaction (H3) for HSS
            // If HSS and Torsion is significant, H3-6 replaces H1-1 as the governing interaction equation
            if ((input.IsPipe || input.IsBoxHSS) && trKnM > 0.1) 
            {
                var h3Check = ChapterH.CalculateTorsionInteraction(
                    input, axialCheck, results.FlexureCheckMajor, results.FlexureCheckMinor, 
                    results.ShearCheckMajor, results.ShearCheckMinor);
                
                results.TorsionCheck = h3Check;
                
                // Set H3-6 as the primary Interaction Check for reporting and governance
                results.InteractionCheck = h3Check;
            }


            // 3. Torsion on Open Sections (Warning only)
            // If checking Torsion on I-Shape/Channel/Angle, warn about Warping which is not fully covered
            bool isOpenSection = !input.IsBoxHSS && !input.IsPipe;
            if (isOpenSection && trKnM > 1.0) // Threshold: 1.0 kN-m
            {
                var warningCheck = new CheckDetail 
                { 
                    Name = "Torsion Warning", 
                    Ratio = 0, 
                    Status = CheckStatus.Warning,
                    Section = "DG9"
                };
                
                warningCheck.Details.Add(new CalculationStep
                {
                    Description = "Significant Torsion on Open Section",
                    Formula = "Tr > 1.0 kN-m",
                    Result = $"{trKnM:F1} kN-m"
                });
                warningCheck.Details.Add(new CalculationStep("Action", "Check Warping", "See AISC DG9", 0));
                
                if (results.TorsionCheck == null)
                {
                    results.TorsionCheck = warningCheck;
                }
                else
                {
                    // Append warning to existing Torsion Check
                    results.TorsionCheck.Status = CheckStatus.Warning;
                    results.TorsionCheck.Details.Add(new CalculationStep("WARNING", "Tr > 1.0 kN-m", "Check Warping", trKnM));
                }
            }



            // Serviceability Check (Chapter L)
            if (input.MaxDeflection > 0)

            {
                results.ServiceabilityCheck = ChapterL.CheckDeflection(input);
            }


            // Determine overall status
            results.DetermineOverall();

            return results;
        }

        /// <summary>
        /// Build AnalyzedMember from STAAD data
        /// </summary>
        // Legacy build-from-external-model path removed during the STAAD migration.

        /// <summary>
        /// Calculate tension strength (Chapter D - simplified)
        /// </summary>
        private static CheckDetail CalculateTension(AnalyzedMember input)
        {
            var result = new CheckDetail
            {
                Name = "Axial Tension",
                Section = "D2",
                Demand = input.Pr / 1000.0 // N -> kN
            };

            double Ag = input.Ag;
            double Fy = input.Fy;

            // D2(a): Yielding
            double Pn = Fy * Ag / 1000; // kN

            result.Details.Add(new CalculationStep("Nominal Strength Pn", "Fy × Ag",
                $"{Fy:F0}×{Ag:F0}/1000", Pn, "kN"));

            double phi = DesignFactors.Phi_Tension_Yield;
            double omega = DesignFactors.Omega_Tension_Yield;
            result.Capacity = DesignFactors.GetCapacity(Pn, phi, omega, input.Method);

            result.Details.Add(new CalculationStep(
                input.Method == DesignMethod.LRFD ? "φPn" : "Pn/Ω",
                "", "", result.Capacity, "kN"));

            result.CalculateRatio();

            // Check Slenderness (L/r < 300) - Note: This is a recommendation (User Note D1)
            double r_min = Math.Min(input.rx, input.ry);
            if (r_min > 0)
            {
                double L_r = input.Length / r_min;
                result.Details.Add(new CalculationStep("Slenderness L/r", "Rec. ≤ 300", 
                    $"{input.Length:F0}/{r_min:F1}", L_r));
                
                if (L_r > 300)
                {
                    result.Messages.Add("Warning: Slenderness L/r > 300 (AISC 360-16 Sec. D1)");
                    // Does not fail the check, just warns
                }
            }

            return result;
        }

        private static void AppendCompressionSlendernessWarning(AnalyzedMember input, CheckDetail compressionCheck)
        {
            if (input == null || compressionCheck == null) return;

            var slenderness = ChapterE.CheckSlendernessLimit(input);
            if (slenderness == null || slenderness.Status == CheckStatus.NotApplicable) return;

            foreach (var step in slenderness.Details)
            {
                compressionCheck.Details.Add(step);
            }

            if (slenderness.Status == CheckStatus.Warning)
            {
                compressionCheck.Messages.Add($"Warning: KL/r = {slenderness.Demand:F1} > 200 (AISC 360-16 E2).");
            }
        }

    }
}
