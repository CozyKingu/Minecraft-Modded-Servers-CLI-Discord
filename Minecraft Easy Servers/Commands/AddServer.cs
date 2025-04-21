using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("add server", HelpText = "Add new server given config")]
    public class AddServer : BaseOptions
    {
        public string Name { get; set; }
        public string Version { get; set; }
    }
}
