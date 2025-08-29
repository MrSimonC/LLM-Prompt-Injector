using System;
using System.Windows.Forms;

namespace FolderSnippets.Services
{
    internal sealed class HotkeyWindow : NativeWindow
    {
        public event Action<int>? HotkeyPressed;

        public HotkeyWindow()
        {
            var cp = new CreateParams
            {
                Caption = "FolderSnippetsHotkeyWindow",
                Parent = new IntPtr(-3) // HWND_MESSAGE - message only window
            };
            CreateHandle(cp);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            if (m.Msg == WM_HOTKEY)
            {
                int id = m.WParam.ToInt32();
                HotkeyPressed?.Invoke(id);
            }
            base.WndProc(ref m);
        }
    }
}
