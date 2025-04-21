using Minecraft_Easy_Servers.Commands;
using Minecraft_Easy_Servers.Commands.Abstract;

namespace Minecraft_Easy_Servers
{
    public class MinecraftCommandLineRunner
        : IRunner<AddServer>, IRunner<AddConfig>
    {
        public MinecraftCommandLineRunner()
        {
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
