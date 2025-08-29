using System;
using System.Drawing;
using System.IO;

namespace FolderSnippets.Services
{
    internal static class UiResources
    {
        private static Icon? _appIcon;

        public static Icon AppIcon
        {
            get
            {
                if (_appIcon != null) return _appIcon;
                try
                {
                    if (File.Exists(AppPaths.IconPath))
                    {
                        using var fs = File.OpenRead(AppPaths.IconPath);
                        _appIcon = new Icon(fs);
                        return _appIcon;
                    }
                }
                catch { }

                try
                {
                    _appIcon = Icon.ExtractAssociatedIcon(AppPaths.ExePath) ?? SystemIcons.Application;
                }
                catch { _appIcon = SystemIcons.Application; }
                return _appIcon;
            }
        }
    }
}
