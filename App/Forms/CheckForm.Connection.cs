using System.Diagnostics;
using System.Windows.Forms;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class CheckForm
    {
        private bool TryGetCheckOpenStaad(out object openStaad)
        {
            if (StaadManager.TryGetOpenSTAAD(out openStaad, out string error))
                return true;

            if (!string.IsNullOrWhiteSpace(error))
                Debug.WriteLine(error);

            TopMostMessageBox.ShowError(
                "Cannot connect to STAAD.Pro. Please ensure STAAD.Pro is running.",
                "Connection Error");
            return false;
        }
    }
}
