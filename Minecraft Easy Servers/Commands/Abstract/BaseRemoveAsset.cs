using CommandLine;

public abstract class BaseRemoveAsset : BaseOptions
{
    [Value(0, MetaName = "config name", Required = true, HelpText = "Configuration name")]
    public required string ConfigName { get; set; }

    [Value(1, MetaName = "name", Required = true, HelpText = "Name")]
    public required string Name { get; set; }
}
