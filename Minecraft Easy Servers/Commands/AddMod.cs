using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("add-mod", HelpText = "Add a new config for minecraft server.")]
    public class AddMod : BaseAddAsset
    {

        [Option("client-side", Required = false, HelpText = "Enable if Client side mode only")]
        public bool ClientSide { get; set; } = false;

        [Option("server-side", Required = false, HelpText = "Enable if Server side mode only")]
        public bool ServerSide { get; set; } = false;
    }
}
