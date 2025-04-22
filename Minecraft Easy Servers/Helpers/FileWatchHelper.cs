using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Easy_Servers.Helpers
{
    public static class FileWatchHelper
    {
        public static IFileWatch Start(string path, Action<string> onNewLine)
        {
            long lastPosition = 0;
            var cancellation = new CancellationTokenSource();

            var task = Task.Run(async () =>
            {
                while (!cancellation.Token.IsCancellationRequested)
                {
                    try
                    {
                        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                        stream.Seek(lastPosition, SeekOrigin.Begin);
                        using var reader = new StreamReader(stream);

                        string line;
                        while ((line = reader.ReadLine()) != null)
                            onNewLine(line);

                        lastPosition = stream.Position;
                    }
                    catch (IOException)
                    {
                        // fichier potentiellement verrouillé temporairement
                    }

                    await Task.Delay(200, cancellation.Token);
                }
            }, cancellation.Token);

            return new FileWatchDisposable(cancellation);
        }

        public interface IFileWatch
        {
            void Stop();
        }

        class FileWatchDisposable : IFileWatch
        {
            private readonly CancellationTokenSource _cancellation;

            public FileWatchDisposable(CancellationTokenSource cancellation)
            {
                _cancellation = cancellation;
            }

            public void Stop()
            {
                _cancellation.Cancel();
            }

            public void Dispose() {
                Stop();
            }
        }
    }
}
