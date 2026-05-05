using System.Windows.Forms;
using System.Drawing;

namespace StaadSteelApp
{
    public static class TopMostMessageBox
    {
        // Redirect ALL Popups to Persistent Status Form
        // Returns OK automatically to not block code flow.

        public static DialogResult Show(string text)
        {
            StatusForm.UpdateMessage(text, autoHideMs: 1500);
            return DialogResult.OK;
        }

        public static DialogResult Show(string text, string caption)
        {
            // Infer Color from Caption if possible
            Color? color = null;
            if (caption.IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) >= 0) color = Color.Red;
            else if (caption.IndexOf("Warning", System.StringComparison.OrdinalIgnoreCase) >= 0) color = Color.Goldenrod;
            else if (caption.IndexOf("Success", System.StringComparison.OrdinalIgnoreCase) >= 0) color = Color.Green;
            
            StatusForm.UpdateMessage(text, caption, color, 1500);
            return DialogResult.OK;
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
             return Show(text, caption);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            Color? color = GetColorFromIcon(icon);
            StatusForm.UpdateMessage(text, caption, color, 1500);
            return DialogResult.OK;
        }

        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            Color? color = GetColorFromIcon(icon);
            StatusForm.UpdateMessage(text, caption, color, 1500);
            return DialogResult.OK;
        }

        // Keep ShowToast for compatibility
        public static void ShowToast(string message, int durationMs = 1500, IWin32Window owner = null)
        {
            StatusForm.UpdateMessage(message, autoHideMs: durationMs);
        }
        public static void ShowToast(IWin32Window owner, string message) => StatusForm.UpdateMessage(message, autoHideMs: 1500);

        // --- New Typed Helpers ---

        public static void ShowSuccess(string message, string title = "Success")
        {
            StatusForm.UpdateMessage(message, title, Color.Green, 1500);
        }

        public static void ShowError(string message, string title = "Error")
        {
            StatusForm.UpdateMessage(message, title, Color.Red, 1500);
        }
        
        public static void ShowWarning(string message, string title = "Warning")
        {
            StatusForm.UpdateMessage(message, title, Color.Goldenrod, 1500);
        }

        public static void ShowInfo(string message, string title = "Info")
        {
            StatusForm.UpdateMessage(message, title, Color.Black, 1500);
        }
        
        private static Color? GetColorFromIcon(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Error: return Color.Red; // Hand, Stop
                case MessageBoxIcon.Warning: return Color.Goldenrod; // Exclamation
                case MessageBoxIcon.Information: return Color.Black; // Asterisk (User requested Black)
                case MessageBoxIcon.Question: return Color.Blue;
                default: return null;
            }
        }
    }
}
