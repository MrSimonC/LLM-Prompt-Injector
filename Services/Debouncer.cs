using System;
using System.Threading;
using System.Threading.Tasks;

namespace FolderSnippets.Services
{
    internal sealed class Debouncer
    {
        private CancellationTokenSource? _cts;

        public void Run(TimeSpan delay, Action action)
        {
            _cts?.Cancel();
            var cts = _cts = new CancellationTokenSource();
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delay, cts.Token);
                    if (!cts.IsCancellationRequested)
                        action();
                }
                catch (TaskCanceledException) { }
            });
        }
    }
}
