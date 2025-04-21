using Minecraft_Easy_Servers.Exceptions;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Easy_Servers.Managers
{
    public class ExecuteManager
    {
        public ExecuteManager()
        {
        }

        public void ExecuteJarAndStop(string jarPath, string stopSubString)
        {
            var javaPath = FindJavaInPath();
            if (javaPath == null)
                throw new ManagerException("Set java runtime in PATH for initializing the server");

            var arguments = $"-Xmx1G -Xms1G -jar {Path.GetFileName(jarPath)} nogui";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = javaPath,
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = Path.GetDirectoryName(jarPath)
                }
            };

            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data == null) return;
                Console.WriteLine(args.Data);

                if (args.Data.Contains(stopSubString))
                {
                    process.StandardInput.Write("stop\n");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
        }

        string? FindJavaInPath()
        {
            var pathEnv = Environment.GetEnvironmentVariable("JAVA_HOME");
            if (string.IsNullOrEmpty(pathEnv))
                throw new ManagerException("Set environment variable JAVA_HOME to your java installation folder");

            var files = Directory.GetFiles(pathEnv);

            return files
                .Where(x => x.Contains("java.exe"))
                .FirstOrDefault(File.Exists);
        }
    }
}
