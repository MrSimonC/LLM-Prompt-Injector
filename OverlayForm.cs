using FolderSnippets.Services;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using System.Windows.Forms;

namespace FolderSnippets
{
    internal sealed class OverlayForm : Form
    {
        private readonly AppSettings _settings;
        private readonly FileIndexService _index;
        private readonly PasteService _paste;

        private readonly TextBox _txtFilter = new();
        private readonly ListBox _list = new();
        private readonly Label _status = new();

        private List<FileItem> _view = new();
        private IntPtr _previousWindow = IntPtr.Zero;

        public OverlayForm(AppSettings settings, FileIndexService index, PasteService paste)
        {
            _settings = settings;
            _index = index;
            _paste = paste;

            Icon = UiResources.AppIcon;
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            TopMost = true;
            KeyPreview = true;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.White;
            Opacity = 0.98;

            Width = 800;
            Height = 480;

            _txtFilter.BorderStyle = BorderStyle.FixedSingle;
            _txtFilter.PlaceholderText = "Type to filter - Esc to close";
            _txtFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            _txtFilter.Location = new Point(12, 12);
            _txtFilter.Width = ClientSize.Width - 24;
            _txtFilter.TextChanged += (_, __) => ApplyFilter();
            _txtFilter.KeyDown += FilterKeyDown;

            _list.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _list.Location = new Point(12, 44);
            _list.Size = new Size(ClientSize.Width - 24, ClientSize.Height - 92);
            _list.IntegralHeight = false;
            _list.Font = new Font(FontFamily.GenericMonospace, 10);
            _list.KeyDown += ListKeyDown;
            _list.DoubleClick += (_, __) => InsertSelectedAsync();

            _status.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            _status.AutoSize = false;
            _status.Location = new Point(12, ClientSize.Height - 36);
            _status.Size = new Size(ClientSize.Width - 24, 24);
            _status.TextAlign = ContentAlignment.MiddleLeft;
            _status.ForeColor = Color.DimGray;

            Controls.AddRange(new Control[] { _txtFilter, _list, _status });

            Resize += (_, __) =>
            {
                _txtFilter.Width = ClientSize.Width - 24;
                _list.Size = new Size(ClientSize.Width - 24, ClientSize.Height - 92);
                _status.Location = new Point(12, ClientSize.Height - 36);
                _status.Size = new Size(ClientSize.Width - 24, 24);
            };

            Deactivate += (_, __) =>
            {
                // keep open unless user hits Esc - do nothing here
            };

            LoadList();
        }

        public void ShowOverlay()
        {
            if (string.IsNullOrWhiteSpace(_settings.FolderPath) || !Directory.Exists(_settings.FolderPath))
            {
                MessageBox.Show("Please choose a valid folder in Settings.", "FolderSnippets", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            _previousWindow = _paste.CaptureCurrentForegroundWindow();
            LoadList();
            ApplyFilter();

            CenterOnActiveScreen();
            Show();
            Activate();
            _txtFilter.Focus();
            _txtFilter.SelectAll();
        }

        private void CenterOnActiveScreen()
        {
            var screen = Screen.FromHandle(_previousWindow != IntPtr.Zero ? _previousWindow : Handle);
            var area = screen.WorkingArea;
            Left = area.Left + (area.Width - Width) / 2;
            Top = area.Top + (area.Height - Height) / 3;
        }

        private void LoadList()
        {
            _index.Rescan();
            _view = _index.Current.ToList();
            BindList(_view);
            UpdateStatus();
        }

        private void ApplyFilter()
        {
            _view = _index.Filter(_txtFilter.Text).ToList();
            BindList(_view);
            UpdateStatus();
        }

        private void BindList(IReadOnlyList<FileItem> items)
        {
            _list.BeginUpdate();
            _list.Items.Clear();
            foreach (var i in items)
                _list.Items.Add(i.Name);
            _list.EndUpdate();
            if (_list.Items.Count > 0)
                _list.SelectedIndex = 0;
        }

        private void UpdateStatus(string? message = null)
        {
            var count = _view.Count;
            var cap = $"{_settings.MaxPasteBytes:N0} bytes cap";
            _status.Text = message ?? $"{count} file(s) - {cap} - ↑/↓ select, Enter insert, Esc close, Ctrl+Backspace clear filter";
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                Hide();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void FilterKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Down)
            {
                MoveSelection(1);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Up)
            {
                MoveSelection(-1);
                e.Handled = true;
            }
            else if (e.Control && e.KeyCode == Keys.Back)
            {
                _txtFilter.Clear();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter)
            {
                InsertSelectedAsync();
                e.Handled = true;
            }
        }

        private void ListKeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                InsertSelectedAsync();
                e.Handled = true;
            }
        }

        private void MoveSelection(int delta)
        {
            if (_list.Items.Count == 0) return;
            var idx = _list.SelectedIndex;
            idx = Math.Max(0, Math.Min(_list.Items.Count - 1, idx + delta));
            _list.SelectedIndex = idx;
        }

        private async Task InsertSelectedAsync()
        {
            if (_list.SelectedIndex < 0 || _list.SelectedIndex >= _view.Count)
                return;

            var item = _view[_list.SelectedIndex];

            if (item.SizeBytes > _settings.MaxPasteBytes)
            {
                UpdateStatus($"Blocked - {item.Name} is {item.SizeBytes:N0} bytes which exceeds the {_settings.MaxPasteBytes:N0} cap");
                System.Media.SystemSounds.Exclamation.Play();
                return;
            }

            string text;
            try
            {
                // BOM aware read with fallback
                text = ReadTextWithFallback(item.FullPath);
            }
            catch (Exception ex)
            {
                UpdateStatus($"Failed to read {item.Name}: {ex.Message}");
                System.Media.SystemSounds.Hand.Play();
                return;
            }

            try
            {
                // hide briefly to avoid keystrokes landing here
                Hide();
                await Task.Delay(1000); // wait a moment to ensure previous window is ready
                _paste.PasteIntoWindowThenReturn(_previousWindow, text, this);
                UpdateStatus($"Inserted {item.Name} ({text.Length:N0} chars)");
            }
            catch (Exception ex)
            {
                Show();
                Activate();
                UpdateStatus($"Paste failed: {ex.Message}");
            }
        }

        private static string ReadTextWithFallback(string path)
        {
            // try default - .NET will respect BOM if present
            try { return File.ReadAllText(path); }
            catch { }

            // fallback to Windows-1252
            var win1252 = System.Text.Encoding.GetEncoding(1252);
            return File.ReadAllText(path, win1252);
        }
    }
}
