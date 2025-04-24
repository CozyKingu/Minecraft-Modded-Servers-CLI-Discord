using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("status", HelpText = "Check server status")]
    public class CheckStatus : BaseOptions
    {
        [Value(0, MetaName = "server name", Required = true, HelpText = "Server name")]
        public required string Name { get; set; }
    }
}
