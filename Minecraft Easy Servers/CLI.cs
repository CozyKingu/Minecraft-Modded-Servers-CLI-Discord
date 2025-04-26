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
            IRunner<CheckStatus>, IRunner<UpServer>, IRunner<DownServer>
    {
        private readonly ServerManager serverManager;
        private readonly ConfigManager configManager;
        private readonly ExecuteManager executeManager;

        public CLI(ServerManager serverManager, ConfigManager configManager, ExecuteManager executeManager)
        {
            this.serverManager = serverManager;
            this.configManager = configManager;
            this.executeManager = executeManager;
        }

        public async Task Run(AddServer options)
        {
            await serverManager.CreateServer(options.Name, options.Version);
        }

        public Task Run(AddConfig options)
        {
            configManager.CreateConfig(options.Name, options.ModLoader, options.Version);
            return Task.CompletedTask;
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

        public Task Run(AddPlugin options)
        {
            configManager.AddPlugin(options.ConfigName, options.Name, options.Link);
            return Task.CompletedTask;
        }

        public Task Run(AddResourcePack options)
        {
            configManager.AddResourcePack(options.ConfigName, options.Name, options.Link, options.ServerDefault);
            return Task.CompletedTask;
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

        public Task Run(AddWorld options)
        {
            configManager.AddWorld(options.ConfigName, options.Name, options.Link, options.ServerDefault);
            return Task.CompletedTask;
        }
    }
}
