using AutoHotkey.Interop;

namespace FolderSnippets.Services
{
    internal static class AutoHotkeyService
    {
        public static void SendCtrlV()
        {
            try { AutoHotkeyEngine.Instance.ExecRaw("Send, ^v"); } catch { }
        }
    }
}
