using System;

namespace StaadSteelApp.Core
{
    public class MemberForceData
    {
        public int MemberNo { get; set; }
        public int LoadCase { get; set; }
        public string Node { get; set; } // "Start" or "End"
        public double Fx { get; set; }
        public double Fy { get; set; }
        public double Fz { get; set; }
        public double Mx { get; set; }
        public double My { get; set; }
        public double Mz { get; set; }

        public override string ToString()
        {
            return $"{MemberNo},{LoadCase},{Node},{Fx},{Fy},{Fz},{Mx},{My},{Mz}";
        }

        public static string GetHeader()
        {
            return "Member,LoadCase,Node,Fx,Fy,Fz,Mx,My,Mz";
        }
    }
}
