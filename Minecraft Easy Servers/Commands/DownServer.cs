using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("down", HelpText = "Stop server")]
    public class DownServer : BaseOptions
    {
        [Value(0, MetaName = "server name", Required = true, HelpText = "Server name")]
        public required string Name { get; set; }
    }
}
