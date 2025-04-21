using Minecraft_Easy_Servers.Commands;
using Minecraft_Easy_Servers.Commands.Abstract;
using Minecraft_Easy_Servers.Managers;

namespace Minecraft_Easy_Servers
{
    public class CLI
        : IRunner<AddServer>, IRunner<AddConfig>
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
    }
}
