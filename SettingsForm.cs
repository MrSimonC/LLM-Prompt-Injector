using FolderSnippets.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace FolderSnippets
{
    internal sealed class SettingsForm : Form
    {
        private readonly AppSettings _settings;
        private readonly FileIndexService _index;
        private readonly StartupManager _startup;

        private readonly TextBox _txtFolder = new();
        private readonly Button _btnBrowse = new();
        private readonly CheckedListBox _exts = new();
        private readonly NumericUpDown _maxBytes = new();
        private readonly CheckBox _chkStartOnLogin = new();
        private readonly CheckBox _chkIgnoreDot = new();
        private readonly TextBox _txtIgnoreSubstring = new();
        private readonly Button _btnSave = new();
        private readonly Button _btnCancel = new();

        public SettingsForm(AppSettings settings, FileIndexService index, StartupManager startup)
        {
            _settings = settings;
            _index = index;
            _startup = startup;

            Text = "FolderSnippets Settings";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Width = 640;
            Height = 420;

            var lblFolder = new Label { Text = "Folder:", Left = 12, Top = 16, AutoSize = true };
            _txtFolder.Left = 80; _txtFolder.Top = 12; _txtFolder.Width = 440; _txtFolder.Text = _settings.FolderPath;
            _btnBrowse.Text = "Browse..."; _btnBrowse.Left = 530; _btnBrowse.Top = 10; _btnBrowse.Click += (_, __) => BrowseFolder();

            var lblExt = new Label { Text = "Extensions:", Left = 12, Top = 56, AutoSize = true };
            _exts.Left = 80; _exts.Top = 52; _exts.Width = 200; _exts.Height = 80;
            var known = new[] { ".txt", ".md", ".markdown" };
            foreach (var e in known)
                _exts.Items.Add(e, _settings.AllowedExtensions.Contains(e, StringComparer.OrdinalIgnoreCase));

            var lblMax = new Label { Text = "Max paste bytes:", Left = 300, Top = 56, AutoSize = true };
            _maxBytes.Left = 420; _maxBytes.Top = 52; _maxBytes.Width = 200;
            _maxBytes.Maximum = 10_000_000; _maxBytes.Minimum = 1_000; _maxBytes.Increment = 1_000;
            _maxBytes.Value = _settings.MaxPasteBytes;

            _chkStartOnLogin.Text = "Start on Windows login";
            _chkStartOnLogin.Left = 80; _chkStartOnLogin.Top = 150; _chkStartOnLogin.Checked = _settings.StartWithWindows;

            _chkIgnoreDot.Text = "Ignore dotfiles";
            _chkIgnoreDot.Left = 80; _chkIgnoreDot.Top = 180; _chkIgnoreDot.Checked = _settings.IgnoreDotfiles;

            var lblIgnore = new Label { Text = "Ignore filenames containing:", Left = 80, Top = 210, AutoSize = true };
            _txtIgnoreSubstring.Left = 280; _txtIgnoreSubstring.Top = 206; _txtIgnoreSubstring.Width = 200; _txtIgnoreSubstring.Text = _settings.IgnoreSubstring;

            _btnSave.Text = "Save"; _btnSave.Left = 440; _btnSave.Top = 320; _btnSave.Click += (_, __) => SaveAndClose();
            _btnCancel.Text = "Cancel"; _btnCancel.Left = 520; _btnCancel.Top = 320; _btnCancel.Click += (_, __) => Close();

            Controls.AddRange(new Control[] {
                lblFolder, _txtFolder, _btnBrowse,
                lblExt, _exts,
                lblMax, _maxBytes,
                _chkStartOnLogin, _chkIgnoreDot, lblIgnore, _txtIgnoreSubstring,
                _btnSave, _btnCancel
            });
        }

        private void BrowseFolder()
        {
            using var fbd = new FolderBrowserDialog { ShowNewFolderButton = false, Description = "Choose folder" };
            if (fbd.ShowDialog() == DialogResult.OK)
                _txtFolder.Text = fbd.SelectedPath;
        }

        private void SaveAndClose()
        {
            if (!string.IsNullOrWhiteSpace(_txtFolder.Text) && !Directory.Exists(_txtFolder.Text))
            {
                MessageBox.Show("Folder does not exist.", "Settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            _settings.FolderPath = _txtFolder.Text.Trim();
            _settings.MaxPasteBytes = (int)_maxBytes.Value;
            _settings.StartWithWindows = _chkStartOnLogin.Checked;
            _settings.IgnoreDotfiles = _chkIgnoreDot.Checked;
            _settings.AllowedExtensions = _exts.CheckedItems.Cast<string>().ToArray();
            _settings.IgnoreSubstring = _txtIgnoreSubstring.Text.Trim();

            _settings.Save();

            if (_settings.StartWithWindows) _startup.EnableStartup(); else _startup.DisableStartup();

            _index.Rescan();
            _index.RefreshWatcher();

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
