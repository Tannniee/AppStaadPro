using System;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void EnsureMaterialListLoaded(object openStaad, string selectedMaterial)
        {
            if (openStaad == null)
                return;

            if (!_materialListLoaded)
            {
                var mats = SectionService.GetIsotropicMaterialNames(openStaad);
                if (mats.Count > 0)
                {
                    cboMaterial.Items.Clear();
                    cboMaterial.Items.Add("None");
                    foreach (var material in mats)
                    {
                        if (!cboMaterial.Items.Contains(material))
                            cboMaterial.Items.Add(material);
                    }

                    _materialListLoaded = true;
                }
            }

            string preferred = string.IsNullOrWhiteSpace(selectedMaterial) ? "None" : selectedMaterial;
            if (string.IsNullOrWhiteSpace(preferred))
                return;

            if (!cboMaterial.Items.Contains(preferred))
                cboMaterial.Items.Insert(0, preferred);

            cboMaterial.Text = preferred;
        }
    }
}
