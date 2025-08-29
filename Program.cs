using System;
using System.Threading;
using System.Windows.Forms;

namespace FolderSnippets
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            // single instance via named mutex
            using var mutex = new Mutex(true, @"Global\FolderSnippets_SingleInstance", out bool isNew);
            if (!isNew)
                return;

            ApplicationConfiguration.Initialize();
            Application.Run(new TrayAppContext());
        }
    }
}
