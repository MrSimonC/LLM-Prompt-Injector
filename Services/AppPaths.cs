using System;
using System.IO;
using System.Reflection;

namespace FolderSnippets.Services
{
    internal static class AppPaths
    {
        public static string ExePath => Environment.ProcessPath ?? Assembly.GetExecutingAssembly().Location;
        public static string AppDataDir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "FolderSnippets");
        public static string ConfigDir => AppDataDir;
        public static string SettingsPath => Path.Combine(ConfigDir, "settings.json");
    }
}
