using FolderSnippets.Services;
using System;
using System.Drawing;
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
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(720, 420);

            var root = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(12) };
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Row 1: Folder selector
            var rowFolder = new TableLayoutPanel { Dock = DockStyle.Top, ColumnCount = 3, AutoSize = true };            
            rowFolder.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            rowFolder.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            rowFolder.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            var lblFolder = new Label { Text = "Folder:", AutoSize = true, Anchor = AnchorStyles.Left };
            _txtFolder.Dock = DockStyle.Fill; _txtFolder.Margin = new Padding(8, 3, 8, 3); _txtFolder.Text = _settings.FolderPath;
            _btnBrowse.Text = "Browse..."; _btnBrowse.AutoSize = true; _btnBrowse.Click += (_, __) => BrowseFolder();
            rowFolder.Controls.Add(lblFolder, 0, 0);
            rowFolder.Controls.Add(_txtFolder, 1, 0);
            rowFolder.Controls.Add(_btnBrowse, 2, 0);
            root.Controls.Add(rowFolder);

            // Row 2: Main content split into two columns
            var split = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 12, 0, 0) };
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            split.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            var gbExt = new GroupBox { Text = "Extensions", Dock = DockStyle.Fill, Padding = new Padding(10) };
            _exts.Dock = DockStyle.Fill; _exts.CheckOnClick = true;
            var known = new[] { ".txt", ".md", ".markdown" };
            foreach (var e in known)
                _exts.Items.Add(e, _settings.AllowedExtensions.Contains(e, StringComparer.OrdinalIgnoreCase));
            gbExt.Controls.Add(_exts);
            split.Controls.Add(gbExt, 0, 0);

            var right = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 5, Padding = new Padding(6) };
            right.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            right.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var lblMax = new Label { Text = "Max paste bytes:", AutoSize = true, Anchor = AnchorStyles.Left };
            _maxBytes.Maximum = 10_000_000; _maxBytes.Minimum = 1_000; _maxBytes.Increment = 1_000; _maxBytes.Value = _settings.MaxPasteBytes; _maxBytes.Width = 160;
            right.Controls.Add(lblMax, 0, 0);
            right.Controls.Add(_maxBytes, 1, 0);

            _chkStartOnLogin.Text = "Start on Windows login"; _chkStartOnLogin.Checked = _settings.StartWithWindows; _chkStartOnLogin.AutoSize = true;
            right.Controls.Add(_chkStartOnLogin, 1, 1);

            _chkIgnoreDot.Text = "Ignore dotfiles"; _chkIgnoreDot.Checked = _settings.IgnoreDotfiles; _chkIgnoreDot.AutoSize = true;
            right.Controls.Add(_chkIgnoreDot, 1, 2);

            var lblIgnore = new Label { Text = "Ignore filenames containing:", AutoSize = true, Anchor = AnchorStyles.Left };
            _txtIgnoreSubstring.Text = _settings.IgnoreSubstring; _txtIgnoreSubstring.Width = 220;
            right.Controls.Add(lblIgnore, 0, 3);
            right.Controls.Add(_txtIgnoreSubstring, 1, 3);

            split.Controls.Add(right, 1, 0);
            root.Controls.Add(split);

            // Row 3: Buttons
            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.RightToLeft, Padding = new Padding(0, 12, 0, 0), AutoSize = true };
            _btnSave.Text = "Save"; _btnSave.AutoSize = true; _btnSave.Click += (_, __) => SaveAndClose();
            _btnCancel.Text = "Cancel"; _btnCancel.AutoSize = true; _btnCancel.Click += (_, __) => Close();
            buttons.Controls.Add(_btnSave);
            buttons.Controls.Add(_btnCancel);
            root.Controls.Add(buttons);

            AcceptButton = _btnSave;
            CancelButton = _btnCancel;

            Controls.Add(root);
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
