using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("add-config", HelpText = "Add a new config for minecraft server.")]
    public class AddConfig : BaseOptions
    {
        [Value(0, MetaName = "config name", Required = true, HelpText = "Configuration name")]
        public required string Name { get; set; }

        [Value(1, MetaName = "mod loader", Required = true, HelpText = "[vanilla,forge,neoforge]")]
        public required string ModLoader { get; set; }

        [Value(2, MetaName = "version", Required = true, HelpText = "Version of the mod loader")]
        public required string Version { get; set; }
    }
}
