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

        public bool ExecuteJarAndStop(string jarPath, string stopSubString, string arguments)
        {
            bool ack = false;
            var javaPath = FindJavaInPath();
            if (javaPath == null)
                throw new ManagerException("Set java runtime in PATH for initializing the server");
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
                    ack = true;
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            return ack;
        }

        public bool ExecuteScriptAndStop(string command, string stopSubString, string errorSubstring)
        {
            var workingDirectory = Path.GetDirectoryName(command);
            var fileName = Path.GetFileName(command);
            bool ack = false;
            bool errorAck = false;
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = command,
                    Arguments = "",
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    UseShellExecute = false,
                    CreateNoWindow = false,
                    WorkingDirectory = workingDirectory
                }
            };
            process.OutputDataReceived += (sender, args) =>
            {
                if (args.Data == null) return;
                Console.WriteLine(args.Data);
                if (args.Data.Contains(stopSubString))
                {
                    process.StandardInput.Write("stop\n");
                    ack = true;
                }
                if (args.Data.Contains(errorSubstring))
                {
                    errorAck = true;
                }
                if (ack || errorAck)
                {
                    process.StandardInput.Write("k\n");
                }
            };

            process.Start();
            process.BeginOutputReadLine();
            process.WaitForExit();
            return ack;
        }

        public int? RunBackgroundScript(string name, string scriptPath, string scriptArgument, string ackSubString, string? errorSubString = null, bool killIfAckFailed = false)
        {
            var logStringBuilder = new StringBuilder();
            bool acknowledged = false;
            EventWaitHandle eventWaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = Path.GetFileName(scriptPath),
                    Arguments = $" {scriptArgument}",
                    UseShellExecute = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetDirectoryName(scriptPath)
                }
            };

            // ack with filewatch
            var stdOutPath = GetStdOutPath(scriptPath);
            File.Create(stdOutPath).Dispose(); // Creating empty file
            var watcher = FileWatchHelper.Start(stdOutPath, (newLine) =>
            {
                Console.WriteLine(newLine);
                logStringBuilder.AppendLine(newLine);
                if (newLine.Contains(ackSubString))
                {
                    acknowledged = true;
                    eventWaitHandle.Set();
                }
                if (errorSubString != null && newLine.Contains(errorSubString))
                {
                    eventWaitHandle.Set();
                }
            });


            process.Start();
            string pidFilePath = GetPidPath(scriptPath);
            File.WriteAllText(pidFilePath, process.Id.ToString());

            if (!ScriptStatus(scriptPath, out _))
                eventWaitHandle.Set();

            eventWaitHandle.WaitOne(TimeSpan.FromSeconds(20));
            var logPath = GetLogPath(scriptPath);
            File.WriteAllText(logPath, logStringBuilder.ToString());

            watcher.Stop();

            if (!acknowledged && killIfAckFailed)
            {
                Console.Error.WriteLine($"Command '{name}' failed to acknowledge. Process will be killed.");
                process.Kill();
                return null;
            }

            return process.Id;
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
                    Arguments = GetJavaShellArguments($"{javaArgument} -jar {Path.GetFileName(jarPath)} {jarArgument}"),
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

        private string GetJavaShellArguments(string arguments)
        {
            var javaPath = FindJavaInPath() ?? throw new ManagerException("No java.exe found in JAVA_HOME");
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => $"/C \"{javaPath}\" {arguments}",
                _ => $"-c \"{javaPath}\" {arguments}"
            };
        }
        
        private string GetRunScriptShellArguments(string arguments)
        {
            return Environment.OSVersion.Platform switch
            {
                PlatformID.Win32NT => $"/C {arguments}",
                _ => $"-c {arguments}"
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

        public bool ScriptStatus(string scriptPath, out int? pid)
        {
            pid = null;
            var pidPath = GetPidPath(scriptPath);
            if (!File.Exists(pidPath)) return false;
            var pidFromFile = int.Parse(File.ReadAllText(pidPath));
            pid = pidFromFile;
            var processes = Process.GetProcesses();
            return processes.FirstOrDefault(x => x.Id == pidFromFile) != null;
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
        private static string GetPidPath(string filePath)
        {
            return Path.Combine(Path.ChangeExtension(filePath, ".pid"));
        }

        public static string GetStdOutPath(string filePath)
        {
            return Path.Combine(Path.ChangeExtension(filePath, ".out"));
        }

        private static string GetLogPath(string filePath)
        {
            return Path.Combine(Path.ChangeExtension(filePath, ".log"));
        }
    }
}
