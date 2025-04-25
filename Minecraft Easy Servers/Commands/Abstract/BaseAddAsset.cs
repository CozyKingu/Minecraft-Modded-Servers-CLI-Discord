using CommandLine;

public abstract class BaseAddAsset : BaseOptions
{
    [Value(0, MetaName = "config name", Required = true, HelpText = "Configuration name")]
    public required string ConfigName { get; set; }

    [Value(1, MetaName = "name", Required = true, HelpText = "Name of mod")]
    public required string Name { get; set; }

    [Value(2, MetaName = "mod link", Required = true, HelpText = "http://host/mod.zip or file:mod.zip if already uploaded")]
    public required string Link { get; set; }
}
