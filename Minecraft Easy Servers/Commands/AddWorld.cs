using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("add-world", HelpText = "Add a new resource pack for minecraft server.")]
    public class AddWorld : BaseAddAsset
    {
        [Option("server-default", Required = false, HelpText = "Enable default server resource pack")]
        public bool ServerDefault { get; set; } = false;
    }
}
