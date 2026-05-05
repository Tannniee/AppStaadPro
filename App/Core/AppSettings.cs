using System;
using System.IO;
using System.Drawing;
using System.Diagnostics;
using System.Collections.Generic;

namespace StaadSteelApp.Core
{
    public class AppSettings
    {
        public Point Location { get; set; }
        
        // Default
        public AppSettings()
        {
            Location = new Point(100, 100);
        }

        private static string SettingsPath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.ini");

        public static void Save(string key, Point loc)
        {
            try
            {
                var settings = LoadSettings();
                settings[key + "_X"] = loc.X.ToString();
                settings[key + "_Y"] = loc.Y.ToString();
                SaveSettings(settings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AppSettings.Save failed for key '{key}': {ex}");
            }
        }

        public static Point Load(string key)
        {
            Point p = new Point(100, 100);
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string[] lines = File.ReadAllLines(SettingsPath);
                    int x = 100, y = 100;
                    foreach(var line in lines)
                    {
                        var parts = line.Split('=');
                        if (parts.Length == 2)
                        {
                            if (parts[0].Trim() == key + "_X") int.TryParse(parts[1], out x);
                            if (parts[0].Trim() == key + "_Y") int.TryParse(parts[1], out y);
                        }
                    }
                    p.X = x; p.Y = y;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AppSettings.Load failed for key '{key}': {ex}");
            }
            return p;
        }

        public static void SaveString(string key, string value)
        {
            try
            {
                var settings = LoadSettings();
                settings[key] = value ?? string.Empty;
                SaveSettings(settings);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AppSettings.SaveString failed for key '{key}': {ex}");
            }
        }

        public static string LoadString(string key, string defaultValue = "")
        {
            try
            {
                var settings = LoadSettings();
                if (settings.TryGetValue(key, out string value))
                {
                    return value;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AppSettings.LoadString failed for key '{key}': {ex}");
            }

            return defaultValue;
        }

        private static Dictionary<string, string> LoadSettings()
        {
            var settings = new Dictionary<string, string>();
            if (!File.Exists(SettingsPath))
            {
                return settings;
            }

            string[] lines = File.ReadAllLines(SettingsPath);
            foreach (var line in lines)
            {
                var parts = line.Split(new[] { '=' }, 2);
                if (parts.Length == 2)
                {
                    settings[parts[0].Trim()] = parts[1].Trim();
                }
            }

            return settings;
        }

        private static void SaveSettings(Dictionary<string, string> settings)
        {
            using (StreamWriter sw = new StreamWriter(SettingsPath))
            {
                foreach (var kvp in settings)
                {
                    sw.WriteLine(kvp.Key + "=" + kvp.Value);
                }
            }
        }
    }
}
