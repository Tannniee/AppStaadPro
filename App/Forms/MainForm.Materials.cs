using System;
using System.Diagnostics;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private void EnsureMaterialListLoaded(object openStaad, string selectedMaterial)
        {
            if (openStaad == null)
            {
                SelectMaterial(selectedMaterial);
                return;
            }

            string modelKey = StaadManager.TryGetCurrentIdentity(out StaadModelIdentity identity, out _)
                ? identity.CacheKey
                : StaadManager.GetModelCacheKey(openStaad);
            if (!string.Equals(_materialListModelKey, modelKey, StringComparison.OrdinalIgnoreCase))
            {
                ReloadMaterialList(openStaad, modelKey);
            }

            SelectMaterial(selectedMaterial);
        }

        private void RefreshMaterialListForModel(StaadModelIdentity identity)
        {
            if (identity == null)
            {
                ResetMaterialList();
                return;
            }

            if (string.Equals(_materialListModelKey, identity.CacheKey, StringComparison.OrdinalIgnoreCase))
                return;

            if (!StaadManager.TryGetConnection(out StaadConnectionSnapshot connection, out string error))
            {
                Debug.WriteLine("Refresh material list failed: " + error);
                ResetMaterialList();
                return;
            }

            if (!string.Equals(connection.Identity.CacheKey, identity.CacheKey, StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine("Skipped material refresh because the active STAAD model changed again.");
                return;
            }

            ReloadMaterialList(connection.OpenStaad, identity.CacheKey);
            SelectMaterial("STEEL");
        }

        private void ReloadMaterialList(object openStaad, string modelKey)
        {
            var materials = SectionService.GetIsotropicMaterialNames(openStaad);

            cboMaterial.BeginUpdate();
            try
            {
                cboMaterial.Items.Clear();
                cboMaterial.Items.Add("None");

                foreach (string material in materials)
                {
                    if (FindMaterialIndex(material) < 0)
                        cboMaterial.Items.Add(material);
                }

                _materialListModelKey = modelKey ?? string.Empty;
            }
            finally
            {
                cboMaterial.EndUpdate();
            }
        }

        private void ResetMaterialList()
        {
            if (string.IsNullOrEmpty(_materialListModelKey) &&
                cboMaterial.Items.Count == 2 &&
                FindMaterialIndex("None") == 0 &&
                FindMaterialIndex("STEEL") == 1 &&
                cboMaterial.SelectedIndex == 1)
            {
                return;
            }

            _materialListModelKey = string.Empty;
            cboMaterial.Items.Clear();
            cboMaterial.Items.Add("None");
            cboMaterial.Items.Add("STEEL");
            SelectMaterial("STEEL");
        }

        private void SelectMaterial(string material)
        {
            string preferred = string.IsNullOrWhiteSpace(material) ? "STEEL" : material.Trim();
            int index = FindMaterialIndex(preferred);
            if (index < 0)
            {
                cboMaterial.Items.Add(preferred);
                index = cboMaterial.Items.Count - 1;
            }

            cboMaterial.SelectedIndex = index;
        }

        private int FindMaterialIndex(string material)
        {
            for (int i = 0; i < cboMaterial.Items.Count; i++)
            {
                if (string.Equals(
                        Convert.ToString(cboMaterial.Items[i]),
                        material,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
