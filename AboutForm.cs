using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace FolderSnippets
{
    internal sealed class AboutForm : Form
    {
        public AboutForm()
        {
            Text = "About FolderSnippets";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = new Font("Segoe UI", 9F);
            ClientSize = new Size(520, 240);

            var fvi = FileVersionInfo.GetVersionInfo(Services.AppPaths.ExePath);
            var ver = fvi.ProductVersion ?? fvi.FileVersion ?? "Unknown";

            var icon = Services.UiResources.AppIcon;
            var picture = new PictureBox
            {
                Image = icon.ToBitmap(),
                SizeMode = PictureBoxSizeMode.Zoom,
                Width = 56,
                Height = 56,
                Margin = new Padding(0, 2, 16, 0)
            };

            var title = new Label
            {
                AutoSize = true,
                Font = new Font("Segoe UI", 13f, FontStyle.Bold),
                Text = "FolderSnippets"
            };

            var details = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.ControlText,
                Text = $"Version {ver}\nAuthor: Simon\nMade with ChatGPT"
            };

            var credit = new Label
            {
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Text = "Made by Simon and GPT5 on OpenAI Codex CLI."
            };

            var link = new LinkLabel
            {
                AutoSize = true,
                Text = "https://github.com/MrSimonC/LLM-Prompt-Injector"
            };
            link.Links.Add(0, link.Text.Length, link.Text);
            link.LinkClicked += (_, e) =>
            {
                var url = e.Link.LinkData as string ?? link.Text;
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); } catch { }
            };

            var contentLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(20)
            };
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            var textLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5
            };
            textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            textLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            textLayout.Controls.Add(title, 0, 0);
            textLayout.Controls.Add(details, 0, 1);
            textLayout.Controls.Add(credit, 0, 2);
            textLayout.Controls.Add(link, 0, 3);

            contentLayout.Controls.Add(picture, 0, 0);
            contentLayout.Controls.Add(textLayout, 1, 0);

            var ok = new Button { Text = "OK", DialogResult = DialogResult.OK, Width = 90, Height = 30 };
            var open = new Button { Text = "Open Link", Width = 100, Height = 30 };
            open.Click += (_, __) => { try { Process.Start(new ProcessStartInfo(link.Text) { UseShellExecute = true }); } catch { } };
            var copy = new Button { Text = "Copy details", Width = 110, Height = 30 };
            copy.Click += (_, __) =>
            {
                var s = $"FolderSnippets\nVersion: {ver}\nAuthor: Simon\nMade with ChatGPT\n{link.Text}";
                try { Clipboard.SetText(s); } catch { }
            };

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(16),
                Height = 58
            };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(open);
            buttons.Controls.Add(copy);

            AcceptButton = ok;
            CancelButton = ok;

            Controls.Add(contentLayout);
            Controls.Add(buttons);
        }
    }
}
