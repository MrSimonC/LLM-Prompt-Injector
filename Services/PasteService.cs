using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace FolderSnippets.Services
{
    internal sealed class PasteService
    {
        [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
        [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
        [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint
lpdwProcessId);
        [DllImport("kernel32.dll")] private static extern uint GetCurrentThreadId();
        [DllImport("user32.dll")]
        private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool
fAttach);

        private const int SW_RESTORE = 9;

        private const uint CF_UNICODETEXT = 13;
        [DllImport("user32.dll", SetLastError = true)] private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll", SetLastError = true)] private static extern bool CloseClipboard();
        [DllImport("user32.dll", SetLastError = true)] private static extern bool EmptyClipboard();
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetClipboardData(uint uFormat,
IntPtr hMem);
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GlobalAlloc(uint uFlags, UIntPtr
dwBytes);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr GlobalLock(IntPtr hMem);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GlobalUnlock(IntPtr hMem);
        private const uint GMEM_MOVEABLE = 0x0002;

        

        public IntPtr CaptureCurrentForegroundWindow() => GetForegroundWindow();

        public void PasteIntoWindowThenReturn(IntPtr targetWindow, string text, Form overlay)
        {
            if (targetWindow == IntPtr.Zero) return;

            IDataObject? prior = GetClipboardObjectWithRetry(TimeSpan.FromMilliseconds(800));

            try
            {
                if (!ClearClipboardNative(TimeSpan.FromMilliseconds(600)))
                    EnsureClipboardCleared(TimeSpan.FromMilliseconds(800));

                if (!SetClipboardTextNative(text, TimeSpan.FromMilliseconds(900)))
                    EnsureClipboardText(text, TimeSpan.FromMilliseconds(1400));

                BringToForeground(targetWindow);
                Thread.Sleep(60);

                AutoHotkeyService.SendCtrlV();
                Thread.Sleep(120);
            }
            finally { }

            try
            {
                var restoreThread = new System.Threading.Thread(() =>
                {
                    try { RestoreClipboard(prior, TimeSpan.FromMilliseconds(2000)); } catch { }
                });
                restoreThread.IsBackground = true;
                restoreThread.SetApartmentState(System.Threading.ApartmentState.STA);
                restoreThread.Start();

                overlay.TopMost = true;
                overlay.Show();
                overlay.Activate();
            }
            catch { }
        }

        

        private static IDataObject? GetClipboardObjectWithRetry(TimeSpan timeout)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                try { return Clipboard.GetDataObject(); } catch { Thread.Sleep(15); }
            }
            try { return Clipboard.GetDataObject(); } catch { return null; }
        }

        private static bool TryOpenClipboard(TimeSpan timeout)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.Elapsed < timeout)
            {
                if (OpenClipboard(IntPtr.Zero)) return true;
                Thread.Sleep(10);
            }
            return OpenClipboard(IntPtr.Zero);
        }

        private static bool ClearClipboardNative(TimeSpan timeout)
        {
            if (!TryOpenClipboard(timeout)) return false;
            try
            {
                if (!EmptyClipboard()) return false;
                return true;
            }
            finally { CloseClipboard(); }
        }

        private static bool SetClipboardTextNative(string text, TimeSpan timeout)
        {
            var data = System.Text.Encoding.Unicode.GetBytes(text + "\0");
            if (!TryOpenClipboard(timeout)) return false;
            try
            {
                if (!EmptyClipboard()) return false;
                var hGlobal = GlobalAlloc(GMEM_MOVEABLE, (UIntPtr)data.Length);
                if (hGlobal == IntPtr.Zero) return false;
                var pGlobal = GlobalLock(hGlobal);
                if (pGlobal == IntPtr.Zero) return false;
                try { Marshal.Copy(data, 0, pGlobal, data.Length); }
                finally { GlobalUnlock(hGlobal); }

                if (SetClipboardData(CF_UNICODETEXT, hGlobal) == IntPtr.Zero) return false;
                return true;
            }
            finally { CloseClipboard(); }
        }

        private static void EnsureClipboardCleared(TimeSpan timeout)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                try { Clipboard.Clear(); } catch { }

                bool empty = false;
                try
                {
                    empty = !Clipboard.ContainsText() && (Clipboard.GetDataObject()?.GetFormats().Length ?? 0) == 0;
                }
                catch { }

                if (empty) return;
                if (sw.Elapsed >= timeout) return;
                Thread.Sleep(20);
            }
        }

        private static void EnsureClipboardText(string text, TimeSpan timeout)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (true)
            {
                bool set = false;
                try { Clipboard.SetDataObject(text, true); set = true; } catch { }

                if (set)
                {
                    try
                    {
                        if (Clipboard.ContainsText())
                        {
                            var got = Clipboard.GetText();
                            if (got.Length == text.Length && got == text) return;
                        }
                    }
                    catch { }
                }

                if (sw.Elapsed >= timeout) return;
                Thread.Sleep(20);
            }
        }

        private static void RestoreClipboard(IDataObject? prior, TimeSpan timeout)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            if (prior == null) return;

            Thread.Sleep(160);

            while (true)
            {
                try
                {
                    Clipboard.SetDataObject(prior, true);
                    return;
                }
                catch { }

                if (sw.Elapsed >= timeout) return;
                Thread.Sleep(25);
            }
        }

        private static void BringToForeground(IntPtr hWnd)
        {
            if (IsIconic(hWnd))
                ShowWindow(hWnd, SW_RESTORE);

            uint fgThread = GetWindowThreadProcessId(GetForegroundWindow(), out _);
            uint targetThread = GetWindowThreadProcessId(hWnd, out _);
            uint curThread = GetCurrentThreadId();

            if (fgThread != curThread)
                AttachThreadInput(curThread, fgThread, true);
            if (targetThread != curThread)
                AttachThreadInput(curThread, targetThread, true);

            SetForegroundWindow(hWnd);

            if (fgThread != curThread)
                AttachThreadInput(curThread, fgThread, false);
            if (targetThread != curThread)
                AttachThreadInput(curThread, targetThread, false);
        }

        // SendInput path removed in favor of AutoHotkey.dll key simulation
    }
}
