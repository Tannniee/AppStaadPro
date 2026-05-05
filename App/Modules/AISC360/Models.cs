using System;
using System.Collections.Generic;

namespace StaadSteelApp.AISC360
{
    #region Enums

    public enum CheckStatus
    {
        Pass,
        Fail,
        Warning,
        NotRun,
        NotApplicable
    }

    public enum ClassificationStatus
    {
        Compact,
        Noncompact,
        Slender
    }

    public enum DesignMethod
    {
        LRFD,
        ASD
    }

    #endregion

    #region Calculation Step Model

    /// <summary>
    /// Represents a single calculation step for detailed output
    /// </summary>
    public class CalculationStep
    {
        public string Description { get; set; }
        public string Formula { get; set; }
        public string Substitution { get; set; }
        public string Result { get; set; }
        public string Unit { get; set; }

        public CalculationStep() { }

        public CalculationStep(string desc, string formula, string sub, double result, string unit = "")
        {
            Description = desc;
            Formula = formula;
            Substitution = sub;
            Result = result.ToString("F2");
            Unit = unit;
        }

        public override string ToString()
        {
            string line = $"  {Description}";
            if (!string.IsNullOrEmpty(Formula))
                line += $": {Formula}";
            if (!string.IsNullOrEmpty(Substitution))
                line += $" = {Substitution}";
            if (!string.IsNullOrEmpty(Result))
                line += $" = {Result}";
            if (!string.IsNullOrEmpty(Unit))
                line += $" {Unit}";
            return line;
        }
    }

    #endregion

    #region Check Detail Model

    /// <summary>
    /// Result of a single design check (e.g., Chapter E Compression)
    /// </summary>
    public class CheckDetail
    {
        public string Name { get; set; }        // e.g., "Axial Compression"
        public string Section { get; set; }     // e.g., "E3"
        public double Capacity { get; set; }    // Design capacity (φPn or Pn/Ω)
        public double Demand { get; set; }      // Required strength (Pr)
        public double Ratio { get; set; }       // D/C ratio
        public CheckStatus Status { get; set; }
        public List<CalculationStep> Details { get; set; } = new List<CalculationStep>();
        public List<string> Messages { get; set; } = new List<string>();
        public List<CheckDetail> SubChecks { get; set; } = new List<CheckDetail>();


        public void CalculateRatio()
        {
            if (Capacity <= 0)
            {
                Ratio = Demand > 0 ? double.PositiveInfinity : 0;
                Status = Demand > 0 ? CheckStatus.Fail : CheckStatus.NotApplicable;
            }
            else
            {
                Ratio = Demand / Capacity;
                Status = Ratio <= 1.0 ? CheckStatus.Pass : CheckStatus.Fail;
            }
        }

        public string GetStatusSymbol()
        {
            switch (Status)
            {
                case CheckStatus.Pass: return "OK";
                case CheckStatus.Fail: return "NG";
                case CheckStatus.Warning: return "WARN";
                default: return "-";
            }
        }
    }

    #endregion

    #region Section Classification

    public class FlexureClassification
    {
        public ClassificationStatus FlangeStatus { get; set; }
        public ClassificationStatus WebStatus { get; set; }
        public ClassificationStatus SectionStatus { get; set; }

        // Slenderness ratios
        public double Lambda_f { get; set; }   // Flange slenderness (bf/2tf)
        public double Lambda_pf { get; set; }  // Compact limit for flange
        public double Lambda_rf { get; set; }  // Noncompact limit for flange

        public double Lambda_w { get; set; }   // Web slenderness (h/tw)
        public double Lambda_pw { get; set; }  // Compact limit for web
        public double Lambda_rw { get; set; }  // Noncompact limit for web

        public List<CalculationStep> Details { get; set; } = new List<CalculationStep>();
    }

    public class CompressionClassification
    {
        public ClassificationStatus FlangeStatus { get; set; }
        public ClassificationStatus WebStatus { get; set; }
        public bool IsSectionSlender { get; set; }

        public double Lambda_f { get; set; }
        public double Lambda_rf { get; set; }
        public double Lambda_w { get; set; }
        public double Lambda_rw { get; set; }

        public double Q { get; set; } = 1.0;   // Total reduction factor
        public double Qs { get; set; } = 1.0;  // Unstiffened element factor
        public double Qa { get; set; } = 1.0;  // Stiffened element factor

        public List<CalculationStep> Details { get; set; } = new List<CalculationStep>();
    }

    #endregion

    #region Member Input (Analyzed)

    /// <summary>
    /// Input data for AISC 360 calculations.
    /// Internal unit convention used by this engine:
    /// Forces in N, moments in N-mm, lengths in mm, stresses in MPa.
    /// </summary>
    public class AnalyzedMember
    {
        // Identity
        public string FrameName { get; set; }
        public string SectionName { get; set; }
        public DesignMethod Method { get; set; } = DesignMethod.LRFD;

        // Forces (N, N-mm)
        public double Pr { get; set; }   // Required axial (negative = compression)
        public double Mrx { get; set; }  // Required moment major axis
        public double Mry { get; set; }  // Required moment minor axis
        public double Vrx { get; set; }  // Required shear major axis
        public double Vry { get; set; }  // Required shear minor axis

        // Material (MPa)
        public double Fy { get; set; }
        public double Fu { get; set; }
        public double E { get; set; } = 200000;
        public double G { get; set; } = 77000;
        



        // Section Dimensions (mm)
        public double d { get; set; }    // Total depth
        public double bf { get; set; }   // Top flange width
        public double tf { get; set; }   // Top flange thickness
        public double tw { get; set; }   // Web thickness
        public double bfb { get; set; }  // Bottom flange width
        public double tfb { get; set; }  // Bottom flange thickness
        public double ho { get; set; }   // Distance between flange centroids

        // Section Properties (mm, mm², mm³, mm⁴)
        public double Ag { get; set; }   // Gross area
        public double Ix { get; set; }   // Moment of inertia X
        public double Iy { get; set; }   // Moment of inertia Y
        public double Sx { get; set; }   // Section modulus X
        public double Sy { get; set; }   // Section modulus Y
        public double Zx { get; set; }   // Plastic modulus X
        public double Zy { get; set; }   // Plastic modulus Y
        public double rx { get; set; }   // Radius of gyration X
        public double ry { get; set; }   // Radius of gyration Y
        public double J { get; set; }    // Torsional constant
        public double Cw { get; set; }   // Warping constant

        // Pipe/Round HSS Properties
        public bool IsPipe { get; set; }     // True for Pipe/Round HSS sections
        public double D_pipe { get; set; }   // Outside diameter (mm)
        public double t_pipe { get; set; }   // Wall thickness (mm)

        // Box/Rectangular HSS Properties
        public bool IsBoxHSS { get; set; }   // True for rectangular HSS/Tube sections
        public double Ht { get; set; }       // Outside height (mm)
        public double B { get; set; }        // Outside width (mm)
        public double tdes { get; set; }     // Design wall thickness (mm)

        // Channel Properties
        public bool IsChannel { get; set; }  // True for C-section/Channel sections

        // Angle Properties
        public bool IsAngle { get; set; }

        // Tee Properties
        public bool IsTee { get; set; }

        // Serviceability
        public double MaxDeflection { get; set; } // mm

        // Torsion Demand
        public double Tr { get; set; }   // Required torsion (N-mm)


        // Design Parameters
        public double Length { get; set; }  // Frame length (mm)
        public double Lb { get; set; }      // Unbraced length LTB (mm)
        public double Lx { get; set; }      // Unbraced length strong axis (mm)
        public double Ly { get; set; }      // Unbraced length weak axis (mm)
        public double Kx { get; set; } = 1.0;
        public double Ky { get; set; } = 1.0;
        public double Cb { get; set; } = 1.0;
        public double CmMajor { get; set; } = 1.0;
        public double CmMinor { get; set; } = 1.0;
        public double ParamUNL { get; set; }  // Raw UNL from STAAD (mm)
        public double ParamUNT { get; set; }  // Raw UNT from STAAD (mm)
        public double ParamUNB { get; set; }  // Raw UNB from STAAD (mm)
        public string LbSource { get; set; }  // Which parameter controlled Lb

        // Derived (calculated in preprocessing)
        public double hc { get; set; }     // Clear dist between flanges (d - tf - tfb)
        public double bf_2tf { get; set; } // Flange slenderness
        public double h_tw { get; set; }   // Web slenderness
        public double rts { get; set; }    // Effective radius of gyration for LTB

        // Classification properties
        public bool IsBuiltUp { get; set; } // True for Built-up/Welded/Tapered sections
        public bool IsSinglySymmetricI { get; set; } // True for I-shapes where top and bottom flanges differ
        
        // Torsional Buckling Properties (E4)


        public double x0 { get; set; }
        public double y0 { get; set; }
        public double ro { get; set; }
        public double H { get; set; }


        public void CalculateDerived()
        {
            if (Ag > 0)
            {
                if (rx <= 0 && Ix > 0)
                    rx = Math.Sqrt(Ix / Ag);
                if (ry <= 0 && Iy > 0)
                    ry = Math.Sqrt(Iy / Ag);
            }

            // E4 needs torsional-buckling geometry terms (x0, y0, ro, H).
            // We currently don't map shear-center offsets from STAAD, so only backfill the
            // doubly-symmetric case (x0=y0=0) to avoid unconservative assumptions for Tee/Channel/Angle.
            if (ro <= 0 && Ag > 0 && Ix > 0 && Iy > 0 && !IsAngle && !IsChannel && !IsTee)
            {
                // AISC ro^2 = (Ix+Iy)/Ag + x0^2 + y0^2. For doubly symmetric sections, x0=y0=0.
                if (x0 == 0 && y0 == 0)
                {
                    ro = Math.Sqrt((Ix + Iy) / Ag);
                    if (H <= 0) H = 1.0; // H = 1 - (x0^2+y0^2)/ro^2
                }
            }

            // Override plastic section modulus for closed shapes.
            // StaadToAiscMapper currently derives Zx/Zy with an I-shape assumption, which is not valid for HSS/Pipe.
            if (IsPipe && D_pipe > 0 && t_pipe > 0)
            {
                double Di = Math.Max(0.0, D_pipe - 2.0 * t_pipe);

                // Plastic modulus for circular annulus: Z = (Do^3 - Di^3) / 6
                double Z = (Math.Pow(D_pipe, 3) - Math.Pow(Di, 3)) / 6.0;
                if (Z > 0)
                {
                    Zx = Z;
                    Zy = Z;
                }

                // Backfill Ix/Iy/Sx/Sy if STAAD mapping did not provide them.
                if (Ix <= 0 || Iy <= 0)
                {
                    double I = Math.PI * (Math.Pow(D_pipe, 4) - Math.Pow(Di, 4)) / 64.0;
                    if (Ix <= 0) Ix = I;
                    if (Iy <= 0) Iy = I;
                }
                if (Sx <= 0 && Ix > 0) Sx = 2.0 * Ix / D_pipe;
                if (Sy <= 0 && Iy > 0) Sy = 2.0 * Iy / D_pipe;
            }
            else if (IsBoxHSS && Ht > 0 && B > 0 && tdes > 0)
            {
                double Hi = Math.Max(0.0, Ht - 2.0 * tdes);
                double Bi = Math.Max(0.0, B - 2.0 * tdes);

                // Plastic modulus for rectangular tube (outer minus inner):
                // Zx = (B*H^2 - Bi*Hi^2) / 4
                // Zy = (H*B^2 - Hi*Bi^2) / 4
                double Zmaj = (B * Ht * Ht - Bi * Hi * Hi) / 4.0;
                double Zmin = (Ht * B * B - Hi * Bi * Bi) / 4.0;
                if (Zmaj > 0) Zx = Zmaj;
                if (Zmin > 0) Zy = Zmin;

                if (Sx <= 0 && Ix > 0) Sx = 2.0 * Ix / Ht;
                if (Sy <= 0 && Iy > 0) Sy = 2.0 * Iy / B;
            }

            if (IsBoxHSS && Ht > 0 && B > 0 && tdes > 0)
            {
                // Box HSS slenderness (inside dimensions)
                hc = Ht - 3 * tdes;  // Clear height
                bf_2tf = (B - 3 * tdes) / tdes;  // Flange slenderness b/t
                h_tw = hc / tdes;  // Web slenderness h/t
            }
            else
            {
                hc = d - tf - tfb;
                double bf_2tf_top = bf > 0 && tf > 0 ? bf / (2 * tf) : 0;
                double bf_2tf_bot = bfb > 0 && tfb > 0 ? bfb / (2 * tfb) : 0;
                bf_2tf = Math.Max(bf_2tf_top, bf_2tf_bot);
                h_tw = hc > 0 && tw > 0 ? hc / tw : 0;
            }

            if (ho <= 0 && d > 0 && tf > 0 && tfb > 0)
                ho = d - tf - tfb;
            if (ho <= 0 && d > 0)
                ho = 0.9 * d;
            
            // rts for LTB (F2-7): rts = sqrt(sqrt(Iy*Cw)/Sx)
            if (Iy > 0 && Cw > 0 && Sx > 0)
            {
                rts = Math.Sqrt(Math.Sqrt(Iy * Cw) / Sx);
            }
            
            // Detect Singly Symmetric I-Shape
            if (!IsPipe && !IsBoxHSS && !IsAngle && !IsChannel && !IsTee)
            {
                if (bf > 0 && bfb > 0 && tf > 0 && tfb > 0)
                {
                    if (Math.Abs(bf - bfb) > 0.1 || Math.Abs(tf - tfb) > 0.1)
                    {
                        IsSinglySymmetricI = true;
                    }
                }
            }
        }
    }



    #endregion

    #region Design Results

    /// <summary>
    /// Complete results of AISC 360 design check
    /// </summary>
    public class DesignResults
    {
        public string FrameName { get; set; }
        public AnalyzedMember Member { get; set; } // For debugging

        // Classifications
        public FlexureClassification FlexureClass { get; set; }
        public CompressionClassification CompressionClass { get; set; }

        // Design Checks
        public CheckDetail TensionCheck { get; set; }
        public CheckDetail CompressionCheck { get; set; }
        public CheckDetail FlexureCheckMajor { get; set; }
        public CheckDetail FlexureCheckMinor { get; set; }
        public CheckDetail ShearCheckMajor { get; set; }
        public CheckDetail ShearCheckMinor { get; set; }
        public CheckDetail InteractionCheck { get; set; }
        public CheckDetail TorsionCheck { get; set; } // H3
        public CheckDetail ServiceabilityCheck { get; set; }



        // Overall
        public CheckStatus OverallStatus { get; set; }
        public double GoverningRatio { get; set; }
        public string GoverningCheck { get; set; }

        public void DetermineOverall()
        {
            var allChecks = new List<CheckDetail>();
            if (TensionCheck != null) allChecks.Add(TensionCheck);
            if (CompressionCheck != null) allChecks.Add(CompressionCheck);
            if (FlexureCheckMajor != null) allChecks.Add(FlexureCheckMajor);
            if (FlexureCheckMinor != null) allChecks.Add(FlexureCheckMinor);
            if (ShearCheckMajor != null) allChecks.Add(ShearCheckMajor);
            if (ShearCheckMinor != null) allChecks.Add(ShearCheckMinor);
            if (InteractionCheck != null) allChecks.Add(InteractionCheck);
            if (TorsionCheck != null) allChecks.Add(TorsionCheck);


            GoverningRatio = 0;
            OverallStatus = CheckStatus.Pass;

            foreach (var check in allChecks)
            {
                if (check.Status == CheckStatus.Fail)
                    OverallStatus = CheckStatus.Fail;

                if (check.Ratio > GoverningRatio)
                {
                    GoverningRatio = check.Ratio;
                    GoverningCheck = check.Name;
                }
            }
        }
    }

    #endregion

    #region LRFD/ASD Factors

    public static class DesignFactors
    {
        // LRFD (φ factors)
        public const double Phi_Tension_Yield = 0.90;
        public const double Phi_Tension_Rupture = 0.75;
        public const double Phi_Compression = 0.90;
        public const double Phi_Flexure = 0.90;
        public const double Phi_Shear = 1.00;  // For most rolled I-shapes
        public const double Phi_Shear_NonCompact = 0.90;
        public const double Phi_Torsion = 0.90; // H3.1 (Round) & H3.2 (Rect)

        // ASD (Ω factors)
        public const double Omega_Tension_Yield = 1.67;
        public const double Omega_Tension_Rupture = 2.00;
        public const double Omega_Compression = 1.67;
        public const double Omega_Flexure = 1.67;
        public const double Omega_Shear = 1.50;
        public const double Omega_Shear_NonCompact = 1.67;
        public const double Omega_Torsion = 1.67;



        public static double GetCapacity(double nominal, double phi, double omega, DesignMethod method)
        {
            return method == DesignMethod.LRFD ? phi * nominal : nominal / omega;
        }
    }

    #endregion
}






