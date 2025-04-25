using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("remove-config", HelpText = "Add a new config for minecraft server.")]
    public class RemoveConfig : BaseOptions
    {
        [Value(0, MetaName = "config name", Required = true, HelpText = "Configuration name")]
        public required string Name { get; set; }
    }
}
