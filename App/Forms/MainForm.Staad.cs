using System.Diagnostics;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public partial class MainForm
    {
        private bool TryGetMainOpenStaad(out object openStaad)
        {
            if (StaadManager.TryGetOpenSTAAD(out openStaad, out string error))
                return true;

            if (!string.IsNullOrWhiteSpace(error))
            {
                Debug.WriteLine(error);
                TopMostMessageBox.ShowError(error, "Connection Error");
            }

            return false;
        }
    }
}
