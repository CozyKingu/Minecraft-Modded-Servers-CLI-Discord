using Kajabity.Tools.Java;
using Microsoft.Extensions.Primitives;
using Minecraft_Easy_Servers.Exceptions;
using Minecraft_Easy_Servers.Helpers;

namespace Minecraft_Easy_Servers.Managers
{
    public class ServerManager
    {
        private readonly ExecuteManager executeManager;
        private readonly CommandManager commandManager;
        private readonly ConfigManager configManager;
        public const string FolderName = "servers";
        private const string AssetsFolder = "Assets";

        public ServerManager(ExecuteManager executeManager, CommandManager commandManager, ConfigManager configManager)
        {
            this.executeManager = executeManager;
            this.commandManager = commandManager;
            this.configManager = configManager;
        }

        public async Task CreateVanillaServer(
            string name,
            string version)
        {
            if (ServerExists(name))
                throw new ManagerException($"Server with name {name} already exists. To remove it, run: $ remove server {name}");

            Directory.CreateDirectory(GetFolderPath(name));
            const string eulaAsset = "eula.txt";
            CopyAssetToFolder(GetFolderPath(name), eulaAsset);

            await MinecraftDownloader.DownloadVanillaMinecraftServer(version, GetFolderPath(name));
            Console.WriteLine($"Download of server version {version} finished. Initializing server with first boot-up...");

            var success = FirstRunServerHardMode(name);
        }

        public async Task CreateServer(
            string name,
            string configName)
        {
            if (ServerExists(name))
                throw new ManagerException($"Server with name {name} already exists. To remove it, run: $ remove server {name}");
            Directory.CreateDirectory(GetFolderPath(name));
            Console.WriteLine($"Download of server version {configName} finished. Initializing server with first boot-up...");

            if (!ConfigManager.ConfigExists(configName))
                throw new ManagerException($"Config with name {configName} doesn't exists. To create it, run: $ add config {configName}");

            var configBaseServerPath = ConfigManager.GetBaseServerPath(configName);
            var diyClientPath = ConfigManager.GetBaseManualClientPath(configName);
            var multiMCClientsPath = ConfigManager.GetBaseMultiMCClientPath(configName);

            CopyDirectory(configBaseServerPath, GetFolderPath(name));
            CopyDirectory(diyClientPath, GetDiyClientPath(name));
            CopyDirectory(multiMCClientsPath, GetMultiMCClientsPath(name));

            CopyAssetToFolder(GetFolderPath(name), "server.properties");

            try
            {
                string modloader = configManager.Read(configName).ModLoader;

                string defaultworld = configManager.Read(configName).Server.DefaultWorld;
                if (!string.IsNullOrEmpty(defaultworld))
                {
                    Console.WriteLine($"Default world found in config {defaultworld}.");
                    var worldDirectoryPath = Directory.EnumerateDirectories(GetFolderPath(name), $"{defaultworld}_*").FirstOrDefault();
                    if (worldDirectoryPath is null)
                        throw new ManagerException($"The configuration {defaultworld} is not found in {name} server folder");

                    UpdateServerPropertiesValue(name, "level-name", Path.GetFileName(worldDirectoryPath)!);
                }

                // TODO: Give a check to resource pack because not sent to client.
                string defaultResourcePack = configManager.Read(configName).Server.ResourcePack;
                if (!string.IsNullOrEmpty(defaultResourcePack))
                {
                    Console.WriteLine($"Default resource pack found in config {defaultResourcePack}.");
                    var resourcePackDirectoryPath = Directory.EnumerateFiles(Path.Combine(GetFolderPath(name), "resourcepacks"), $"{defaultResourcePack}_*").FirstOrDefault();
                    if (resourcePackDirectoryPath is null)
                        throw new ManagerException($"The resource pack {defaultResourcePack} is not found in {name} server folder");
                    UpdateServerPropertiesValue(name, "resource-pack", resourcePackDirectoryPath!);
                }

                if (modloader == "vanilla")
                {
                    var success = FirstRunServerHardMode(name);
                    if (!success)
                        throw new ManagerException("Server first run failed. Server removed.");
                }
                else
                {
                    var success = FirstRunServerHardMode(name); // Running server by applying usual process.
                    if (!success)
                        throw new ManagerException("Server first run failed. Server removed.");
                }
            } catch (Exception)
            {
                await RemoveServer(name);
                throw;
            }

            await Task.CompletedTask;
        }

        // Method to copy folder Folder1/ Folder2/ keeping structure
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            if (!Directory.Exists(sourceDir))
                throw new ManagerException($"Source directory {sourceDir} doesn't exists.");

            if (!Directory.Exists(destDir))
                Directory.CreateDirectory(destDir);
            foreach (var file in Directory.GetFiles(sourceDir))
            {
                var destFile = Path.Combine(destDir, Path.GetFileName(file));
                File.Copy(file, destFile, true);
            }
            foreach (var directory in Directory.GetDirectories(sourceDir))
            {
                var destDirectory = Path.Combine(destDir, Path.GetFileName(directory));
                CopyDirectory(directory, destDirectory);
            }
        }


        private static bool ServerExists(string name)
        {
            return Directory.Exists(GetFolderPath(name));
        }

        public void UpServer(string name, int port)
        {
            if (!ServerExists(name))
                throw new ManagerException($"Server with name {name} doesn't exists. To create it, run: $ add server {name}");

            if (StatusServer(name).Result != ServerStatus.NONE)
                throw new ManagerException($"Server with name {name} is already running. To stop it, run: $ down {name}");

            UpdateServerPropertiesValue(name, "motd", name);
            UpdateServerPropertiesValue(name, "server-port", port.ToString());
            UpdateServerPropertiesValue(name, "query.port", port.ToString());
            UpdateServerPropertiesValue(name, "rcon.port", (port + 10).ToString());
            UpdateServerPropertiesValue(name, "enable-rcon", "true");
            UpdateServerPropertiesValue(name, "rcon.password", "password");

            string serverJar = GetServerJar(name);
            string scriptAbsolutePath = GetServerRunAbsoluteScriptPath(name);

            if (File.Exists(scriptAbsolutePath))
            {
                // Remove :exit line and pause line from script
                var lines = File.ReadAllLines(scriptAbsolutePath);
                var newLines = lines.Where(x => !x.Contains("pause")).ToArray();
                File.WriteAllLines(scriptAbsolutePath, newLines);

                // Add nogui parameter in script before %*
                var scriptContent = File.ReadAllText(scriptAbsolutePath);
                if (!scriptContent.Contains("nogui %*"))
                {
                    scriptContent = scriptContent.Replace("%*", " nogui %*");
                    File.WriteAllText(scriptAbsolutePath, scriptContent);
                }
            }

            if (File.Exists(scriptAbsolutePath))
            {
                var pid = executeManager.RunBackgroundScript(
                    name: name,
                    scriptPath: scriptAbsolutePath,
                    scriptArgument: $" > {Path.GetFileName(ExecuteManager.GetStdOutPath(scriptAbsolutePath))} 2>&1",
                    ackSubString: "Done",
                    errorSubString: null,
                    killIfAckFailed: true);
                if (pid is null)
                    throw new ManagerException($"Server up command failed. Script execution failed.");
                Console.WriteLine($"Server script with PID {pid} is running.");
                return;
            }
            else if (File.Exists(serverJar))
            {
                var pid = executeManager.RunBackgroundJar(
                    jarPath: serverJar,
                    ackSubString: "Done",
                    errorSubString: "/ERROR",
                    javaArgument: $"-Xmx1G -Xms1G",
                    jarArgument: $"nogui > {Path.GetFileName(ExecuteManager.GetStdOutPath(serverJar))}",
                    killIfAckFailed: true);
                if (pid is null)
                    throw new ManagerException($"Server up command failed. Jar execution failed.");
                Console.WriteLine($"Server jar with PID {pid} is running.");
            }
            else
                throw new ManagerException($"No run script or jar file in {name} server folder");
        }

        public async Task RemoveServer(string name)
        {
            if (!ServerExists(name))
                throw new ManagerException($"Server with name {name} doesn't exists.");

            if (await StatusServer(name) != ServerStatus.NONE)
                throw new ManagerException($"Server with name {name} is  running. Stop it before removing it. run: $ down {name}");

            InternalRemoveServer(name);
        }

        public async Task DownServer(string name)
        {
            if (!ServerExists(name))
                throw new ManagerException($"Server with name {name} doesn't exists. To create it, run: $ add server {name}");

            if (await StatusServer(name) == ServerStatus.NONE)
                throw new ManagerException($"Server with name {name} is not running. To run it, run: $ up {name}");

            var response = await commandManager.StopServer(GetRconPort(name), "password");
            if (response is null)
                executeManager.KillJarProcess(GetServerJar(name));
            else
                Thread.Sleep(2000);

                var newStatus = await StatusServer(name);
            if (newStatus != ServerStatus.NONE)
                Console.WriteLine($"Server failed to shutdown. Force shutdown from task manager (java.exe).");
            else
                Console.WriteLine($"Server stopped.");
        }

        public async Task<ServerStatus> StatusServer(string name)
        {
            if (!ServerExists(name))
                return ServerStatus.NONE;

            try
            {
                string serverScript = GetServerRunAbsoluteScriptPath(name);
                string serverJar = GetServerJar(name);
                if (File.Exists(serverScript))
                {
                    var status = executeManager.ScriptStatus(serverScript, out _);
                    if (!status)
                        return ServerStatus.NONE;
                }
                else if (File.Exists(serverJar))
                {
                    var jarStatus = executeManager.JarStatus(serverJar, out _);
                    if (!jarStatus)
                        return ServerStatus.NONE;
                }
            }
            catch (Exception)
            {
                return ServerStatus.NONE;
            }

            var serverStatus = await commandManager.GetStatus(GetRconPort(name), "password");
            return serverStatus != null ? ServerStatus.LISTENING : ServerStatus.PROCESS_RUNNING;
        }

        private static string GetFolderPath(string name)
        {
            return Path.Combine(FolderName, name);
        }

        private bool FirstRunServerHardMode(string name)
        {
            string runScriptPath = GetServerRunAbsoluteScriptPath(name);
            string serverJar = GetServerJar(name);
            if (File.Exists(runScriptPath))
            {
                // Run
                // Remove :exit line and pause line from script
                if (File.Exists(runScriptPath))
                {
                    var lines = File.ReadAllLines(runScriptPath);
                    var newLines = lines.Where(x => !x.Contains("pause")).ToArray();
                    File.WriteAllLines(runScriptPath, newLines);

                    // Add nogui parameter in script before %*
                    var scriptContent = File.ReadAllText(runScriptPath);
                    if (!scriptContent.Contains("nogui %*"))
                    {
                        scriptContent = scriptContent.Replace("%*", " nogui %*");
                        File.WriteAllText(runScriptPath, scriptContent);
                    }
                }
                return executeManager.ExecuteScriptAndStop(runScriptPath, "Done", "/ERROR", "All dimensions are saved"); // server can hang on "All dimensions are saved"
            }
            else if (File.Exists(serverJar))
            {
                return executeManager.ExecuteJarAndStop(serverJar, "Done", $"-Xmx1G -Xms1G -jar {Path.GetFileName(serverJar)} nogui");
            }
            else
            {
                throw new ManagerException($"No run script or jar file in {name} server folder");
            }
        }

        private async Task<bool> FirstRunServerSoft(string name)
        {
            try
            {
                UpServer(name, 25565);
                var status = await StatusServer(name);
                if (status == ServerStatus.NONE)
                {
                    Console.WriteLine("Server failed to run.");
                } else if (status == ServerStatus.PROCESS_RUNNING)
                {
                    Console.WriteLine("Server process is running, but is not listening. RCON is not working properly, maybe to misconfigurations.");
                }
                else if (status == ServerStatus.LISTENING)
                {
                    Console.WriteLine("Server is listening on its first run ! Congrats.");
                }

                if (status == ServerStatus.PROCESS_RUNNING || status == ServerStatus.LISTENING)
                {
                    Console.WriteLine("Server first run finished. Shutting down server...");
                    await DownServer(name);
                }
                return true;
            } catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public int GetPort(string name)
        {
            return int.Parse(GetServerPropertiesValue(name, "server-port"));
        }
        
        public int GetRconPort(string name)
        {
            return int.Parse(GetServerPropertiesValue(name, "rcon.port"));
        }

        public string GetServerPropertiesValue(string name, string propertyKey)
        {
            if (!ServerExists(name))
                throw new ManagerException($"Server with name {name} doesn't exists. To create it, run: $ add server {name}");
            string serverPropertiesFilePath = GetOrCreateServerPropertiesPath(name);
            var properties = new JavaProperties();

            using (FileStream streamIn = new(serverPropertiesFilePath, FileMode.Open))
            {
                properties.Load(streamIn);
            }

            return properties.GetValueOrDefault(propertyKey) ?? throw new ManagerException($"Property {propertyKey} is missing in {name} server server.properties");
        }

        public bool UpdateServerPropertiesValue(string name, string propertyKey, string newValue)
        {
            if (!ServerExists(name))
                throw new ManagerException($"Server with name {name} doesn't exists. To create it, run: $ add server {name}");
            string serverPropertiesFilePath = GetOrCreateServerPropertiesPath(name);
            var properties = new JavaProperties();
            using (FileStream streamIn = new(serverPropertiesFilePath, FileMode.Open))
            {
                properties.Load(streamIn);
            }

            properties.SetProperty(propertyKey, newValue);
            using (FileStream streamOut = new(serverPropertiesFilePath, FileMode.Open))
            {
                properties.Store(streamOut);
            }

            return true;
        }

        private static string GetOrCreateServerPropertiesPath(string name)
        {
            string serverPropertiesFilePath = Path.Combine(GetFolderPath(name), "server.properties");
            if (!File.Exists(serverPropertiesFilePath))
            {
                File.Create(serverPropertiesFilePath).Dispose();
            }
            return serverPropertiesFilePath;
        }

        private static string GetServerJar(string name)
        {
            var jarFiles = Directory.GetFiles(GetFolderPath(name), "*.jar");
            var serverJar = jarFiles.Where(x => x.Contains("server")).FirstOrDefault() ?? throw new ManagerException($"No jar file in {name} server folder");
            return serverJar;
        }

        private static string GetServerRunAbsoluteScriptPath(string name)
        {
            string scriptExtension = OperatingSystem.IsWindows() ? "bat" : "sh";
            return Path.GetFullPath(Path.Combine(GetFolderPath(name), $"run.{scriptExtension}"));
        }


        private static string GetDiyClientPath(string name)
        {
            return Path.Combine(GetFolderPath(name), "clients", "diyClient");
        }

        private static string GetMultiMCClientsPath(string name)
        {
            return Path.Combine(GetFolderPath(name), "clients", "multiMCClients");
        }

        private void InternalRemoveServer(string name)
        {
            Directory.Delete(GetFolderPath(name), recursive: true);
        }

        private static void CopyAssetToFolder(string folderPath, string assetFile)
        {
            File.Copy(Path.Combine(AssetsFolder, assetFile), Path.Combine(folderPath, assetFile));
        }
    }
}
