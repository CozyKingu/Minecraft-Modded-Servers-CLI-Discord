using CommandLine;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("ListAssets", HelpText = "List all of config assets.")]
    public class ListAssets : BaseOptions
    {
        [Value(0, MetaName = "config name", Required = true, HelpText = "Configuration name")]
        public required string ConfigName { get; set; }
    }
}
