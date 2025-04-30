using Minecraft_Easy_Servers.Commands;
using Minecraft_Easy_Servers.Commands.Abstract;
using Minecraft_Easy_Servers.Managers;
using Minecraft_Easy_Servers.Managers.Models;

namespace Minecraft_Easy_Servers
{
    public class CLI :
            // Add commands
            IRunner<AddServer>, IRunner<AddConfig>, IRunner<AddMod>, IRunner<AddPlugin>, IRunner<AddResourcePack>, IRunner<AddWorld>,
            // Remove commands
            IRunner<RemoveServer>, IRunner<RemoveConfig>, IRunner<RemoveMod>, IRunner<RemovePlugin>, IRunner<RemoveResourcePack>, IRunner<RemoveWorld>,
            // Server execution commands
            IRunner<CheckStatus>, IRunner<UpServer>, IRunner<DownServer>, IRunner<SendCommand>,
            // Server assets changements when already instanciated
            IRunner<SetServerWorld>, IRunner<SetServerResourcePack>, IRunner<SetServerProperty>,
            IRunner<AddServerMod>, IRunner<AddServerPlugin>, 
            IRunner<RemoveServerMod>, IRunner<RemoveServerPlugin>,
            // List commands
            IRunner<ListServers>, IRunner<ListConfigs>, IRunner<ListAssets>, IRunner<ListServerAssets>
    {
        public ServerManager ServerManager => serverManager;
        public ConfigManager ConfigManager => configManager;

        private readonly ServerManager serverManager;
        private readonly ConfigManager configManager;

        public CLI(ServerManager serverManager, ConfigManager configManager)
        {
            this.serverManager = serverManager;
            this.configManager = configManager;
        }

        public static CLI Create(string? rootPath = null)
        {
            var executeManager = new ExecuteManager();
            var commandManager = new CommandManager();

            ConfigManager.RootPath = rootPath;
            ServerManager.RootPath = rootPath;
            var configManager = new ConfigManager(executeManager);
            var serverManger = new ServerManager(executeManager, commandManager, configManager);
            return new CLI(serverManger, configManager);
        }

        public async Task Run(AddServer options)
        {
            await serverManager.CreateServer(options.Name, options.Config);
        }

        public async Task Run(AddConfig options)
        {
            await configManager.CreateConfig(options.Name, options.ModLoader, options.Version);
        }

        public async Task Run(CheckStatus options)
        {
            var serverStatus = await serverManager.StatusServer(options.Name);
            var message = serverStatus switch
            {
                ServerStatus.NONE => "Server not running",
                ServerStatus.PROCESS_RUNNING => "Server process is running but the server is not listening",
                ServerStatus.LISTENING => "Server is listening",
                _ => throw new InvalidOperationException("Status server undefined")
            };

            Console.WriteLine($"{message}");
        }

        public async Task Run(DownServer options)
        {
            Console.WriteLine("Please wait...");
            await serverManager.DownServer(options.Name);
        }

        public Task Run(UpServer options)
        {
            Console.WriteLine("Please wait...");
            serverManager.UpServer(options.Name, options.Port);
            return Task.CompletedTask;
        }

        public async Task Run(RemoveServer options)
        {
            await serverManager.RemoveServer(options.Name);
        }

        public async Task Run(AddMod options)
        {
            var modType =
                    options.ClientSide ? ModTypeEnum.CLIENT
                    : options.ServerSide ? ModTypeEnum.SERVER
                    : ModTypeEnum.GLOBAL;
            await configManager.AddMod(options.ConfigName, options.Name, options.Link, modType);
        }

        public Task Run(RemoveConfig options)
        {
            configManager.RemoveConfig(options.Name);
            return Task.CompletedTask;
        }

        public Task Run(RemoveMod options)
        {
            configManager.RemoveMod(options.ConfigName, options.Name);
            return Task.CompletedTask;
        }

        public async Task Run(AddPlugin options)
        {
            await configManager.AddPlugin(options.ConfigName, options.Name, options.Link);
        }

        public async Task Run(AddResourcePack options)
        {
            await configManager.AddResourcePack(options.ConfigName, options.Name, options.Link, options.ServerDefault);
        }

        public Task Run(RemovePlugin options)
        {
            configManager.RemovePlugin(options.ConfigName, options.Name);
            return Task.CompletedTask;
        }

        public Task Run(RemoveResourcePack options)
        {
            configManager.RemoveResourcePack(options.ConfigName, options.Name);
            return Task.CompletedTask;
        }

        public Task Run(RemoveWorld options)
        {
            configManager.RemoveWorld(options.ConfigName, options.Name);
            return Task.CompletedTask;
        }

        public async Task Run(AddWorld options)
        {
            await configManager.AddWorld(options.ConfigName, options.Name, options.Link, options.ServerDefault);
        }

        public async Task Run(SetServerWorld options)
        {
            await serverManager.SetServerWorld(options.ServerName, options.Link);
        }

        public Task Run(SetServerResourcePack options)
        {
            serverManager.SetServerResourcePack(options.ServerName, options.Link);
            return Task.CompletedTask;
        }

        public Task Run(SetServerProperty options)
        {
            serverManager.SetServerProperty(options.ServerName, options.KeyValue);
            return Task.CompletedTask;
        }

        public async Task Run(AddServerMod options)
        {
            await serverManager.AddServerMod(options.ServerName, options.Name, options.Link);
        }

        public async Task Run(AddServerPlugin options)
        {
            await serverManager.AddServerPlugin(options.ServerName, options.Name, options.Link);
        }

        public async Task Run(RemoveServerMod options)
        {
            serverManager.RemoveServerMod(options.ServerName, options.Name);
            await Task.CompletedTask;
        }

        public async Task Run(RemoveServerPlugin options)
        {
            serverManager.RemoveServerPlugin(options.ServerName, options.Name);
            await Task.CompletedTask;
        }

        public Task Run(ListServers options)
        {
            var servers = ServerManager.ListAvailableServers();
            foreach (var server in servers)
            {
                Console.WriteLine(server);
            }
            return Task.CompletedTask;
        }

        public Task Run(ListConfigs options)
        {
            var configs = ConfigManager.ListConfigs();
            foreach (var config in configs)
            {
                Console.WriteLine(config);
            }
            return Task.CompletedTask;
        }

        public Task Run(ListAssets options)
        {
            var assetTypes = new Dictionary<string, string>
                   {
                       { "Mods", "mods" },
                       { "Resource Packs", "resourcepacks" },
                       { "Plugins", "plugins" },
                       { "Worlds", "worlds" }
                   };

            foreach (var assetType in assetTypes)
            {
                Console.WriteLine($"{assetType.Key}:");
                var assets = configManager.ListAssets(options.ConfigName, assetType.Value);
                foreach (var asset in assets)
                {
                    Console.WriteLine($"- {asset.Name}");
                }
            }

            return Task.CompletedTask;
        }

        public Task Run(ListServerAssets options)
        {
            var assetTypes = new Dictionary<string, string>
                   {
                       { "Mods", "mods" },
                       { "Recommanded Resource Pack", "resourcepacks" },
                       { "Plugins", "plugins" },
                       { "Current World", "worlds" }
                   };

            foreach (var assetType in assetTypes)
            {
                Console.WriteLine($"{assetType.Key}:");
                var assets = serverManager.ListServerAssets(options.ServerName, assetType.Value);
                foreach (var asset in assets)
                {
                    Console.WriteLine($"- {(string.IsNullOrEmpty(asset.Name) ? "No selected" : asset.Name) }");
                }
            }

            return Task.CompletedTask;
        }

        public async Task Run(SendCommand options)
        {
            await serverManager.SendCommand(options.ServerName, options.Command);
        }
    }
}
