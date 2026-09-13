using System;
using System.IO;
using System.Text.Json;

namespace M18BatteryInfo
{
    /// <summary>
    /// Persists user configurable paths for the Python executable and m18.py script. Paths are stored
    /// in a JSON file under the user's application data folder so they survive app restarts without
    /// requiring registry edits or manual config tweaks.
    /// </summary>
    public class AppSettings
    {
        public string PythonExecutablePath { get; set; } = "python";
        public string ScriptPath { get; set; } = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "m18.py");

        private static string SettingsFilePath =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "M18BatteryInfo", "settings.json");

        /// <summary>
        /// Load persisted settings from disk. If the file is missing or unreadable, defaults are
        /// returned and a new settings file will be written the next time <see cref="Save"/> is called.
        /// </summary>
        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json);
                    if (settings != null)
                    {
                        return settings;
                    }
                }
            }
            catch
            {
                // If the file is malformed, fall back to defaults without throwing to keep the UI usable.
            }

            return new AppSettings();
        }

        /// <summary>
        /// Persist the current settings to disk, creating the directory if needed.
        /// </summary>
        public void Save()
        {
            var directory = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsFilePath, json);
        }
    }
}
