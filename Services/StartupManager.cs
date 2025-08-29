using Microsoft.Win32;

namespace FolderSnippets.Services
{
    internal sealed class StartupManager
    {
        private readonly string _valueName;
        private readonly string _exePath;

        public StartupManager(string valueName, string exePath)
        {
            _valueName = valueName;
            _exePath = exePath;
        }

        public void EnableStartup()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true)
                ?? Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key.SetValue(_valueName, $"\"{_exePath}\"");
        }

        public void DisableStartup()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", true);
            key?.DeleteValue(_valueName, false);
        }

        public bool IsEnabled()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run", false);
            var v = key?.GetValue(_valueName) as string;
            return !string.IsNullOrEmpty(v);
        }
    }
}
