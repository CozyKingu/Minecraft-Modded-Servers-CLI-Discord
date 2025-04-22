using Minecraft_Easy_Servers.Commands;
using Minecraft_Easy_Servers.Commands.Abstract;
using Minecraft_Easy_Servers.Managers;

namespace Minecraft_Easy_Servers
{
    public class CLI
        : IRunner<AddServer>, IRunner<AddConfig>, IRunner<CheckStatus>
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

        public void Run(AddServer options)
        {
            Console.WriteLine("ok");
        }

        public void Run(AddConfig options)
        {
            throw new NotImplementedException();
        }

        public void Run(CheckStatus options)
        {
            var serverStatus = serverManager.StatusServer(options.Name);
            var message = serverStatus switch
            {
                ServerStatus.NONE => "Server not running",
                ServerStatus.PROCESS_RUNNING => "Server process is running but the server is not listening",
                ServerStatus.LISTENING => "Server is listening",
                _ => throw new InvalidOperationException("Status server undefined")
            };

            Console.WriteLine($"{message}");
        }
    }
}
