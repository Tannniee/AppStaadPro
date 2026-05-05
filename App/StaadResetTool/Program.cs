using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Windows.Forms;

namespace StaadResetTool
{
    internal static class Program
    {
        private const string ResetPositionEventName = @"Local\StaadSteelApp.ResetPosition";
        private const string ResetFlagKey = "Main_ResetToCenter";

        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            try
            {
                string settingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");
                Dictionary<string, string> settings = LoadSettings(settingsPath);
                settings[ResetFlagKey] = "1";
                SaveSettings(settingsPath, settings);

                TrySignalRunningApp();

                MessageBox.Show(
                    "Da reset vi tri cua STAAD Tools.\r\n\r\nNeu app dang mo, cua so se duoc dua ve giua man hinh.\r\nNeu app chua mo, lan mo tiep theo se vao giua man hinh.",
                    "STAAD Reset",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Khong the reset vi tri cua STAAD Tools.\r\n\r\n" + ex.Message,
                    "STAAD Reset",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error,
                    MessageBoxDefaultButton.Button1,
                    MessageBoxOptions.DefaultDesktopOnly);
            }
        }

        private static void TrySignalRunningApp()
        {
            try
            {
                using (var resetEvent = EventWaitHandle.OpenExisting(ResetPositionEventName))
                {
                    resetEvent.Set();
                    Thread.Sleep(150);
                    resetEvent.Set();
                }
            }
            catch
            {
                // App may not be running yet. The persisted flag handles next launch.
            }
        }

        private static Dictionary<string, string> LoadSettings(string path)
        {
            var settings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!File.Exists(path))
            {
                return settings;
            }

            foreach (string line in File.ReadAllLines(path))
            {
                string[] parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    settings[parts[0].Trim()] = parts[1].Trim();
                }
            }

            return settings;
        }

        private static void SaveSettings(string path, Dictionary<string, string> settings)
        {
            using (var writer = new StreamWriter(path))
            {
                foreach (var kvp in settings)
                {
                    writer.WriteLine(kvp.Key + "=" + kvp.Value);
                }
            }
        }
    }
}
