using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FolderSnippets.Services
{
    internal sealed class FileIndexService : IDisposable
    {
        private readonly AppSettings _settings;
        private FileSystemWatcher? _watcher;
        private readonly Debouncer _debounce = new();

        private readonly object _lock = new();
        private List<FileItem> _all = new();

        public FileIndexService(AppSettings settings)
        {
            _settings = settings;
            SetupWatcher();
        }

        public IReadOnlyList<FileItem> Current => _all;

        public void Rescan()
        {
            lock (_lock)
            {
                _all = ScanNow();
            }
        }

        private void SetupWatcher()
        {
            if (string.IsNullOrWhiteSpace(_settings.FolderPath) || !Directory.Exists(_settings.FolderPath))
                return;

            _watcher?.Dispose();
            _watcher = new FileSystemWatcher(_settings.FolderPath)
            {
                IncludeSubdirectories = false,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.Size | NotifyFilters.LastWrite
            };
            _watcher.Created += OnChanged;
            _watcher.Deleted += OnChanged;
            _watcher.Renamed += OnChanged;
            _watcher.Changed += OnChanged;
            _watcher.EnableRaisingEvents = true;
        }

        private void OnChanged(object? sender, FileSystemEventArgs e)
        {
            _debounce.Run(TimeSpan.FromMilliseconds(200), () =>
            {
                try { Rescan(); } catch { }
            });
        }

        private List<FileItem> ScanNow()
        {
            var results = new List<FileItem>();
            if (string.IsNullOrWhiteSpace(_settings.FolderPath) || !Directory.Exists(_settings.FolderPath))
                return results;

            var allowed = new HashSet<string>(_settings.AllowedExtensions.Select(x => x.ToLowerInvariant()));
            foreach (var path in Directory.EnumerateFiles(_settings.FolderPath))
            {
                try
                {
                    var name = Path.GetFileName(path);
                    if (_settings.IgnoreDotfiles && name.StartsWith("."))
                        continue;
                    if (!string.IsNullOrEmpty(_settings.IgnoreSubstring) && name.IndexOf(_settings.IgnoreSubstring, StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    if (!allowed.Contains(ext))
                        continue;

                    var fi = new FileInfo(path);
                    results.Add(new FileItem(name, fi.FullName, fi.Length));
                }
                catch { /* ignore individual file issues */ }
            }

            return results.OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase).ToList();
        }

        public IReadOnlyList<FileItem> Filter(string? query)
        {
            var q = query?.Trim() ?? "";
            if (q.Length == 0) return Current;
            return Current.Where(f => f.Name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0).ToList();
        }

        public void RefreshWatcher() => SetupWatcher();

        public void Dispose()
        {
            _watcher?.Dispose();
        }
    }

    internal sealed record FileItem(string Name, string FullPath, long SizeBytes);
}
