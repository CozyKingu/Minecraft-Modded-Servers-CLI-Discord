using CommandLine;

public abstract class BaseRemoveServerAsset : BaseOptions
{
    [Value(0, MetaName = "server name", Required = true, HelpText = "server name")]
    public required string ServerName { get; set; }

    [Value(1, MetaName = "name", Required = true, HelpText = "Name")]
    public required string Name { get; set; }
}
