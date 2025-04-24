using Minecraft_Easy_Servers.Commands;
using Minecraft_Easy_Servers.Commands.Abstract;
using Minecraft_Easy_Servers.Managers;

namespace Minecraft_Easy_Servers
{
    public class CLI
        : IRunner<AddServer>, IRunner<AddConfig>, IRunner<CheckStatus>, IRunner<UpServer>, IRunner<DownServer>, IRunner<RemoveServer>
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

        public async Task Run(AddConfig options)
        {
            throw new NotImplementedException();
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
    }
}
