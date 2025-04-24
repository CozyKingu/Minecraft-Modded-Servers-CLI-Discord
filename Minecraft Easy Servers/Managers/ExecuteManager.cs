using Minecraft_Easy_Servers.Exceptions;
using Minecraft_Easy_Servers.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Easy_Servers.Managers
{
    public class ExecuteManager
    {
        Dictionary<string, Process> processWithKeys = new Dictionary<string, Process>();

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

        public int? RunBackgroundJar(string jarPath, string ackSubString, string errorSubString, string javaArgument, string jarArgument, bool killIfAckFailed = false)
        {
            var logStringBuilder = new StringBuilder();
            bool acknowledged = false;
            EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = GetShellFileName(),
                    Arguments = GetShellArguments($"{javaArgument} -jar {Path.GetFileName(jarPath)} {jarArgument}"),
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(jarPath),
                }
            };

            var stdOutPath = GetStdOutPath(jarPath);
            File.Create(stdOutPath).Dispose(); // Creating empty file
            var watcher = FileWatchHelper.Start(stdOutPath, (newLine) =>
            {
                logStringBuilder.AppendLine(newLine);
                if (newLine.Contains(ackSubString))
                {
                    acknowledged = true;
                    eventWaitHandle.Set();
                }

                if (newLine.Contains(errorSubString))
                {
                    eventWaitHandle.Set();
                }
            }); 

            process.Start();
            string pidFilePath = GetPidPath(jarPath);
            File.WriteAllText(pidFilePath, process.Id.ToString());
            eventWaitHandle.WaitOne(TimeSpan.FromMinutes(1));

            string logPath = GetLogPath(jarPath);
            File.WriteAllText(logPath, logStringBuilder.ToString());
            
            watcher.Stop();

            if (!acknowledged && killIfAckFailed)
            {
                Console.Error.WriteLine("Server jar launch failed to acknowledge. Java process will be killed.");
                process.Kill();
                return null;
            }

            return process.Id;
        }

        private static string GetShellFileName()
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => @"C:\Windows\System32\cmd.exe",
                _ => "/bin/bash"
            };
        }

        private string GetShellArguments(string arguments)
        {
            var javaPath = FindJavaInPath() ?? throw new ManagerException("No java.exe found in JAVA_HOME");
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => $"/C \"{javaPath}\" {arguments}",
                _ => $"-c \"{javaPath}\" {arguments}"
            };
        }

        public bool KillJarProcess(string jarPath)
        {
            if (!JarStatus(jarPath, out var pid))
                return false;

            var process = Process.GetProcessById(pid !.Value);
            process.Kill();
            return true;
        }

        public bool JarStatus(string jarPath, out int? pid)
        {
            pid = null;
            var pidPath = GetPidPath(jarPath);
            if (!File.Exists(pidPath)) return false;
            var pidFromFile = int.Parse(File.ReadAllText(pidPath));
            pid = pidFromFile;

            var processes = Process.GetProcesses();
            return processes.FirstOrDefault(x => x.Id == pidFromFile) != null;
        }

        public bool ExistsProcess(string key)
        {
            return false;
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
        private static string GetPidPath(string jarPath)
        {
            return Path.Combine(Path.ChangeExtension(jarPath, ".pid"));
        }

        public static string GetStdOutPath(string jarPath)
        {
            return Path.Combine(Path.ChangeExtension(jarPath, ".out"));
        }

        private static string GetLogPath(string jarPath)
        {
            return Path.Combine(Path.ChangeExtension(jarPath, ".log"));
        }
    }
}
