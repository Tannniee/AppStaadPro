using System.Windows.Forms;
using System.Drawing;
using StaadSteelApp.Core;

namespace StaadSteelApp
{
    public static class TopMostMessageBox
    {
        // Routes legacy message-box calls to the persistent status form.
        // Methods return OK to preserve existing caller flow.

        public static DialogResult Show(string text)
        {
            AppLogger.Info(text);
            StatusForm.UpdateMessage(text, autoHideMs: 1500);
            return DialogResult.OK;
        }

        public static DialogResult Show(string text, string caption)
        {
            LogMessage(text, caption, MessageBoxIcon.None);
            // Use the caption as a compatibility hint for status color.
            string safeCaption = caption ?? string.Empty;
            Color? color = null;
            if (safeCaption.IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) >= 0) color = Color.Red;
            else if (safeCaption.IndexOf("Warning", System.StringComparison.OrdinalIgnoreCase) >= 0) color = Color.Goldenrod;
            else if (safeCaption.IndexOf("Success", System.StringComparison.OrdinalIgnoreCase) >= 0) color = Color.Green;
            
            StatusForm.UpdateMessage(text, caption, color, 1500);
            return DialogResult.OK;
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons)
        {
             return Show(text, caption);
        }

        public static DialogResult Show(string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            LogMessage(text, caption, icon);
            Color? color = GetColorFromIcon(icon);
            StatusForm.UpdateMessage(text, caption, color, 1500);
            return DialogResult.OK;
        }

        public static DialogResult Show(IWin32Window owner, string text, string caption, MessageBoxButtons buttons, MessageBoxIcon icon)
        {
            LogMessage(text, caption, icon);
            Color? color = GetColorFromIcon(icon);
            StatusForm.UpdateMessage(text, caption, color, 1500);
            return DialogResult.OK;
        }

        // Compatibility wrapper for existing toast-style call sites.
        public static void ShowToast(string message, int durationMs = 1500, IWin32Window owner = null)
        {
            AppLogger.Info(message);
            StatusForm.UpdateMessage(message, autoHideMs: durationMs);
        }
        public static void ShowToast(IWin32Window owner, string message)
        {
            AppLogger.Info(message);
            StatusForm.UpdateMessage(message, autoHideMs: 1500);
        }

        public static void ShowSuccess(string message, string title = "Success")
        {
            AppLogger.Info(title + ": " + message);
            StatusForm.UpdateMessage(message, title, Color.Green, 1500);
        }

        public static void ShowError(string message, string title = "Error")
        {
            AppLogger.Error(title + ": " + message);
            StatusForm.UpdateMessage(message, title, Color.Red, 1500);
        }
        
        public static void ShowWarning(string message, string title = "Warning")
        {
            AppLogger.Warning(title + ": " + message);
            StatusForm.UpdateMessage(message, title, Color.Goldenrod, 1500);
        }

        public static void ShowInfo(string message, string title = "Info")
        {
            AppLogger.Info(title + ": " + message);
            StatusForm.UpdateMessage(message, title, Color.Black, 1500);
        }

        private static void LogMessage(string text, string caption, MessageBoxIcon icon)
        {
            string safeCaption = caption ?? string.Empty;
            string message = (string.IsNullOrWhiteSpace(safeCaption) ? "Message" : safeCaption) + ": " + (text ?? string.Empty);
            if (icon == MessageBoxIcon.Error ||
                safeCaption.IndexOf("Error", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AppLogger.Error(message);
                return;
            }

            if (icon == MessageBoxIcon.Warning ||
                safeCaption.IndexOf("Warning", System.StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AppLogger.Warning(message);
                return;
            }

            AppLogger.Info(message);
        }
        
        private static Color? GetColorFromIcon(MessageBoxIcon icon)
        {
            switch (icon)
            {
                case MessageBoxIcon.Error: return Color.Red;
                case MessageBoxIcon.Warning: return Color.Goldenrod;
                case MessageBoxIcon.Information: return Color.Black;
                case MessageBoxIcon.Question: return Color.Blue;
                default: return null;
            }
        }
    }
}
