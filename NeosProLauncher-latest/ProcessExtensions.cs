using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace NeosProLauncher
{
    public static class ProcessExtensions
    {
        public static async Task WaitForExitAsync(
          this Process process,
          CancellationToken cancellationToken = default(CancellationToken))
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += new EventHandler(Process_Exited);
            try
            {
                if (process.HasExited) return;
                else
                {
                    CancellationTokenRegistration tokenRegistration = cancellationToken.Register((Action)(() => tcs.TrySetCanceled()));
                    try
                    {
                        int num = await tcs.Task ? 1 : 0;
                    }
                    finally
                    {
                        tokenRegistration.Dispose();
                    }
                    tokenRegistration = new CancellationTokenRegistration();
                }
            }
            finally
            {
                process.Exited -= new EventHandler(Process_Exited);
            }

            void Process_Exited(object sender, EventArgs e) => tcs.TrySetResult(true);
        }
    }
}