using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("set-server-resource-pack", HelpText = "Set server property for instanciated server.")]
    public class SetServerResourcePack : BaseOptions
    {
        [Value(0, MetaName = "server name", Required = true, HelpText = "Server name")]
        public required string ServerName { get; set; }

        [Value(1, MetaName = "resource pack link", Required = true, HelpText = "http://host/mod.zip. Only links are supported.")]
        public required string Link { get; set; }
    }
}
