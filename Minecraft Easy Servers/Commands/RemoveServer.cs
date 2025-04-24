using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("remove-server", HelpText = "Add new server given config")]
    public class RemoveServer : BaseOptions
    {
        [Value(0, MetaName = "server name", Required = true, HelpText = "Server name")]
        public required string Name { get; set; }
    }
}
