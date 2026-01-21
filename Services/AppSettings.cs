using System;
using System.IO;
using System.Text.Json;

namespace FolderSnippets.Services
{
    internal sealed class AppSettings
    {
        public string FolderPath { get; set; } = "";
        public bool StartWithWindows { get; set; } = true;
        public string[] AllowedExtensions { get; set; } = new[] { ".txt", ".md", ".markdown" };
        public int MaxPasteBytes { get; set; } = 1_048_576; // 1 MB
        public string IgnoreSubstring { get; set; } = "_draft";
        public bool IgnoreDotfiles { get; set; } = true;
        public bool KeepOpenAfterInsertion { get; set; } = false;

        public static AppSettings LoadOrCreate()
        {
            Directory.CreateDirectory(AppPaths.ConfigDir);
            if (!File.Exists(AppPaths.SettingsPath))
            {
                var def = new AppSettings();
                def.Save();
                return def;
            }

            try
            {
                var json = File.ReadAllText(AppPaths.SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                return settings;
            }
            catch
            {
                return new AppSettings();
            }
        }

        public void Save()
        {
            Directory.CreateDirectory(AppPaths.ConfigDir);
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppPaths.SettingsPath, json);
        }
    }
}
