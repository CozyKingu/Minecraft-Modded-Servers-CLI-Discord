using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("status", HelpText = "Check server status")]
    public class CheckStatus : BaseOptions
    {
        [Option('n', "name", HelpText = "Server name")]
        public required string Name { get; set; }
    }
}
