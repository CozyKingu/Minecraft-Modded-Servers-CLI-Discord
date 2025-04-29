using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("add-server", HelpText = "Add new server given config")]
    public class AddServer : BaseOptions
    {
        [Value(0, MetaName = "server name", Required = true, HelpText = "Server name")]
        public required string Name { get; set; }

        [Option('c', "config", Required = true, HelpText = "Config name to initialize the server with")]
        public required string Config { get; set; }
    }
}
