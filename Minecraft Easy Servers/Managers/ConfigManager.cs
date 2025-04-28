namespace Minecraft_Easy_Servers.Managers
{
    using JsonFlatFileDataStore;
    using Minecraft_Easy_Servers.Exceptions;
    using Minecraft_Easy_Servers.Helpers;
    using Minecraft_Easy_Servers.Managers.Models;
    using System.Text.Json;

    /// <summary>
    /// Defines the <see cref="ConfigManager" />
    /// </summary>
    public class ConfigManager
    {
        /// <summary>
        /// Defines the FolderName
        /// </summary>
        public const string FolderName = "configs";

        /// <summary>
        /// Defines the executeManager
        /// </summary>
        private readonly ExecuteManager executeManager;

        /// <summary>
        /// Defines the AssetsFolder
        /// </summary>
        private const string AssetsFolder = "Assets";

        /// <summary>
        /// Initializes a new instance of the <see cref="ConfigManager"/> class.
        /// </summary>
        /// <param name="executeManager">The executeManager<see cref="ExecuteManager"/></param>
        public ConfigManager(ExecuteManager executeManager)
        {
            this.executeManager = executeManager;
        }

        /// <summary>
        /// The CreateConfig
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="modloader">The modloader<see cref="string"/></param>
        /// <param name="version">The version<see cref="string"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task CreateConfig(string name, string modloader, string version)
        {
            if (ConfigExists(name))
                throw new ManagerException($"Config with name {name} already exists. To remove it, run: $ remove-config {name}");

            Directory.CreateDirectory(GetFolderPath(name));

            // Create baseServer directory
            var baseServerPath = GetOrCreateBaseServerPath(name);

            // copy eula.txt to baseServer directory
            var eulaPath = Path.Combine(AssetsFolder, "eula.txt");
            if (!File.Exists(eulaPath))
            {
                throw new ManagerException($"EULA file not found in {baseServerPath}. Please check the installation.");
            }
            File.Copy(eulaPath, Path.Combine(baseServerPath, "eula.txt"), true);

            // Create baseClient directory
            var baseDIYClientPath = GetOrCreateBaseDIYClientPath(name);

            Directory.CreateDirectory(GetDIYClientAssetPath(name, "saves"));
            Directory.CreateDirectory(GetDIYClientAssetPath(name, "resourcepacks"));
            Directory.CreateDirectory(GetDIYClientAssetPath(name, "mods"));

            // Create baseMcClient directory
            var baseMultiMCClientPath = GetOrCreateBaseMultiMCClientPath(name);

            // Create json database.
            var configJsonDb = new ConfigJsonDb
            {
                ModLoader = modloader,
                Version = version
            };

            var json = JsonSerializer.Serialize(configJsonDb, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            var configPath = GetConfigPath(name);
            File.WriteAllText(configPath, json);

            try
            {
                // Downloading given modLoader
                if (modloader.Equals("forge", StringComparison.OrdinalIgnoreCase))
                {
                    var forgeInstaller = await MinecraftDownloader.DownloadForgeMinecraftUniversalInstaller(version, GetFolderPath(name));
                    // Extract forge version from forgeInstaller path when path contains forge-{mineVersion}-{forgeVersion}-installer.jar
                    var forgeVersion = Path.GetFileName(forgeInstaller).Split('-')[2];

                    Console.WriteLine($"Forge installer downloaded to {forgeInstaller}. Installing base server.");
                    var ack = executeManager.ExecuteJarAndStop(forgeInstaller, "The server installed successfully", $"-jar {Path.GetFileName(forgeInstaller)} --installServer baseServer");
                    if (!ack)
                    {
                        Directory.Delete(GetFolderPath(name), true);
                        throw new ManagerException($"Forge installation failed. Please check the logs for more details.");
                    }

                    // Copy the forge installer jar to the baseClient directory
                    var destinationPath = Path.Combine(baseDIYClientPath, Path.GetFileName(forgeInstaller));
                    File.Copy(forgeInstaller, destinationPath, true);

                    await PrepareMultiMCClients(
                        name: name,
                        modloader: modloader,
                        modloaderVersion: forgeVersion,
                        version: version,
                        baseMultiMCClientPath: baseMultiMCClientPath);

                }
                else if (modloader.Equals("neoforge", StringComparison.OrdinalIgnoreCase))
                {
                    var neoforgeInstaller = await MinecraftDownloader.DownloadNeoforgeMinecraftUniversalInstaller(version, GetFolderPath(name));
                    // Extract neoforge version from neoforgeInstaller path when path contains neoforge-{neoforgeVersion}-installer.jar
                    var neoforgeVersion = Path.GetFileName(neoforgeInstaller).Split('-')[1];

                    Console.WriteLine($"Neoforge installer downloaded to {neoforgeInstaller}. Installing base server.");
                    var ack = executeManager.ExecuteJarAndStop(neoforgeInstaller, "The server installed successfully", $"-jar {Path.GetFileName(neoforgeInstaller)} --install-server baseServer");
                    if (!ack)
                    {
                        Directory.Delete(GetFolderPath(name), true);
                        throw new ManagerException($"Neoforge installation failed. Please check the logs for more details.");
                    }

                    // Copy the neoforge installer jar to the baseClient directory
                    var destinationPath = Path.Combine(baseDIYClientPath, Path.GetFileName(neoforgeInstaller));
                    File.Copy(neoforgeInstaller, destinationPath, true);

                    await PrepareMultiMCClients(
                        name: name,
                        modloader: modloader,
                        modloaderVersion: neoforgeVersion,
                        version: version,
                        baseMultiMCClientPath: baseMultiMCClientPath);

                }
                else if (modloader.Equals("vanilla", StringComparison.OrdinalIgnoreCase))
                {
                    var vanillaServerRunner = await MinecraftDownloader.DownloadVanillaMinecraftServer(version, baseServerPath);
                    Console.WriteLine($"Vanilla server jar downloaded to {vanillaServerRunner}");
                    await PrepareMultiMCClients(name, modloader, version, "", baseMultiMCClientPath);
                }
                else
                {
                    throw new ManagerException($"Modloader {modloader} is not supported.");
                }

                Console.WriteLine($"Config {name} created.");
            }
            catch (Exception ex)
            {
                // Clean up in case of failure
                Directory.Delete(GetFolderPath(name), true);
                throw;
            }
        }

        /// <summary>
        /// The PrepareMultiMCClients
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="modloader">The modloader<see cref="string"/></param>
        /// <param name="modloaderVersion">The modloaderVersion<see cref="string"/></param>
        /// <param name="version">The version<see cref="string"/></param>
        /// <param name="baseMultiMCClientPath">The baseMultiMCClientPath<see cref="string"/></param>
        /// <returns>The <see cref="Task"/></returns>
        private static async Task PrepareMultiMCClients(string name, string modloader, string modloaderVersion, string version, string baseMultiMCClientPath)
        {
            Console.WriteLine("Preparing MultiMC clients...");

            // Prepare MultiMC clients for each OS
            var osList = new[] { "windows", "linux", "mac" };
            var osClientPaths = new Dictionary<string, string>();

            foreach (var os in osList)
            {
                var mcClient = await MinecraftDownloader.DownloadMultiMCArchive(os, baseMultiMCClientPath);
                var folderMcClient = ArchiveHelper.ExtractArchiveAndIsolateContentAddPrefix(mcClient, baseMultiMCClientPath, os, searchForFileWithExtension: null, contentIsFolder: true);

                var mcClientNewInstancePath = Path.Combine(folderMcClient, "instances", $"{name}_{version}");
                Directory.CreateDirectory(mcClientNewInstancePath);

                // Create .minecraft and subdirectories
                var minecraftPath = Path.Combine(mcClientNewInstancePath, ".minecraft");
                Directory.CreateDirectory(minecraftPath);
                Directory.CreateDirectory(Path.Combine(minecraftPath, "mods"));
                Directory.CreateDirectory(Path.Combine(minecraftPath, "resourcepacks"));
                Directory.CreateDirectory(Path.Combine(minecraftPath, "saves"));

                File.Copy(Path.Combine(AssetsFolder, "MultiMC", "instance.cfg"), Path.Combine(mcClientNewInstancePath, "instance.cfg"), true);
                ReplaceInstanceName(Path.Combine(mcClientNewInstancePath, "instance.cfg"), $"{name}_{version}");

                osClientPaths[os] = mcClientNewInstancePath;
            }

            var mmcPackPath = Path.Combine(AssetsFolder, "MultiMC", $"mmc-pack-{modloader}.json");

            // Replace Minecraft version and modloader version in mmc-pack.json
            var tempMmcPackPath = Path.GetTempFileName();
            File.Copy(mmcPackPath, tempMmcPackPath, true);
            ReplaceMinecraftVersion(tempMmcPackPath, version);
            ReplaceModLoaderVersion(tempMmcPackPath, modloader, modloaderVersion);

            foreach (var os in osList)
            {
                File.Copy(tempMmcPackPath, Path.Combine(osClientPaths[os], "mmc-pack.json"), true);
            }

            File.Delete(tempMmcPackPath);
        }

        /// <summary>
        /// The AddAssetToClient
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="version">The version<see cref="string"/></param>
        /// <param name="assetType">The assetType<see cref="string"/></param>
        /// <param name="assetName">The assetName<see cref="string"/></param>
        /// <param name="assetPath">The assetPath<see cref="string"/></param>
        public void AddAssetToClient(string name, string version, string assetType, string assetName, string assetPath)
        {
            var osList = new[] { "windows", "linux", "mac" };
            var assetFolder = GetFolderNameFromAssetTypeClient(assetType);

            foreach (var os in osList)
            {
                // Handle MultiMC client
                string multiMCOsFolder = $"{os}_MultiMC" + (os == "mac" ? ".app" : string.Empty);
                var multiMCInstanceAssetPath = Path.Combine(GetBaseMultiMCClientPath(name), multiMCOsFolder, "instances", $"{name}_{version}", ".minecraft", assetFolder);
                if (!Directory.Exists(multiMCInstanceAssetPath))
                    throw new ManagerException($"MultiMC instance path for {os} does not exist. Ensure the client is prepared.");

                CopyAsset(assetPath, multiMCInstanceAssetPath);
            }

            // Handle DIY client
            string diyClientAssetPath = GetOrCreateDIYClientAssetPath(name, assetFolder);
            CopyAsset(assetPath, diyClientAssetPath);

            Console.WriteLine($"Asset {assetName} added to {assetFolder} for all clients.");
        }

        /// <summary>
        /// The GetOrCreateDIYClientAssetPath
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="assetType">The assetType<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GetOrCreateDIYClientAssetPath(string name, string assetFolder)
        {
            string diyClientAssetPath = GetDIYClientAssetPath(name, assetFolder);
            if (!Directory.Exists(diyClientAssetPath))
                Directory.CreateDirectory(diyClientAssetPath);
            return diyClientAssetPath;
        }

        /// <summary>
        /// The GetDIYClientAssetPath
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="assetFolder">The assetType<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GetDIYClientAssetPath(string name, string assetFolder)
        {
            return Path.Combine(GetBaseManualClientPath(name), assetFolder.ToLower());
        }

        /// <summary>
        /// The RemoveAssetFromClient
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="version">The version<see cref="string"/></param>
        /// <param name="assetType">The assetType<see cref="string"/></param>
        /// <param name="assetName">The assetName<see cref="string"/></param>
        public void RemoveAssetFromClient(string name, string version, string assetType, string assetName)
        {
            var assetFolder = GetFolderNameFromAssetTypeClient(assetType);
            var osList = new[] { "windows", "linux", "mac" };

            foreach (var os in osList)
            {
                // Handle MultiMC client
                string multiMCOsFolder = $"{os}_MultiMC" + (os == "mac" ? ".app" : string.Empty);
                var multiMCInstanceAssetPath = Path.Combine(GetBaseMultiMCClientPath(name), multiMCOsFolder, "instances", $"{name}_{version}", ".minecraft", assetFolder);
                if (!Directory.Exists(multiMCInstanceAssetPath))
                    throw new ManagerException($"MultiMC instance path for {os} does not exist. Ensure the client is prepared.");

                RemoveAsset(multiMCInstanceAssetPath, assetName);
            }

            // Handle DIY client
            var diyClientAssetPath = GetDIYClientAssetPath(name, assetFolder);
            if (!Directory.Exists(diyClientAssetPath))
                throw new ManagerException($"DIY asset client path for {name} does not exist. Ensure the client is prepared.");

            RemoveAsset(diyClientAssetPath, assetName);

            Console.WriteLine($"Asset {assetName} removed from {assetFolder} for all clients.");
        }

        /// <summary>
        /// The CopyAsset
        /// </summary>
        /// <param name="sourcePath">The sourcePath<see cref="string"/></param>
        /// <param name="destinationPath">The destinationPath<see cref="string"/></param>
        private static void CopyAsset(string sourcePath, string destinationPath, bool destinationIsTheCopiedAsset = false)
        {
            if (File.Exists(sourcePath))
            {
                File.Copy(sourcePath, Path.Combine(destinationPath, Path.GetFileName(sourcePath)), true);
            }
            else if (Directory.Exists(sourcePath))
            {
                if (Directory.Exists(destinationPath))
                    Directory.Delete(destinationPath, true);
                var copiedFolderPath = destinationIsTheCopiedAsset ? destinationPath : Path.Combine(destinationPath, Path.GetFileName(sourcePath));
                if (!Directory.Exists(copiedFolderPath))
                    Directory.CreateDirectory(copiedFolderPath);

                foreach (var dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, copiedFolderPath));
                }

                foreach (var filePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                {
                    File.Copy(filePath, filePath.Replace(sourcePath, copiedFolderPath), true);
                }
            }
            else
            {
                throw new ManagerException($"Asset path {sourcePath} does not exist.");
            }
        }

        /// <summary>
        /// The RemoveAsset
        /// </summary>
        /// <param name="assetPath">The assetPath<see cref="string"/></param>
        /// <param name="assetName">The assetName<see cref="string"/></param>
        private static void RemoveAsset(string assetPath, string assetName)
        {
            // Rechercher les fichiers correspondant au préfixe assetName
            var matchingFiles = Directory.GetFiles(assetPath, $"{assetName}_*");
            var matchingDirectories = Directory.GetDirectories(assetPath, $"{assetName}_*");

            // Supprimer les fichiers correspondants
            if (matchingFiles.Any())
            {
                foreach (var file in matchingFiles)
                {
                    File.Delete(file);
                }
            }

            // Supprimer les dossiers correspondants
            if (matchingDirectories.Any())
            {
                foreach (var directory in matchingDirectories)
                {
                    Directory.Delete(directory, true);
                }
            }

            // Si aucun fichier ou dossier correspondant n'est trouvé, lever une exception
            if (!matchingFiles.Any() && !matchingDirectories.Any())
            {
                Console.WriteLine($"Warning: Asset {assetName} not found in {assetPath}.");
            }
        }

        // Add Asset to baseServer directory (no .minecraft)

        /// <summary>
        /// The AddAssetToBaseServer
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="assetPath">The assetPath<see cref="string"/></param>
        /// <param name="assetType">The assetType<see cref="string"/></param>
        public void AddAssetToBaseServer(string name, string assetPath, string assetType)
        {
            var baseServerPath = GetBaseServerPath(name);
            if (!Directory.Exists(baseServerPath))
                throw new ManagerException($"Base server path for {name} does not exist. Ensure the server is prepared.");
            string assetFolderName = assetType.ToLower();
            var baseServerAssetPath = assetType == "worlds" ? Path.Combine(GetBaseServerPath(name), Path.GetFileName(assetPath)) : GetOrCreateBaseServerAssetPath(name, assetFolderName);
            CopyAsset(assetPath, baseServerAssetPath, destinationIsTheCopiedAsset: assetType == "worlds");
        }

        // Remove Asset from baseServer directory (no .minecraft)

        /// <summary>
        /// The RemoveAssetFromBaseServer
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="assetType">The assetType<see cref="string"/></param>
        /// <param name="assetName">The assetName<see cref="string"/></param>
        public void RemoveAssetFromBaseServer(string name, string assetType, string assetName)
        {
            var baseServerPath = GetBaseServerPath(name);
            if (!Directory.Exists(baseServerPath))
                throw new ManagerException($"Base server path for {name} does not exist. Ensure the server is prepared.");
            string assetFolder = assetType.ToLower();
            var baseServerAssetPath = assetType == "worlds" ? GetBaseServerPath(name) :  GetBaseServerAssetPath(name, assetFolder);
            if (!Directory.Exists(baseServerAssetPath))
            {
                Console.WriteLine($"Base server asset path for {name} does not exist.");
                return;
            }

            RemoveAsset(baseServerAssetPath, assetName);
        }

        /// <summary>
        /// The ReplaceMinecraftVersion
        /// </summary>
        /// <param name="filePath">The filePath<see cref="string"/></param>
        /// <param name="version">The version<see cref="string"/></param>
        private static void ReplaceMinecraftVersion(string filePath, string version)
        {
            var content = File.ReadAllText(filePath);
            content = content.Replace("{minecraftVersion}", version);
            File.WriteAllText(filePath, content);
        }

        /// <summary>
        /// The ReplaceInstanceName
        /// </summary>
        /// <param name="filePath">The filePath<see cref="string"/></param>
        /// <param name="instanceName">The instanceName<see cref="string"/></param>
        private static void ReplaceInstanceName(string filePath, string instanceName)
        {
            var content = File.ReadAllText(filePath);
            content = content.Replace("{instanceName}", instanceName);
            File.WriteAllText(filePath, content);
        }

        /// <summary>
        /// The ReplaceModLoaderVersion
        /// </summary>
        /// <param name="filePath">The filePath<see cref="string"/></param>
        /// <param name="modloader">The modloader<see cref="string"/></param>
        /// <param name="modLoaderVersion">The modLoaderVersion<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string ReplaceModLoaderVersion(string filePath, string modloader, string modLoaderVersion)
        {
            var content = File.ReadAllText(filePath);
            content = content.Replace($"{{{modloader}Version}}", modLoaderVersion);
            File.WriteAllText(filePath, content);
            return filePath;
        }

        /// <summary>
        /// The Read
        /// </summary>
        /// <param name="configName">The configName<see cref="string"/></param>
        /// <returns>The <see cref="ConfigJsonDb"/></returns>
        public ConfigJsonDb Read(string configName)
        {
            if (!ConfigExists(configName))
                throw new ManagerException($"Config with name {configName} doesn't exist. To create it, run: $ add-config {configName}");

            return JsonSerializer.Deserialize<ConfigJsonDb>(File.ReadAllText(GetConfigPath(configName))) ?? throw new ManagerException($"An error occured while deserializing config {configName}");
        }

        /// <summary>
        /// The AddAsset
        /// </summary>
        /// <param name="configName">The configName<see cref="string"/></param>
        /// <param name="assetName">The assetName<see cref="string"/></param>
        /// <param name="assetLink">The assetLink<see cref="string"/></param>
        /// <param name="filePath">The filePath<see cref="string"/></param>
        /// <param name="assetType">The assetType<see cref="string"/></param>
        /// <param name="addToClient"></param>
        /// <param name="addToServer"></param>
        public void AddAsset(string configName, string assetName, string assetLink, string filePath, string assetType, bool addToClient, bool addToServer)
        {
            var isDownloaded = assetLink.Contains("http") || assetLink.Contains("https");
            if (!ConfigExists(configName))
                throw new ManagerException($"Config with name {configName} doesn't exists. To create it, run: $ add-config {configName}");
            var store = new DataStore(GetConfigPath(configName));
            var collection = store.GetCollection<Asset>(assetType);
            var existingAsset = collection.Find(x => x.Name.Equals(assetName)).FirstOrDefault();
            if (existingAsset != null)
                throw new ManagerException($"{assetType} with name {assetName} already exists. To remove it, run: $ remove-{assetType.ToLower()} {configName} {assetName}");

            // Add asset to base server
            if (addToServer)
                AddAssetToBaseServer(configName, filePath, assetType);

            // Add asset to clients
            if (addToClient)
                AddAssetToClient(configName, Read(configName).Version, assetType, assetName, filePath);

            var asset = new Asset()
            {
                Link = isDownloaded ? assetLink : "file:" + filePath,
                Name = assetName
            };

            collection.InsertOne(asset);
            Console.WriteLine($"{assetType} {assetName} added to config {configName}.");
        }

        /// <summary>
        /// The AddMod
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="modName">The modName<see cref="string"/></param>
        /// <param name="link">The link<see cref="string"/></param>
        /// <param name="modType">The modType<see cref="ModTypeEnum"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task AddMod(string name, string modName, string link, ModTypeEnum modType)
        {
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exists. To create it, run: $ add-config {name}");

            var filePath = await DownloadOrCopyAssetAsync(configName: name, assetType: "mods", assetName: modName, assetLink: link, searchForFileWithExtension: ".jar");

            if (string.IsNullOrEmpty(filePath))
                throw new ManagerException($"Retrieving mod from {link} failed");

            bool isModForServer = modType == ModTypeEnum.SERVER || modType == ModTypeEnum.GLOBAL;
            bool isModForClient = modType == ModTypeEnum.CLIENT || modType == ModTypeEnum.GLOBAL;


            AddAsset(name, modName, link, filePath, "mods", isModForClient, isModForServer);
            var configPath = GetConfigPath(name);
            var store = new DataStore(configPath);

            // Sync server and client config.
            if (isModForServer)
            {
                var serverConfig = store.GetItem<ServerConfig>("server") ?? new ServerConfig();
                serverConfig.Mods ??= new List<string>();
                if (!serverConfig.Mods.Contains(modName))
                {
                    serverConfig.Mods.Add(modName);
                    store.ReplaceItem("server", serverConfig);
                }
            }

            if (isModForClient)
            {
                var clientConfig = store.GetItem<ClientConfig>("client") ?? new ClientConfig();
                clientConfig.Mods ??= new List<string>();
                if (!clientConfig.Mods.Contains(modName))
                {
                    clientConfig.Mods.Add(modName);
                    store.ReplaceItem("client", clientConfig);
                }
            }

            Console.WriteLine($"Mod {modName} added to {modType.ToString().ToLower()} configuration for {name}.");
        }

        /// <summary>
        /// The AddPlugin
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="pluginName">The pluginName<see cref="string"/></param>
        /// <param name="link">The link<see cref="string"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task AddPlugin(string name, string pluginName, string link)
        {
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exists. To create it, run: $ add-config {name}");
            var filePath = await DownloadOrCopyAssetAsync(
                configName: name,
                assetType: "plugins",
                assetName: pluginName,
                assetLink: link,
                searchForFileWithExtension: ".jar",
                contentIsFolder: false);
            if (string.IsNullOrEmpty(filePath))
                throw new ManagerException($"Retrieving plugin from {link} failed");
            AddAsset(name, pluginName, link, filePath, "plugins", addToClient: false, addToServer: true);
        }

        /// <summary>
        /// The AddResourcePack
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="resourcePackName">The resourcePackName<see cref="string"/></param>
        /// <param name="link">The link<see cref="string"/></param>
        /// <param name="isServerDefault">The isServerDefault<see cref="bool"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task AddResourcePack(string name, string resourcePackName, string link, bool isServerDefault = false)
        {
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exists. To create it, run: $ add-config {name}");
            var filePath = await DownloadOrCopyAssetAsync(
                configName: name,
                assetType: "resourcePacks",
                assetName: resourcePackName,
                assetLink: link,
                searchForFileWithExtension: ".zip",
                contentIsFolder: false);
            if (string.IsNullOrEmpty(filePath))
                throw new ManagerException($"Retrieving resource pack from {link} failed");

            if (isServerDefault)
            {
                // Removing default resource pack from server config.
                RemoveAssetFromBaseServer(name, "resourcePacks", Read(name).Server.ResourcePack);
            }

            AddAsset(name, resourcePackName, link, filePath, "resourcePacks", !isServerDefault, isServerDefault);

            var configPath = GetConfigPath(name);
            var store = new DataStore(configPath);
            var clientConfig = store.GetItem<ClientConfig>("client") ?? new ClientConfig();
            clientConfig.ResourcePacks ??= new List<string>();
            if (!clientConfig.ResourcePacks.Contains(resourcePackName) && !isServerDefault)
            {
                clientConfig.ResourcePacks.Add(resourcePackName);
                store.ReplaceItem("client", clientConfig);
            }
            if (isServerDefault)
            {
                // Update the server.resource_pack property
                var config = store.GetItem<ServerConfig>("server");
                config.ResourcePack = resourcePackName;
                store.ReplaceItem("server", config);
                Console.WriteLine($"Resource pack {resourcePackName} set as default for config {name}.");
            }
        }

        /// <summary>
        /// The AddWorld
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="worldName">The worldName<see cref="string"/></param>
        /// <param name="link">The link<see cref="string"/></param>
        /// <param name="isServerDefault">The isServerDefault<see cref="bool"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task AddWorld(string name, string worldName, string link, bool isServerDefault = false)
        {
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exists. To create it, run: $ add-config {name}");
            var filePath = await DownloadOrCopyAssetAsync(
                configName: name,
                assetType: "worlds",
                assetName: worldName,
                assetLink: link,
                searchForFileWithExtension: null,
                contentIsFolder: true);
            if (string.IsNullOrEmpty(filePath))
                throw new ManagerException($"Retrieving world from {link} failed");

            if (isServerDefault && !string.IsNullOrEmpty(Read(name).Server.DefaultWorld))
            {
                // Removing default resource pack from server config.
                RemoveAssetFromBaseServer(name, "worlds", Read(name).Server.DefaultWorld);
            }

            AddAsset(name, worldName, link, filePath, "worlds", !isServerDefault, isServerDefault);
            var configPath = GetConfigPath(name);
            var store = new DataStore(configPath);

            if (isServerDefault)
            {
                // Update the server.default_world property
                var config = store.GetItem<ServerConfig>("server");
                config.DefaultWorld = worldName;
                store.ReplaceItem("server", config);
                Console.WriteLine($"World {worldName} set as default for config {name}.");
            }
            else
            {
                var clientConfig = store.GetItem<ClientConfig>("client") ?? new ClientConfig();
                clientConfig.Worlds ??= new List<string>();
                if (!clientConfig.Worlds.Contains(worldName))
                {
                    clientConfig.Worlds.Add(worldName);
                    store.ReplaceItem("client", clientConfig);
                }
            }
        }

        /// <summary>
        /// The GetConfigPath
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GetConfigPath(string name)
        {
            return Path.Combine(GetFolderPath(name), "config.json");
        }

        /// <summary>
        /// The RemoveConfig
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        public void RemoveConfig(string name)
        {
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exists. To create it, run: $ add-config {name}");
            Directory.Delete(GetFolderPath(name), true);
            Console.WriteLine($"Config {name} removed.");
        }

        /// <summary>
        /// The ConfigExists
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <returns>The <see cref="bool"/></returns>
        public static bool ConfigExists(string name)
        {
            return Directory.Exists(GetFolderPath(name));
        }

        /// <summary>
        /// The GetFolderPath
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GetFolderPath(string name)
        {
            return Path.Combine(FolderName, name);
        }

        /// <summary>
        /// The RemoveAsset
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="assetName">The assetName<see cref="string"/></param>
        /// <param name="assetType">The assetType<see cref="string"/></param>
        public void RemoveAsset(string name, string assetName, string assetType)
        {
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exist. To create it, run: $ add-config {name}");

            var store = new DataStore(GetConfigPath(name));
            var collection = store.GetCollection<Asset>(assetType);
            var existingAsset = collection.Find(x => x.Name.Equals(assetName)).FirstOrDefault();

            if (existingAsset == null)
                throw new ManagerException($"{assetType} with name {assetName} doesn't exist. To add it, run: $ add-{assetType.ToLower()} {name} {assetName}");

            var assetPath = GetOrCreateAssetFolderPath(name, assetType);
            // Remove asset file or asset directory to assetPath/assetName
            var assetFilePath = Path.Combine(assetPath, assetName);
            var matchingFiles = Directory.GetFiles(assetPath, $"{assetName}_*");
            var matchingDirectories = Directory.GetDirectories(assetPath, $"{assetName}_*");
            if (matchingFiles.Any())
            {
                foreach (var file in matchingFiles)
                {
                    File.Delete(file);
                }
            }
            else if (matchingDirectories.Any())
            {
                foreach (var directory in matchingDirectories)
                {
                    Directory.Delete(directory, true);
                }
            }
            else
            {
                Console.WriteLine($"Warning: Asset {assetName} not found in {assetPath}.");
            }

            RemoveAssetFromBaseServer(name, assetType, assetName);
            RemoveAssetFromClient(name, Read(name).Version, assetType, assetName);

            collection.DeleteOne(x => x.Name.Equals(assetName));
            Console.WriteLine($"{assetType} {assetName} removed from config {name}.");
        }

        /// <summary>
        /// The RemoveMod
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="modName">The modName<see cref="string"/></param>
        public void RemoveMod(string name, string modName)
        {
            RemoveAsset(name, modName, "mods");

            var configPath = GetConfigPath(name);
            var store = new DataStore(configPath);

            // Remove from server configuration if present
            var serverConfig = store.GetItem<ServerConfig>("server");
            if (serverConfig?.Mods != null && serverConfig.Mods.Contains(modName))
            {
                serverConfig.Mods.Remove(modName);
                store.ReplaceItem("server", serverConfig);
            }

            // Remove from client configuration if present
            var clientConfig = store.GetItem<ClientConfig>("client");
            if (clientConfig?.Mods != null && clientConfig.Mods.Contains(modName))
            {
                clientConfig.Mods.Remove(modName);
                store.ReplaceItem("client", clientConfig);
            }

            Console.WriteLine($"Mod {modName} removed from all configurations for {name}.");
        }

        /// <summary>
        /// The RemovePlugin
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="pluginName">The pluginName<see cref="string"/></param>
        public void RemovePlugin(string name, string pluginName)
        {
            RemoveAsset(name, pluginName, "plugins");
        }

        /// <summary>
        /// The RemoveResourcePack
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="resourcePackName">The resourcePackName<see cref="string"/></param>
        public void RemoveResourcePack(string name, string resourcePackName)
        {
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exist. To create it, run: $ add-config {name}");

            var configPath = GetConfigPath(name);
            var store = new DataStore(configPath);

            // Check if the resource pack is the default one
            var config = store.GetItem<ServerConfig>("server");
            if (config.ResourcePack == resourcePackName)
            {
                config.ResourcePack = string.Empty; // Remove the default resource pack
                store.ReplaceItem("server", config);
                Console.WriteLine($"Resource pack {resourcePackName} was the default and has been removed from the default property.");
            }

            // Remove from client configuration if present
            var clientConfig = store.GetItem<ClientConfig>("client");
            if (clientConfig?.ResourcePacks != null && clientConfig.ResourcePacks.Contains(resourcePackName))
            {
                clientConfig.ResourcePacks.Remove(resourcePackName);
                store.ReplaceItem("client", clientConfig);
            }

            RemoveAsset(name, resourcePackName, "resourcePacks");
        }

        // List asset by asset type. Give Asset objects correspond to the collection

        /// <summary>
        /// The ListAssets
        /// </summary>
        /// <param name="configName">The configName<see cref="string"/></param>
        /// <param name="assetName">The assetName<see cref="string"/></param>
        /// <returns>The <see cref="List{Asset}"/></returns>
        public List<Asset> ListAssets(string configName, string assetName)
        {
            if (!ConfigExists(configName))
                throw new ManagerException($"Config with name {configName} doesn't exist. To create it, run: $ add-config {configName}");
            var store = new DataStore(GetConfigPath(configName));
            var collection = store.GetCollection<Asset>(assetName);
            return collection.AsQueryable().ToList();
        }

        /// <summary>
        /// The DownloadOrCopyAssetAsync
        /// </summary>
        /// <param name="configName">The configName<see cref="string"/></param>
        /// <param name="assetType">The assetType<see cref="string"/></param>
        /// <param name="assetName">The assetName<see cref="string"/></param>
        /// <param name="assetLink">The assetLink<see cref="string"/></param>
        /// <param name="searchForFileWithExtension">The searchForFileWithExtension<see cref="string?"/></param>
        /// <param name="contentIsFolder">The contentIsFolder<see cref="bool"/></param>
        /// <returns>The <see cref="Task{string?}"/></returns>
        private static async Task<string?> DownloadOrCopyAssetAsync(string configName, string assetType, string assetName, string assetLink, string? searchForFileWithExtension, bool contentIsFolder = false)
        {
            var assetFolderPath = GetOrCreateAssetFolderPath(configName, assetType);
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
                    string destPath = Path.Combine(assetFolderPath, $"{assetName}_{Path.GetFileName(assetLink)}");
                    File.Copy(assetLink, destPath, true);
                    return destPath;
                }
                else
                    filePath = assetLink;
            }
            else
                filePath = await MinecraftDownloader.DownloadFile(assetFolderPath, assetLink, prefixName: assetName);

            if (Path.GetExtension(filePath).Equals(".zip", StringComparison.OrdinalIgnoreCase) && searchForFileWithExtension != ".zip")
            {
                var contentPath = ArchiveHelper.ExtractArchiveAndIsolateContentAddPrefix(
                    archivePath: filePath,
                    directoryForContentPath: assetFolderPath,
                    prefixName: assetName,
                    searchForFileWithExtension: searchForFileWithExtension,
                    contentIsFolder: contentIsFolder);
                return contentPath;
            }

            // No asset file found.
            return null;
        }

        /// <summary>
        /// The RemoveWorld
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <param name="worldName">The worldName<see cref="string"/></param>
        public void RemoveWorld(string name, string worldName)
        {
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exist. To create it, run: $ add-config {name}");

            var configPath = GetConfigPath(name);
            var store = new DataStore(configPath);

            // Check if the world is the default one
            var config = store.GetItem<ServerConfig>("server");
            if (config.DefaultWorld == worldName)
            {
                config.DefaultWorld = string.Empty; // Remove the default world
                store.ReplaceItem("server", config);
                Console.WriteLine($"World {worldName} was the default and has been removed from the default property.");
            }
            // Remove from client configuration if present
            var clientConfig = store.GetItem<ClientConfig>("client");
            if (clientConfig?.Worlds != null && clientConfig.Worlds.Contains(worldName))
            {
                clientConfig.Worlds.Remove(worldName);
                store.ReplaceItem("client", clientConfig);
            }

            RemoveAsset(name, worldName, "worlds");
        }

        /// <summary>
        /// The GetOrCreateBaseDIYClientPath
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GetOrCreateBaseDIYClientPath(string name)
        {
            var path = GetBaseManualClientPath(name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// The GetOrCreateAssetFolderPath
        /// </summary>
        /// <param name="configName">The configName<see cref="string"/></param>
        /// <param name="assetName">The assetName<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GetOrCreateAssetFolderPath(string configName, string assetName)
        {
            var assetFolderPath = Path.Combine(GetFolderPath(configName), "allAssets", assetName.ToLower());
            if (!Directory.Exists(assetFolderPath))
                Directory.CreateDirectory(assetFolderPath);

            return assetFolderPath;
        }

        /// <summary>
        /// The GetOrCreateBaseServerAssetPath
        /// </summary>
        /// <param name="assetFolderName">The assetType<see cref="string"/></param>
        /// <param name="baseServerPath">The baseServerPath<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GetOrCreateBaseServerAssetPath(string configName, string assetFolderName)
        {
            var serverAssetPath = GetBaseServerAssetPath(configName, assetFolderName);
            if (!Directory.Exists(serverAssetPath))
                Directory.CreateDirectory(serverAssetPath);
            return serverAssetPath;
        }

        private static string GetBaseServerAssetPath(string configName, string assetFolderName)
        {
            return Path.Combine(GetBaseServerPath(configName), assetFolderName);
        }

        /// <summary>
        /// The GetOrCreateBaseMultiMCClientPath
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GetOrCreateBaseMultiMCClientPath(string name)
        {
            var path = GetBaseMultiMCClientPath(name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        /// <summary>
        /// The GetOrCreateBaseServerPath
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string GetOrCreateBaseServerPath(string name)
        {
            var path = GetBaseServerPath(name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private static string GetFolderNameFromAssetTypeClient(string assetType)
        {
            return assetType.ToLower() switch
            {
                "worlds" => "saves",
                _ => assetType.ToLower()
            };
        }

        /// <summary>
        /// The GetBaseManualClientPath
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        public static string GetBaseManualClientPath(string name)
        {
            return Path.Combine(GetFolderPath(name), "baseManualClient");
        }

        /// <summary>
        /// The GetBaseMultiMCClientPath
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        public static string GetBaseMultiMCClientPath(string name)
        {
            return Path.Combine(GetFolderPath(name), "baseMultiMCClient");
        }

        /// <summary>
        /// The GetBaseServerPath
        /// </summary>
        /// <param name="name">The name<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        public static string GetBaseServerPath(string name)
        {
            return Path.Combine(GetFolderPath(name), "baseServer");
        }
    }
}
