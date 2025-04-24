using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("up", HelpText = "Run server")]
    public class UpServer : BaseOptions
    {
        [Value(0, MetaName = "server name", Required = true, HelpText = "Server name")]
        public required string Name { get; set; }
        
        [Value(0, MetaName = "server port", Required = true, HelpText = "Server port")]
        public required int Port { get; set; }
    }
}
