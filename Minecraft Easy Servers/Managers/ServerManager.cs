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
        public static string? RootPath { get; set; }
        public const string FolderName = "servers";
        private const string AssetsFolder = "Assets";
        private const int RCON_PORT_OFFSET = 100;

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

            UpdateMultiMCInstanceConfig(name);

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

                string defaultResourcePack = configManager.Read(configName).Server.ResourcePack;
                if (!string.IsNullOrEmpty(defaultResourcePack) && (defaultResourcePack.Contains("http") || defaultResourcePack.Contains("https")))
                {
                    Console.WriteLine($"Default resource pack found link in config {defaultResourcePack}.");
                    UpdateServerPropertiesValue(name, "resource-pack", defaultResourcePack!);
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
            UpdateServerPropertiesValue(name, "rcon.port", (port + RCON_PORT_OFFSET).ToString());
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
            if (RootPath != null)
                return Path.Combine(RootPath, FolderName, name);
            else
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

        // Debug purpose only.
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

        public async Task AddServerMod(string serverName, string modName, string link)
        {
            if (!ServerExists(serverName))
                throw new ManagerException($"Server with name {serverName} doesn't exist. To create it, run: $ add server {serverName}");

            string modsFolderPath = Path.Combine(GetFolderPath(serverName), "mods");
            if (!Directory.Exists(modsFolderPath))
                Directory.CreateDirectory(modsFolderPath);

            string? modFilePath = await DownloadOrCopyAssetAsync(
                destinationPath: modsFolderPath,
                assetName: modName,
                assetLink: link,
                searchForFileWithExtension: ".jar",
                contentIsFolder: false);

            if (modFilePath is null)
                throw new ManagerException($"Failed to add mod {modName} to server {serverName}.");

            // Sync to DIY Client
            string diyClientModsPath = Path.Combine(GetDiyClientPath(serverName), "mods");
            if (!Directory.Exists(diyClientModsPath))
                Directory.CreateDirectory(diyClientModsPath);
            File.Copy(modFilePath, Path.Combine(diyClientModsPath, Path.GetFileName(modFilePath)), true);

            // Sync to MultiMC Clients
            SyncModToMultiMCClients(serverName, modFilePath);

            Console.WriteLine($"Mod {modName} added to server {serverName}.");
        }

        public void RemoveServerMod(string serverName, string modName)
        {
            if (!ServerExists(serverName))
                throw new ManagerException($"Server with name {serverName} doesn't exist. To create it, run: $ add server {serverName}");

            string modsFolderPath = Path.Combine(GetFolderPath(serverName), "mods");
            string modFilePrefix = $"{modName}_";

            var modFiles = Directory.GetFiles(modsFolderPath, $"{modFilePrefix}*");
            if (modFiles.Length == 0)
                throw new ManagerException($"Mod {modName} not found in server {serverName}.");

            foreach (var modFile in modFiles)
                File.Delete(modFile);

            // Sync removal from DIY Client
            string diyClientModsPath = Path.Combine(GetDiyClientPath(serverName), "mods");
            var diyModFiles = Directory.GetFiles(diyClientModsPath, $"{modFilePrefix}*");
            foreach (var diyModFile in diyModFiles)
                File.Delete(diyModFile);

            // Sync removal from MultiMC Clients
            RemoveModFromMultiMCClients(serverName, modFilePrefix);

            Console.WriteLine($"Mod {modName} removed from server {serverName}.");
        }

        public async Task AddServerPlugin(string serverName, string pluginName, string link)
        {
            if (!ServerExists(serverName))
                throw new ManagerException($"Server with name {serverName} doesn't exist. To create it, run: $ add server {serverName}");

            string pluginsFolderPath = Path.Combine(GetFolderPath(serverName), "plugins");
            if (!Directory.Exists(pluginsFolderPath))
                Directory.CreateDirectory(pluginsFolderPath);

            string? pluginFilePath = await DownloadOrCopyAssetAsync(
                destinationPath: pluginsFolderPath,
                assetName: pluginName,
                assetLink: link,
                searchForFileWithExtension: ".jar",
                contentIsFolder: false);

            if (pluginFilePath is null)
                throw new ManagerException($"Failed to add plugin {pluginName} to server {serverName}.");

            Console.WriteLine($"Plugin {pluginName} added to server {serverName}.");
        }

        public void RemoveServerPlugin(string serverName, string pluginName)
        {
            if (!ServerExists(serverName))
                throw new ManagerException($"Server with name {serverName} doesn't exist. To create it, run: $ add server {serverName}");

            string pluginsFolderPath = Path.Combine(GetFolderPath(serverName), "plugins");
            string pluginFilePrefix = $"{pluginName}_";

            var pluginFiles = Directory.GetFiles(pluginsFolderPath, $"{pluginFilePrefix}*");
            if (pluginFiles.Length == 0)
                throw new ManagerException($"Plugin {pluginName} not found in server {serverName}.");

            foreach (var pluginFile in pluginFiles)
                File.Delete(pluginFile);

            Console.WriteLine($"Plugin {pluginName} removed from server {serverName}.");
        }

        public async Task SetServerWorld(string serverName, string link)
        {
            if (!ServerExists(serverName))
                throw new ManagerException($"Server with name {serverName} doesn't exist. To create it, run: $ add server {serverName}");

            string worldFolderPath = Path.Combine(GetFolderPath(serverName), "world");
            if (Directory.Exists(worldFolderPath))
                Directory.Delete(worldFolderPath, true);

            string? worldPath = await DownloadOrCopyAssetAsync(
                destinationPath: GetFolderPath(serverName),
                assetName: "world",
                assetLink: link,
                searchForFileWithExtension: null,
                contentIsFolder: true) ?? throw new ManagerException("World folder not found when downloading or retrieving asset.");

            UpdateServerPropertiesValue(serverName, "level-name", Path.GetFileName(worldPath));

            Console.WriteLine($"World set for server {serverName}.");
        }

        public void SetServerResourcePack(string serverName, string link)
        {
            if (!ServerExists(serverName))
                throw new ManagerException($"Server with name {serverName} doesn't exist. To create it, run: $ add server {serverName}");

            if (!link.StartsWith("http") && !link.StartsWith("https"))
                throw new ManagerException("Resource pack link must be a valid HTTP/HTTPS URL.");

            UpdateServerPropertiesValue(serverName, "resource-pack", link);
            Console.WriteLine($"Resource pack set for server {serverName}.");
        }

        public void SetServerProperty(string serverName, string keyValue)
        {
            if (!ServerExists(serverName))
                throw new ManagerException($"Server with name {serverName} doesn't exist. To create it, run: $ add server {serverName}");

            var keyValueParts = keyValue.Split('=', 2);
            if (keyValueParts.Length != 2)
                throw new ManagerException("Invalid key=value format.");

            string key = keyValueParts[0].Trim();
            string value = keyValueParts[1].Trim();

            UpdateServerPropertiesValue(serverName, key, value);
            Console.WriteLine($"Property {key} set to {value} for server {serverName}.");
        }

        public int GetPort(string name)
        {
            return int.Parse(GetServerPropertiesValue(name, "server-port"));
        }
        
        public int GetRconPort(string name)
        {
            return int.Parse(GetServerPropertiesValue(name, "rcon.port"));
        }

        //  Get server.properties value for key=value
        public string GetServerPropertiesValue(string name, string propertyKey)
        {
            if (!ServerExists(name))
                throw new ManagerException($"Server with name {name} doesn't exists. To create it, run: $ add server {name}");

            string serverPropertiesFilePath = GetOrCreateServerPropertiesPath(name);

            var properties = new Dictionary<string, string>();
            foreach (var line in File.ReadLines(serverPropertiesFilePath))
            {
                if (!string.IsNullOrWhiteSpace(line) && !line.StartsWith("#"))
                {
                    var keyValue = line.Split('=', 2);
                    if (keyValue.Length == 2)
                        properties[keyValue[0].Trim()] = keyValue[1].Trim();
                }
            }

            if (!properties.TryGetValue(propertyKey, out var value))
                throw new ManagerException($"Property {propertyKey} not found in server.properties for server {name}.");

            return value;
        }

        public bool UpdateServerPropertiesValue(string name, string propertyKey, string newValue)
        {
            if (!ServerExists(name))
                throw new ManagerException($"Server with name {name} doesn't exists. To create it, run: $ add server {name}");

            string serverPropertiesFilePath = GetOrCreateServerPropertiesPath(name);

            var lines = File.ReadAllLines(serverPropertiesFilePath);
            bool propertyUpdated = false;

            for (int i = 0; i < lines.Length; i++)
            {
                if (!string.IsNullOrWhiteSpace(lines[i]) && !lines[i].StartsWith("#"))
                {
                    var keyValue = lines[i].Split('=', 2);
                    if (keyValue.Length == 2 && keyValue[0].Trim() == propertyKey)
                    {
                        lines[i] = $"{propertyKey}={newValue}";
                        propertyUpdated = true;
                        break;
                    }
                }
            }

            if (!propertyUpdated)
            {
                using (var writer = File.AppendText(serverPropertiesFilePath))
                {
                    writer.WriteLine($"{propertyKey}={newValue}");
                }
            }
            else
            {
                File.WriteAllLines(serverPropertiesFilePath, lines);
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

        private static async Task<string?> DownloadOrCopyAssetAsync(
            string destinationPath,
            string assetName,
            string assetLink,
            string? searchForFileWithExtension,
            bool contentIsFolder = false)
        {
            string filePath;
            if (!(assetLink.Contains("http") || assetLink.Contains("https")))
            {
                if (!File.Exists(assetLink))
                    throw new ManagerException($"Asset link is not an URL nor a valid filePath");

                if ((searchForFileWithExtension != null && !assetLink.Contains(searchForFileWithExtension) && !contentIsFolder && !assetLink.Contains(".zip")))
                    throw new Exception($"Asset link {assetLink} does not contain the expected file extension {searchForFileWithExtension}");

                if (searchForFileWithExtension != null && assetLink.Contains(searchForFileWithExtension))
                {
                    // Copy file and finish.
                    string destPath = Path.Combine(destinationPath, $"{assetName}_{Path.GetFileName(assetLink)}");
                    File.Copy(assetLink, destPath, true);
                    return destPath;
                }
                else
                    filePath = assetLink;
            }
            else
                filePath = await MinecraftDownloader.DownloadFile(destinationPath, assetLink, prefixName: assetName);

            if (Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase) && searchForFileWithExtension != ".zip")
            {
                var contentPath = ArchiveHelper.ExtractArchiveAndIsolateContentAddPrefix(
                    archivePath: filePath,
                    directoryForContentPath: destinationPath,
                    prefixName: assetName,
                    searchForFileWithExtension: searchForFileWithExtension,
                    contentIsFolder: contentIsFolder);
                return contentPath;
            }

            // No asset file found.
            return null;
        }
        
        private static string GetMultiMCInstancesPath(string serverName, string platform)
        {
            return platform switch
            {
                "windows" => Path.Combine(GetMultiMCClientPath(serverName, platform), "instances"),
                "linux" => Path.Combine(GetMultiMCClientPath(serverName, platform), "bin", "instances"),
                "mac" => Path.Combine(GetMultiMCClientPath(serverName, platform), "Contents", "MacOS", "instances"),
                _ => throw new ManagerException($"Unsupported platform: {platform}")
            };
        }
        
        private static string GetMultiMCClientPath(string serverName, string platform)
        {
            return platform switch
            {
                "windows" => Path.Combine(GetMultiMCClientsPath(serverName),"windows_MultiMC"),
                "linux" => Path.Combine(GetMultiMCClientsPath(serverName), "linux_MultiMC"),
                "mac" => Path.Combine(GetMultiMCClientsPath(serverName), "mac_MultiMC.app"),
                _ => throw new ManagerException($"Unsupported platform: {platform}")
            };
        }

        private static string GetMultiMCModsPath(string instancePath)
        {
            return Path.Combine(instancePath, ".minecraft", "mods");
        }

        public void SyncModToMultiMCClients(string serverName, string modFilePath)
        {
            string[] platforms = { "windows", "linux", "mac" };
            foreach (var platform in platforms)
            {
                string instancesPath = GetMultiMCInstancesPath(serverName, platform);
                string? instancePath = Directory.EnumerateDirectories(instancesPath, $"{serverName}_*", SearchOption.AllDirectories).FirstOrDefault();
                if (instancePath != null)
                {
                    string multiMCModsPath = GetMultiMCModsPath(instancePath);
                    if (!Directory.Exists(multiMCModsPath))
                        Directory.CreateDirectory(multiMCModsPath);
                    File.Copy(modFilePath, Path.Combine(multiMCModsPath, Path.GetFileName(modFilePath)), true);
                }
            }
        }

        public void RemoveModFromMultiMCClients(string serverName, string modFilePrefix)
        {
            string[] platforms = { "windows", "linux", "mac" };
            foreach (var platform in platforms)
            {
                string instancesPath = GetMultiMCInstancesPath(serverName, platform);
                string? instancePath = Directory.EnumerateDirectories(instancesPath, $"{serverName}_*", SearchOption.AllDirectories).FirstOrDefault();
                if (instancePath != null)
                {
                    string multiMCModsPath = GetMultiMCModsPath(instancePath);
                    var multiMCModFiles = Directory.GetFiles(multiMCModsPath, $"{modFilePrefix}*");
                    foreach (var multiMCModFile in multiMCModFiles)
                        File.Delete(multiMCModFile);
                }
            }
        }
        public void UpdateMultiMCInstanceConfig(string serverName)
        {
            string[] platforms = { "windows", "linux", "mac" };
            foreach (var platform in platforms)
            {
                string instancesPath = GetMultiMCInstancesPath(serverName, platform);
                string? instancePath = Directory.EnumerateDirectories(instancesPath, $"*_*", SearchOption.AllDirectories).FirstOrDefault();

                if (instancePath != null)
                {
                    string newInstanceName = $"{serverName}_{Path.GetFileName(instancePath).Split('_', 2)[1]}";
                    string configFilePath = Path.Combine(instancePath, "instance.cfg");
                    if (File.Exists(configFilePath))
                    {
                        var lines = File.ReadAllLines(configFilePath);
                        for (int i = 0; i < lines.Length; i++)
                        {
                            if (lines[i].StartsWith("name="))
                            {
                                lines[i] = $"name={newInstanceName}";
                            }
                        }
                        File.WriteAllLines(configFilePath, lines);
                    }

                    // Rename the folder to ensure the first part matches the serverName  
                    string newInstancePath = Path.Combine(Path.GetDirectoryName(instancePath)!, newInstanceName);
                    if (!instancePath.Equals(newInstancePath, StringComparison.OrdinalIgnoreCase))
                    {
                        Directory.Move(instancePath, newInstancePath);
                    }
                }
            }
        }
    }
}
