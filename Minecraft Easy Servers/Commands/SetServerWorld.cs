using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("set-server-world", HelpText = "Set server world for instanciated server.")]
    public class SetServerWorld : BaseOptions
    {
        [Value(0, MetaName = "server name", Required = true, HelpText = "Server name")]
        public required string ServerName { get; set; }

        [Value(1, MetaName = "world link", Required = true, HelpText = "http://host/mod.zip or file:mod.zip if already uploaded")]
        public required string Link { get; set; }
    }
}
