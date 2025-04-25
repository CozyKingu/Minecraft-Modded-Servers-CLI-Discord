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
        public ConfigManager()
        {
        }

        public void CreateConfig(string name, string modloader, string version)
        {
            if (ConfigExists(name))
                throw new ManagerException($"Config with name {name} already exists. To remove it, run: $ remove-config {name}");

            Directory.CreateDirectory(GetFolderPath(name));

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
            Console.WriteLine($"Config {name} created.");
        }

        public void AddAsset(string name, string assetName, string link, string collectionName)
        {
            if (!ConfigExists(name))
                throw new ManagerException($"Config with name {name} doesn't exists. To create it, run: $ add-config {name}");
            var store = new DataStore(GetConfigPath(name));
            var collection = store.GetCollection<Asset>(collectionName);
            var existingAsset = collection.Find(x => x.Name.Equals(assetName)).FirstOrDefault();
            if (existingAsset != null)
                throw new ManagerException($"{collectionName} with name {assetName} already exists. To remove it, run: $ remove-{collectionName.ToLower()} {name} {assetName}");

            var asset = new Asset()
            {
                Link = link,
                Name = assetName
            };

            collection.InsertOne(asset);
            Console.WriteLine($"{collectionName} {assetName} added to config {name}.");
        }

        public void AddMod(string name, string modName, string link, ModTypeEnum modType)
        {
            AddAsset(name, modName, link, "mods");

            var configPath = GetConfigPath(name);
            var store = new DataStore(configPath);

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

        public void AddPlugin(string name, string pluginName, string link)
        {
            AddAsset(name, pluginName, link, "plugins");
        }

        public void AddResourcePack(string name, string resourcePackName, string link, bool isServerDefault = false)
        {
            AddAsset(name, resourcePackName, link, "resourcePacks");

            if (isServerDefault)
            {
                var configPath = GetConfigPath(name);
                var store = new DataStore(configPath);

                // Update the server.resource_pack property
                var config = store.GetItem<ServerConfig>("server");
                config.ResourcePack = resourcePackName;
                store.ReplaceItem("server", config);
                Console.WriteLine($"Resource pack {resourcePackName} set as default for config {name}.");
            }
        }

        public void AddWorld(string name, string worldName, string link, bool isServerDefault = false)
        {
            AddAsset(name, worldName, link, "worlds");

            if (isServerDefault)
            {
                var configPath = GetConfigPath(name);
                var store = new DataStore(configPath);

                // Update the server.default_world property
                var config = store.GetItem<ServerConfig>("server");
                config.DefaultWorld = worldName;
                store.ReplaceItem("server", config);
                Console.WriteLine($"World {worldName} set as default for config {name}.");
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

            RemoveAsset(name, resourcePackName, "resourcePacks");
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

            RemoveAsset(name, worldName, "worlds");
        }
    }
}
