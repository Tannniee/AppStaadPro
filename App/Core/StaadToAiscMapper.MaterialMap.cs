using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using StaadSteelApp.AISC360;

namespace StaadSteelApp.Core
{
    public partial class StaadToAiscMapper
    {
        private void MapMaterialProperties(int memberNo, AnalyzedMember am)
        {
            // Use STAAD's default steel stiffness when material properties cannot be read.
            am.E = 205000; // MPa
            am.Fy = 345;   // MPa (Gr. 50)
            am.Fu = 450;
            am.G = 79000;  // MPa (paired with E=205000, nu~0.30)

            if (_os == null || am == null) return;

            string materialName = TryGetBeamMaterialName(memberNo);
            if (string.IsNullOrWhiteSpace(materialName))
            {
                WarnOnce($"MaterialBlank:{memberNo}", $"Member {memberNo}: STAAD material name is blank/unknown. Default steel material properties will be used.");
                return;
            }

            double rawE = 0;
            double rawPoisson = 0;
            double rawFy = 0;
            double rawFu = 0;

            bool gotMaterialEx = TryGetMaterialPropertyEx(materialName, out rawE, out rawPoisson, out rawFy, out rawFu);
            if (!gotMaterialEx)
            {
                if (!TryGetMaterialProperty(materialName, out rawE, out rawPoisson))
                    return;
            }
            else
            {
                // GetMaterialPropertyEx reported success but returned no usable values.
                if (rawE <= 0 && rawFy <= 0 && rawFu <= 0)
                {
                    WarnOnce($"MaterialAllZero:{materialName}", $"Material '{materialName}': STAAD returned no usable E/Fy/Fu. Defaults will be used.");
                }
            }

            if (rawE > 0)
            {
                if (TryConvertStressToMpa(rawE, out double eMpa) && eMpa > 1000)
                    am.E = eMpa;
                else
                    WarnOnce($"MaterialE:{materialName}", $"Material '{materialName}': could not convert E='{rawE}' to a valid MPa value. Default E will be used.");
            }

            if (rawFy > 0)
            {
                if (TryConvertStressToMpa(rawFy, out double fyMpa))
                    am.Fy = fyMpa;
                else
                    WarnOnce($"MaterialFy:{materialName}", $"Material '{materialName}': could not convert Fy='{rawFy}' to MPa. Default Fy will be used.");
            }

            if (rawFu > 0)
            {
                if (TryConvertStressToMpa(rawFu, out double fuMpa))
                    am.Fu = fuMpa;
                else
                    WarnOnce($"MaterialFu:{materialName}", $"Material '{materialName}': could not convert Fu='{rawFu}' to MPa. Default Fu will be used.");
            }

            // Use nu from material if available to keep G consistent with E.
            if (rawPoisson > 0 && rawPoisson < 0.5 && am.E > 0)
                am.G = am.E / (2.0 * (1.0 + rawPoisson));
        }
    }
}
