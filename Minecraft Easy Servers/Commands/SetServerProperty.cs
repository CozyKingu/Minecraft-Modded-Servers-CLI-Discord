using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("set-server-property", HelpText = "Set server property for instanciated server.")]
    public class SetServerProperty : BaseOptions
    {
        [Value(0, MetaName = "server name", Required = true, HelpText = "Server name")]
        public required string ServerName { get; set; }

        [Value(1, MetaName = "key=value", Required = true, HelpText = "i.e: gamemode=creative")]
        public required string KeyValue { get; set; }
    }
}
