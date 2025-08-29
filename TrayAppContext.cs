using FolderSnippets.Services;
using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace FolderSnippets
{
    internal sealed class TrayAppContext : ApplicationContext
    {
        private readonly NotifyIcon _tray;
        private readonly GlobalHotkey _hotkey;
        private readonly OverlayForm _overlay;
        private readonly FileIndexService _indexService;
        private readonly PasteService _pasteService;
        private readonly AppSettings _settings;
        private readonly StartupManager _startup;

        public TrayAppContext()
        {
            _settings = AppSettings.LoadOrCreate();
            _startup = new StartupManager("FolderSnippets", AppPaths.ExePath);

            if (_settings.StartWithWindows)
                _startup.EnableStartup();
            else
                _startup.DisableStartup();

            _pasteService = new PasteService();

            _indexService = new FileIndexService(_settings);
            _indexService.Rescan();

            _overlay = new OverlayForm(_settings, _indexService, _pasteService);

            _tray = new NotifyIcon
            {
                Icon = UiResources.AppIcon,
                Text = "FolderSnippets",
                Visible = true,
                ContextMenuStrip = BuildMenu()
            };
            _tray.DoubleClick += (_, __) => ShowOverlay();

            _hotkey = new GlobalHotkey();
            RegisterHotkeyOrWarn();

            // first run - ensure folder selected
            if (string.IsNullOrWhiteSpace(_settings.FolderPath) || !Directory.Exists(_settings.FolderPath))
                PromptForFolder();
        }

        private ContextMenuStrip BuildMenu()
        {
            var menu = new ContextMenuStrip();

            var openItem = new ToolStripMenuItem("Open Overlay", null, (_, __) => ShowOverlay());
            var settingsItem = new ToolStripMenuItem("Settings...", null, (_, __) => ShowSettings());
            var startOnLoginItem = new ToolStripMenuItem("Start on Windows login")
            {
                Checked = _settings.StartWithWindows,
                CheckOnClick = true
            };
            startOnLoginItem.CheckedChanged += (_, __) =>
            {
                _settings.StartWithWindows = startOnLoginItem.Checked;
                if (_settings.StartWithWindows) _startup.EnableStartup(); else _startup.DisableStartup();
                _settings.Save();
            };
            var restartHotkeyItem = new ToolStripMenuItem("Restart Hotkey", null, (_, __) => RegisterHotkeyOrWarn());
            var aboutItem = new ToolStripMenuItem("About...", null, (_, __) => ShowAbout());
            var exitItem = new ToolStripMenuItem("Exit", null, (_, __) => ExitThread());

            menu.Items.Add(openItem);
            menu.Items.Add(settingsItem);
            menu.Items.Add(startOnLoginItem);
            menu.Items.Add(restartHotkeyItem);
            menu.Items.Add(aboutItem);
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add(exitItem);

            return menu;
        }

        private void RegisterHotkeyOrWarn()
        {
            _hotkey.Unregister();

            // Ctrl + Alt + F
            if (!_hotkey.Register(GlobalHotkey.Modifiers.MOD_CONTROL | GlobalHotkey.Modifiers.MOD_ALT, Keys.F, OnHotkey))
            {
                _tray.ShowBalloonTip(4000, "FolderSnippets", "Could not register Ctrl+Alt+F - is another app using it?", ToolTipIcon.Warning);
            }
        }

        private void OnHotkey()
        {
            ShowOverlay();
        }

        private void ShowOverlay()
        {
            _overlay.ShowOverlay();
        }

        private void ShowSettings()
        {
            using var dlg = new SettingsForm(_settings, _indexService, _startup);
            dlg.ShowDialog();
        }

        private void ShowAbout()
        {
            using var dlg = new AboutForm();
            dlg.ShowDialog();
        }

        private void PromptForFolder()
        {
            using var fbd = new FolderBrowserDialog
            {
                Description = "Choose the folder containing your text or markdown files",
                ShowNewFolderButton = false
            };
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                _settings.FolderPath = fbd.SelectedPath;
                _settings.Save();
                _indexService.Rescan();
            }
        }

        protected override void ExitThreadCore()
        {
            _hotkey.Unregister();
            _tray.Visible = false;
            _tray.Dispose();
            _overlay.Dispose();
            base.ExitThreadCore();
        }
    }
}
