using System;
using System.Linq;

namespace StaadSteelApp.Core
{
    public static class SectionNamingService
    {
        public static string AutoIncrementString(string s)
        {
            if (string.IsNullOrEmpty(s)) return "1";

            string numStr = "";
            int i;

            // Read a trailing numeric suffix and increment it.
            for (i = s.Length - 1; i >= 0; i--)
            {
                if (char.IsDigit(s[i]))
                {
                    numStr = s[i] + numStr;
                }
                else
                {
                    break;
                }
            }

            if (numStr.Length > 0)
            {
                string prefix = s.Substring(0, s.Length - numStr.Length);
                
                if (long.TryParse(numStr, out long n))
                {
                    n++;
                    return prefix + n.ToString();
                }
            }
            
            return s + "-1";
        }

        public static string FmtNum(double d)
        {
            if (Math.Abs(d - Math.Truncate(d)) < 0.000001)
            {
                return ((long)d).ToString();
            }
            else
            {
                return d.ToString("0.###").Replace(",", ".");
            }
        }

        public static string FmtNum(string s)
        {
            if (double.TryParse(s, out double v)) return FmtNum(v);
            return "0";
        }

        public static string GenerateSectionName(eFramePropType type, double h1, double h2, double tw, double bf, double tf, double bfb, double tfb, double dis = 0)
        {
            if (type == eFramePropType.Pipe)
            {
               if (h1 > 0 && tw > 0) return "Pipe-" + FmtNum(h1) + "x" + FmtNum(tw);
            }
            else if (type == eFramePropType.Rectangular || type == eFramePropType.Box)
            {
               if (h1 > 0 && h2 > 0 && tf > 0)
               {
                   if (tw <= 0 || Math.Abs(tf - tw) < 0.000001) return "Tube-" + FmtNum(h1) + "x" + FmtNum(h2) + "x" + FmtNum(tf);
                   else return "Tube-" + FmtNum(h1) + "x" + FmtNum(h2) + "x" + FmtNum(tf) + "x" + FmtNum(tw);
               }
            }
            else if (type == eFramePropType.Channel)
            {
               if (h1 > 0 && bf > 0 && tf > 0 && tw > 0)
                   return "Channel-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tw) + "x" + FmtNum(tf);
            }
            else if (type == eFramePropType.T)
            {
               if (h1 > 0 && bf > 0 && tf > 0 && tw > 0)
                   return "Tee-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tw) + "x" + FmtNum(tf);
            }
            else if (type == eFramePropType.Angle)
            {
               if (h1 > 0 && bf > 0 && tf > 0)
               {
                   if (Math.Abs(h1 - bf) < 0.000001) return "L-" + FmtNum(h1) + "x" + FmtNum(tf);
                   else return "L-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tf);
               }
            }
            else if (type == eFramePropType.DblAngle)
            {
               if (h1 > 0 && bf > 0 && tf > 0)
                   return "2L-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tf) + "-gap" + FmtNum(dis);
            }
            else if (type == eFramePropType.DbChannel)
            {
               if (h1 > 0 && bf > 0 && tf > 0 && tw > 0)
                   return "2C-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tw) + "x" + FmtNum(tf) + "-gap" + FmtNum(dis);
            }
            else if (type == eFramePropType.Circle)
            {
               if (h1 > 0) return "Rod-D" + FmtNum(h1);
            }
            else if (type == eFramePropType.Trapezoidal)
            {
               if (h1 > 0 && h2 > 0 && bf > 0)
                   return "Trap-" + FmtNum(h1) + "x" + FmtNum(h2) + "x" + FmtNum(bf);
            }
            else // Beam or Tapered
            {
                if (h1 <= 0 || tw <= 0 || bf <= 0 || tf <= 0) return "";

                 if (Math.Abs(h1 - h2) < 0.000001 || h2 <= 0)
                 {
                     if (bfb > 0 && tfb > 0 && (Math.Abs(bf - bfb) > 0.1 || Math.Abs(tf - tfb) > 0.1))
                     {
                         return "I-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tw) + "x" + FmtNum(tf) + "x" + FmtNum(bfb) + "x" + FmtNum(tfb);
                     }
                     return "I-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tw) + "x" + FmtNum(tf);
                 }
                 else
                 {
                     if (bfb > 0 && tfb > 0 && (Math.Abs(bf - bfb) > 0.1 || Math.Abs(tf - tfb) > 0.1))
                     {
                         return "I-(" + FmtNum(h1) + "~" + FmtNum(h2) + ")x" + FmtNum(bf) + "x" + FmtNum(tw) + "x" + FmtNum(tf) + "x" + FmtNum(bfb) + "x" + FmtNum(tfb);
                     }
                     return "I-(" + FmtNum(h1) + "~" + FmtNum(h2) + ")x" + FmtNum(bf) + "x" + FmtNum(tw) + "x" + FmtNum(tf);
                 }
             }
 
             return "";
         }

        public static string GenerateUptSectionName(string uptType, double h1, double h2, double tw, double bf, double tf, double bfb, double tfb, double dis = 0)
        {
            string type = (uptType ?? string.Empty).Trim().ToUpperInvariant();

            if (type == "WIDE FLANGE")
            {
                if (h1 <= 0 || bf <= 0 || tw <= 0 || tf <= 0) return "";
                string name = "I-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tw) + "x" + FmtNum(tf);
                if (bfb > 0 && tfb > 0 && (Math.Abs(bf - bfb) > 0.000001 || Math.Abs(tf - tfb) > 0.000001))
                {
                    name += "/" + FmtNum(bfb) + "x" + FmtNum(tfb);
                }
                return name;
            }

            if (type == "CHANNEL")
            {
                if (h1 > 0 && bf > 0 && tw > 0 && tf > 0)
                    return "Channel-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tw) + "x" + FmtNum(tf);
                return "";
            }

            if (type == "ANGLE")
            {
                if (h1 > 0 && bf > 0 && tf > 0)
                    return "L-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tf);
                return "";
            }

            if (type == "DOUBLE ANGLE")
            {
                if (h1 > 0 && bf > 0 && tf > 0)
                {
                    string name = "2L-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tf);
                    if (dis > 0) name += "-gap" + FmtNum(dis);
                    return name;
                }
                return "";
            }

            if (type == "TEE")
            {
                if (h1 > 0 && bf > 0 && tw > 0 && tf > 0)
                    return "Tee-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tw) + "x" + FmtNum(tf);
                return "";
            }

            if (type == "PIPE")
            {
                if (h1 > 0 && bf > 0)
                    return "Pipe-" + FmtNum(h1) + "x" + FmtNum(bf);
                if (h1 > 0)
                    return "Pipe-" + FmtNum(h1);
                return "";
            }

            if (type == "TUBE")
            {
                if (h1 > 0 && bf > 0 && tf > 0)
                    return "Tube-" + FmtNum(h1) + "x" + FmtNum(bf) + "x" + FmtNum(tf);
                return "";
            }

            if (type == "GENERAL")
            {
                if (h1 > 0 && h2 > 0 && tw > 0 && tf > 0)
                    return "General-" + FmtNum(h1) + "x" + FmtNum(h2) + "x" + FmtNum(tw) + "x" + FmtNum(tf);
                return "";
            }

            if (type == "ISECTION")
            {
                if (h1 > 0 && tw > 0 && h2 > 0 && bf > 0 && tf > 0 && bfb > 0 && tfb > 0)
                {
                    return "ISection-" + FmtNum(h1) + "x" + FmtNum(tw) + "x" + FmtNum(h2) + "x"
                        + FmtNum(bf) + "x" + FmtNum(tf) + "x" + FmtNum(bfb) + "x" + FmtNum(tfb);
                }
                return "";
            }

            if (type == "PRISMATIC")
            {
                if (h1 > 0 && h2 > 0)
                    return "Prismatic-" + FmtNum(h1) + "x" + FmtNum(h2);
                return "";
            }

            return "";
        }
    }
}

