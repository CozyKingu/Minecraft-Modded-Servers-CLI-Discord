using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("add-resource-pack", HelpText = "Add a new resource pack for minecraft server.")]
    public class AddResourcePack : BaseAddAsset
    {
        [Option("server-default", Required = false, HelpText = "Enable default server resource pack")]
        public bool ServerDefault { get; set; } = false;
    }
}
