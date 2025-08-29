using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace FolderSnippets.Services
{
    internal sealed class GlobalHotkey : IDisposable
    {
        [Flags]
        public enum Modifiers : uint
        {
            MOD_ALT = 0x0001,
            MOD_CONTROL = 0x0002,
            MOD_SHIFT = 0x0004,
            MOD_WIN = 0x0008,
            MOD_NOREPEAT = 0x4000
        }

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private readonly HotkeyWindow _window = new();
        private int _currentId = 0;
        private Action? _onHotkey;

        public GlobalHotkey()
        {
            _window.HotkeyPressed += id =>
            {
                if (id == _currentId)
                    _onHotkey?.Invoke();
            };
        }

        public bool Register(Modifiers modifiers, Keys key, Action onHotkey)
        {
            Unregister();

            _onHotkey = onHotkey;
            _currentId = 1;
            return RegisterHotKey(_window.Handle, _currentId, (uint)(modifiers | Modifiers.MOD_NOREPEAT), (uint)key);
        }

        public void Unregister()
        {
            if (_currentId != 0)
            {
                UnregisterHotKey(_window.Handle, _currentId);
                _currentId = 0;
            }
        }

        public void Dispose()
        {
            Unregister();
            _window.DestroyHandle();
        }
    }
}
