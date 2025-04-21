using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("add config", HelpText = "Add a new config for minecraft server.")]
    public class AddConfig : BaseOptions
    {
        [Option('n', "name", Required = true, HelpText = "Nom de la configuration")]
        public string Name { get; set; }
    }
}
