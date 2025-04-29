using Minecraft_Easy_Servers;
using Minecraft_Easy_Servers.Commands;
using NetCord.Services.ApplicationCommands;

namespace DiscordBot
{
    public class DiscordCommands : ApplicationCommandModule<ApplicationCommandContext>
    {
        private static readonly CLI cli = CLI.Create("C:\\Users\\nbout\\source\\repos\\Minecraft Easy Servers\\Minecraft Easy Servers\\bin\\Debug\\net8.0");

        [SlashCommand("create-server", "Creates a new Minecraft server.")]
        public static async Task<string> CreateServer(string serverName, string configName)
        {
            await cli.Run(new AddServer
            {
                Name = serverName,
                Config = configName
            });

            return $"Server '{serverName}' created with config '{configName}'.";
        }

        [SlashCommand("create-config", "Creates a new server configuration.")]
        public static async Task<string> CreateConfig(string configName, string modLoader, string version)
        {
            await cli.Run(new AddConfig
            {
                Name = configName,
                ModLoader = modLoader,
                Version = version
            });

            return $"Config '{configName}' created with mod loader '{modLoader}' and version '{version}'.";
        }

        [SlashCommand("status-server", "Checks the status of a server.")]
        public static async Task<string> StatusServer(string serverName)
        {
            await cli.Run(new CheckStatus
            {
                Name = serverName
            });

            return $"Status of server '{serverName}' checked.";
        }

        [SlashCommand("down-server", "Stops a running server.")]
        public static async Task<string> DownServer(string serverName)
        {
            await cli.Run(new DownServer
            {
                Name = serverName
            });

            return $"Server '{serverName}' has been stopped.";
        }

        [SlashCommand("up-server", "Starts a server.")]
        public static async Task<string> UpServer(string serverName, int port)
        {
            await cli.Run(new UpServer
            {
                Name = serverName,
                Port = port
            });

            return $"Server '{serverName}' started on port {port}.";
        }

        [SlashCommand("remove-server", "Removes a server.")]
        public static async Task<string> RemoveServer(string serverName)
        {
            await cli.Run(new RemoveServer
            {
                Name = serverName
            });

            return $"Server '{serverName}' has been removed.";
        }

        [SlashCommand("add-mod", "Adds a mod to a configuration.")]
        public static async Task<string> AddMod(string configName, string modName, string link, bool clientSide = false, bool serverSide = false)
        {
            await cli.Run(new AddMod
            {
                ConfigName = configName,
                Name = modName,
                Link = link,
                ClientSide = clientSide,
                ServerSide = serverSide
            });

            return $"Mod '{modName}' added to config '{configName}'.";
        }

        [SlashCommand("remove-config", "Removes a server configuration.")]
        public static async Task<string> RemoveConfig(string configName)
        {
            await cli.Run(new RemoveConfig
            {
                Name = configName
            });

            return $"Config '{configName}' has been removed.";
        }

        [SlashCommand("remove-mod", "Removes a mod from a configuration.")]
        public static async Task<string> RemoveMod(string configName, string modName)
        {
            await cli.Run(new RemoveMod
            {
                ConfigName = configName,
                Name = modName
            });

            return $"Mod '{modName}' removed from config '{configName}'.";
        }

        [SlashCommand("add-plugin", "Adds a plugin to a configuration.")]
        public static async Task<string> AddPlugin(string configName, string pluginName, string link)
        {
            await cli.Run(new AddPlugin
            {
                ConfigName = configName,
                Name = pluginName,
                Link = link
            });

            return $"Plugin '{pluginName}' added to config '{configName}'.";
        }

        [SlashCommand("add-resource-pack", "Adds a resource pack to a configuration.")]
        public static async Task<string> AddResourcePack(string configName, string resourcePackName, string link, bool serverDefault = false)
        {
            await cli.Run(new AddResourcePack
            {
                ConfigName = configName,
                Name = resourcePackName,
                Link = link,
                ServerDefault = serverDefault
            });

            return $"Resource pack '{resourcePackName}' added to config '{configName}'.";
        }

        [SlashCommand("remove-plugin", "Removes a plugin from a configuration.")]
        public static async Task<string> RemovePlugin(string configName, string pluginName)
        {
            await cli.Run(new RemovePlugin
            {
                ConfigName = configName,
                Name = pluginName
            });

            return $"Plugin '{pluginName}' removed from config '{configName}'.";
        }

        [SlashCommand("remove-resource-pack", "Removes a resource pack from a configuration.")]
        public static async Task<string> RemoveResourcePack(string configName, string resourcePackName)
        {
            await cli.Run(new RemoveResourcePack
            {
                ConfigName = configName,
                Name = resourcePackName
            });

            return $"Resource pack '{resourcePackName}' removed from config '{configName}'.";
        }

        [SlashCommand("remove-world", "Removes a world from a configuration.")]
        public static async Task<string> RemoveWorld(string configName, string worldName)
        {
            await cli.Run(new RemoveWorld
            {
                ConfigName = configName,
                Name = worldName
            });

            return $"World '{worldName}' removed from config '{configName}'.";
        }

        [SlashCommand("add-world", "Adds a world to a configuration.")]
        public static async Task<string> AddWorld(string configName, string worldName, string link, bool serverDefault = false)
        {
            await cli.Run(new AddWorld
            {
                ConfigName = configName,
                Name = worldName,
                Link = link,
                ServerDefault = serverDefault
            });

            return $"World '{worldName}' added to config '{configName}'.";
        }

        [SlashCommand("set-server-world", "Sets the world for a server.")]
        public static async Task<string> SetServerWorld(string serverName, string link)
        {
            await cli.Run(new SetServerWorld
            {
                ServerName = serverName,
                Link = link
            });

            return $"World set for server '{serverName}'.";
        }

        [SlashCommand("set-server-resource-pack", "Sets the resource pack for a server.")]
        public static async Task<string> SetServerResourcePack(string serverName, string link)
        {
            await cli.Run(new SetServerResourcePack
            {
                ServerName = serverName,
                Link = link
            });

            return $"Resource pack set for server '{serverName}'.";
        }

        [SlashCommand("set-server-property", "Sets a property for a server.")]
        public static async Task<string> SetServerProperty(string serverName, string keyValue)
        {
            await cli.Run(new SetServerProperty
            {
                ServerName = serverName,
                KeyValue = keyValue
            });

            return $"Property '{keyValue}' set for server '{serverName}'.";
        }

        [SlashCommand("add-server-mod", "Adds a mod to a server.")]
        public static async Task<string> AddServerMod(string serverName, string modName, string link)
        {
            await cli.Run(new AddServerMod
            {
                ServerName = serverName,
                Name = modName,
                Link = link
            });

            return $"Mod '{modName}' added to server '{serverName}'.";
        }

        [SlashCommand("add-server-plugin", "Adds a plugin to a server.")]
        public static async Task<string> AddServerPlugin(string serverName, string pluginName, string link)
        {
            await cli.Run(new AddServerPlugin
            {
                ServerName = serverName,
                Name = pluginName,
                Link = link
            });

            return $"Plugin '{pluginName}' added to server '{serverName}'.";
        }

        [SlashCommand("remove-server-mod", "Removes a mod from a server.")]
        public static async Task<string> RemoveServerMod(string serverName, string modName)
        {
            await cli.Run(new RemoveServerMod
            {
                ServerName = serverName,
                Name = modName
            });

            return $"Mod '{modName}' removed from server '{serverName}'.";
        }

        [SlashCommand("remove-server-plugin", "Removes a plugin from a server.")]
        public static async Task<string> RemoveServerPlugin(string serverName, string pluginName)
        {
            await cli.Run(new RemoveServerPlugin
            {
                ServerName = serverName,
                Name = pluginName
            });

            return $"Plugin '{pluginName}' removed from server '{serverName}'.";
        }
    }
}
