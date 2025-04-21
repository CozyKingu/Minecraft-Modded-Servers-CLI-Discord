using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("add server", HelpText = "Add new server given config")]
    public class AddServer : BaseOptions
    {
        [Option('n', "name", HelpText = "Server name")]
        public required string Name { get; set; }

        [Option('v', "version", HelpText = "Minecraft version")]
        public required string Version { get; set; }

        [Option('c', "config", HelpText = "Config name to initialize the server with")]
        public required string Config { get; set; }
    }
}
