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
        private static readonly object _settingsLock = new object();
        private static Dictionary<string, string> _cache;

        public static void Save(string key, Point loc)
        {
            try
            {
                lock (_settingsLock)
                {
                    var settings = GetSettingsLocked();
                    settings[key + "_X"] = loc.X.ToString();
                    settings[key + "_Y"] = loc.Y.ToString();
                    SaveSettings(settings);
                }
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
                lock (_settingsLock)
                {
                    var settings = GetSettingsLocked();
                    int x = 100, y = 100;
                    if (settings.TryGetValue(key + "_X", out string valueX)) int.TryParse(valueX, out x);
                    if (settings.TryGetValue(key + "_Y", out string valueY)) int.TryParse(valueY, out y);
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
                lock (_settingsLock)
                {
                    var settings = GetSettingsLocked();
                    settings[key] = value ?? string.Empty;
                    SaveSettings(settings);
                }
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
                lock (_settingsLock)
                {
                    var settings = GetSettingsLocked();
                    if (settings.TryGetValue(key, out string value))
                    {
                        return value;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"AppSettings.LoadString failed for key '{key}': {ex}");
            }

            return defaultValue;
        }

        private static Dictionary<string, string> GetSettingsLocked()
        {
            if (_cache == null)
                _cache = LoadSettingsFromDisk();

            return _cache;
        }

        private static Dictionary<string, string> LoadSettingsFromDisk()
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
