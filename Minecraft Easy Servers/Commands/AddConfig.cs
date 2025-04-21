using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("add config", HelpText = "Add a new config for minecraft server.")]
    public class AddConfig : BaseOptions
    {
        [Option('n', "name", HelpText = "Configuration name")]
        public required string Name { get; set; }
    }
}
