using JsonFlatFileDataStore;
using Minecraft_Easy_Servers.Exceptions;
using Minecraft_Easy_Servers.Helpers;
using Minecraft_Easy_Servers.Managers.Models;
using System.Text.Json;

namespace Minecraft_Easy_Servers.Managers
{
    public class ConfigManager
    {
        public const string FolderName = "configs";
        private readonly ExecuteManager executeManager;

        public ConfigManager(ExecuteManager executeManager)
        {
            this.executeManager = executeManager;
        }

        public async Task CreateConfig(string name, string modloader, string version)
        {
            if (ConfigExists(name))
                throw new ManagerException($"Config with name {name} already exists. To remove it, run: $ remove-config {name}");

            Directory.CreateDirectory(GetFolderPath(name));

            // Create baseServer directory
            var baseServerPath = GetOrCreateBaseServerPath(name);

            // Create baseClient directory
            var baseManualClientPath = GetOrCreateBaseManualClientPath(name);

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

            // Downloading given modLoader
            if (modloader.Equals("forge", StringComparison.OrdinalIgnoreCase))
            {
                var forgeInstaller = await MinecraftDownloader.DownloadForgeMinecraftUniversalInstaller(version, GetFolderPath(name));
                Console.WriteLine($"Forge installer downloaded to {forgeInstaller}. Installing base server.");
                var ack = executeManager.ExecuteJarAndStop(forgeInstaller, "The server installed successfully", $"-jar {Path.GetFileName(forgeInstaller)} --installServer {baseServerPath}");
                if (!ack)
                {
                    Directory.Delete(GetFolderPath(name), true);
                    throw new ManagerException($"Forge installation failed. Please check the logs for more details.");
                }

                // Copy the forge installer jar to the baseClient directory
                var destinationPath = Path.Combine(baseManualClientPath, Path.GetFileName(forgeInstaller));
                File.Copy(forgeInstaller, destinationPath, true);

            }
            else if (modloader.Equals("neoforge", StringComparison.OrdinalIgnoreCase))
            {
                var neoforgeInstaller = await MinecraftDownloader.DownloadNeoforgeMinecraftUniversalInstaller(version, GetFolderPath(name));
                Console.WriteLine($"Neoforge installer downloaded to {neoforgeInstaller}. Installing base server.");
                var ack = executeManager.ExecuteJarAndStop(neoforgeInstaller, "The server installed successfully", $"-jar {Path.GetFileName(neoforgeInstaller)} --server-jar --server-install {baseServerPath}");
                if (!ack)
                {
                    Directory.Delete(GetFolderPath(name), true);
                    throw new ManagerException($"Neoforge installation failed. Please check the logs for more details.");
                }
                // Copy the neoforge installer jar to the baseClient directory
                var destinationPath = Path.Combine(baseManualClientPath, Path.GetFileName(neoforgeInstaller));

                // Prepare MultiMC clients
                var windowsMcClient = await MinecraftDownloader.DownloadMultiMCArchive("windows", baseMultiMCClientPath);
                var linuxMcClient = await MinecraftDownloader.DownloadMultiMCArchive("linux", baseMultiMCClientPath);
                var macMcClient = await MinecraftDownloader.DownloadMultiMCArchive("mac", baseMultiMCClientPath);

                var windowsFolderMcClient = ArchiveHelper.ExtractArchiveAndIsolateContentAddPrefix(destinationPath, windowsMcClient, "windows", searchForFileWithExtension: null, contentIsFolder: true);
                var linuxFolderMcClient = ArchiveHelper.ExtractArchiveAndIsolateContentAddPrefix(destinationPath, linuxMcClient, "linux", searchForFileWithExtension: null, contentIsFolder: true);
                var macFolderMcClient = ArchiveHelper.ExtractArchiveAndIsolateContentAddPrefix(destinationPath, macMcClient, "mac", searchForFileWithExtension: null, contentIsFolder: true);

                // Add the project /Assets/MultiMC files in a new directory in instances corresponding to configname and version
                var windowsMcClientPath = Path.Combine(windowsFolderMcClient, "instances", $"{name}_{version}");
                var linuxMcClientPath = Path.Combine(linuxFolderMcClient, "instances", $"{name}_{version}");
                var macMcClientPath = Path.Combine(macFolderMcClient, "instances", $"{name}_{version}");
                Directory.CreateDirectory(windowsMcClientPath);
                Directory.CreateDirectory(linuxMcClientPath);
                Directory.CreateDirectory(macMcClientPath);


                File.Copy(neoforgeInstaller, destinationPath, true);
            }
            else if (modloader.Equals("vanilla", StringComparison.OrdinalIgnoreCase))
            {
                var vanillaServerRunner = await MinecraftDownloader.DownloadVanillaMinecraftServer(version, GetFolderPath(name));
                Console.WriteLine($"Vanilla server jar downloaded to {vanillaServerRunner}");
            }
            else
            {
                throw new ManagerException($"Modloader {modloader} is not supported.");
            }

            Console.WriteLine($"Config {name} created.");
        }

        public ConfigJsonDb Read(string configName)
        {
            if (!ConfigExists(configName))
                throw new ManagerException($"Config with name {configName} doesn't exist. To create it, run: $ add-config {configName}");

            return JsonSerializer.Deserialize<ConfigJsonDb>(File.ReadAllText(GetConfigPath(configName))) ?? throw new ManagerException($"An error occured while deserializing config {configName}");
        }

        public void AddAsset(string name, string assetName, string link, string filePath, string collectionName)
        {
            var isDownloaded = link.Contains("http") || link.Contains("https");
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exists. To create it, run: $ add-config {name}");
            var store = new DataStore(GetConfigPath(name));
            var collection = store.GetCollection<Asset>(collectionName);
            var existingAsset = collection.Find(x => x.Name.Equals(assetName)).FirstOrDefault();
            if (existingAsset != null)
                throw new ManagerException($"{collectionName} with name {assetName} already exists. To remove it, run: $ remove-{collectionName.ToLower()} {name} {assetName}");

            var asset = new Asset()
            {
                Link = isDownloaded ? link : "file:"+filePath,
                Name = assetName
            };

            collection.InsertOne(asset);
            Console.WriteLine($"{collectionName} {assetName} added to config {name}.");
        }

        public async Task AddMod(string name, string modName, string link, ModTypeEnum modType)
        {
            var filePath = await DownloadOrCopyAssetAsync(configName: name, assetType: "mods", assetName: modName, assetLink: link, searchForFileWithExtension: ".jar");

            if (string.IsNullOrEmpty(filePath))
                throw new ManagerException($"Retrieving mod from {link} failed");

            AddAsset(name, modName, link, filePath, "mods");
            var configPath = GetConfigPath(name);
            var store = new DataStore(configPath);

            // Sync server and client config.
            if (modType == ModTypeEnum.SERVER || modType == ModTypeEnum.GLOBAL)
            {
                var serverConfig = store.GetItem<ServerConfig>("server") ?? new ServerConfig();
                serverConfig.Mods ??= new List<string>();
                if (!serverConfig.Mods.Contains(modName))
                {
                    serverConfig.Mods.Add(modName);
                    store.ReplaceItem("server", serverConfig);
                }
            }

            if (modType == ModTypeEnum.CLIENT || modType == ModTypeEnum.GLOBAL)
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

        public async Task AddPlugin(string name, string pluginName, string link)
        {
            var filePath = await DownloadOrCopyAssetAsync(
                configName: name,
                assetType: "plugins",
                assetName: pluginName,
                assetLink: link,
                searchForFileWithExtension: ".jar",
                contentIsFolder: false);
            if (string.IsNullOrEmpty(filePath))
                throw new ManagerException($"Retrieving plugin from {link} failed");
            AddAsset(name, pluginName, link, filePath, "plugins");
        }

        public async Task AddResourcePack(string name, string resourcePackName, string link, bool isServerDefault = false)
        {
            var filePath = await DownloadOrCopyAssetAsync(
                configName: name,
                assetType: "resourcePacks",
                assetName: resourcePackName,
                assetLink: link,
                searchForFileWithExtension: ".zip",
                contentIsFolder: false);
            if (string.IsNullOrEmpty(filePath))
                throw new ManagerException($"Retrieving resource pack from {link} failed");

            AddAsset(name, resourcePackName, link, filePath, "resourcePacks");

            var configPath = GetConfigPath(name);
            var store = new DataStore(configPath);
            var clientConfig = store.GetItem<ClientConfig>("client") ?? new ClientConfig();
            clientConfig.ResourcePacks ??= new List<string>();
            if (!clientConfig.ResourcePacks.Contains(resourcePackName))
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

        public async Task AddWorld(string name, string worldName, string link, bool isServerDefault = false)
        {
            var filePath = await DownloadOrCopyAssetAsync(
                configName: name,
                assetType: "worlds",
                assetName: worldName,
                assetLink: link,
                searchForFileWithExtension: null,
                contentIsFolder: true);
            if (string.IsNullOrEmpty(filePath))
                throw new ManagerException($"Retrieving world from {link} failed");

            AddAsset(name, worldName, link, filePath, "worlds");
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

            var clientConfig = store.GetItem<ClientConfig>("client") ?? new ClientConfig();
            clientConfig.Worlds ??= new List<string>();
            if (!clientConfig.Worlds.Contains(worldName))
            {
                clientConfig.Worlds.Add(worldName);
                store.ReplaceItem("client", clientConfig);
            }
        }

        private static string GetConfigPath(string name)
        {
            return Path.Combine(GetFolderPath(name), "config.json");
        }

        public void RemoveConfig(string name)
        {
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exists. To create it, run: $ add-config {name}");
            Directory.Delete(GetFolderPath(name), true);
            Console.WriteLine($"Config {name} removed.");
        }

        private static bool ConfigExists(string name)
        {
            return Directory.Exists(GetFolderPath(name));
        }

        private static string GetFolderPath(string name)
        {
            return Path.Combine(FolderName, name);
        }
        public void RemoveAsset(string name, string assetName, string collectionName)
        {
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exist. To create it, run: $ add-config {name}");

            var store = new DataStore(GetConfigPath(name));
            var collection = store.GetCollection<Asset>(collectionName);
            var existingAsset = collection.Find(x => x.Name.Equals(assetName)).FirstOrDefault();

            if (existingAsset == null)
                throw new ManagerException($"{collectionName} with name {assetName} doesn't exist. To add it, run: $ add-{collectionName.ToLower()} {name} {assetName}");

            var assetPath = GetOrCreateAssetFolderPath(name, collectionName);
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

            collection.DeleteOne(x => x.Name.Equals(assetName));
            Console.WriteLine($"{collectionName} {assetName} removed from config {name}.");
        }

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

        public void RemovePlugin(string name, string pluginName)
        {
            RemoveAsset(name, pluginName, "plugins");
        }

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
        public List<Asset> ListAssets(string configName, string assetName)
        {
            if (!ConfigExists(configName))
                throw new ManagerException($"Config with name {configName} doesn't exist. To create it, run: $ add-config {configName}");
            var store = new DataStore(GetConfigPath(configName));
            var collection = store.GetCollection<Asset>(assetName);
            return collection.AsQueryable().ToList();
        }

        // Download mods
        private async Task DownloadAssetsAsync(string configName, string assetName, string? searchForFileWithExtension = null, bool extractOnly = false)
        {
            var assets = ListAssets(configName, assetName)
                .Where(x => x.Link.Contains("http") || x.Link.Contains("https"));
            foreach (var asset in assets)
            {
                await DownloadOrCopyAssetAsync(configName, assetName, "", asset.Link, searchForFileWithExtension, extractOnly);
            }
        }

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
                    string destPath = Path.Combine(assetFolderPath, Path.GetFileName(assetLink));
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

        private static string GetOrCreateAssetFolderPath(string configName, string assetName)
        {
            var assetFolderPath = Path.Combine(GetFolderPath(configName), assetName);
            if (!Directory.Exists(assetFolderPath))
                Directory.CreateDirectory(assetFolderPath);

            return assetFolderPath;
        }

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
        private static string GetOrCreateBaseManualClientPath(string name)
        {
            var path = GetBaseManualClientPath(name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private static string GetOrCreateBaseMultiMCClientPath(string name)
        {
            var path = GetBaseMultiMCClientPath(name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private static string GetOrCreateBaseServerPath(string name)
        {
            var path = GetBaseServerPath(name);
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }

        private static string GetBaseManualClientPath(string name)
        {
            return Path.Combine(GetFolderPath(name), "baseManualClient");
        }

        private static string GetBaseMultiMCClientPath(string name)
        {
            return Path.Combine(GetFolderPath(name), "baseMultiMCClient");
        }

        private static string GetBaseServerPath(string name)
        {
            return Path.Combine(GetFolderPath(name), "baseServer");
        }
    }
}
