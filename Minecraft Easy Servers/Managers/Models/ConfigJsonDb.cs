using System.Text.Json.Serialization;

namespace Minecraft_Easy_Servers.Managers.Models
{
    public class ConfigJsonDb
    {
        public required string ModLoader { get; set; }

        public required string Version { get; set; }

        public List<Asset> Mods { get; set; } = new ();

        public List<Asset> ResourcePacks { get; set; } = new ();

        public List<Asset> Plugins { get; set; } = new ();

        public List<Asset> Worlds { get; set; } = new ();

        public ClientConfig Client { get; set; } = new ();

        public ServerConfig Server { get; set; } = new ();

        // TODO: Mod config modifier.
    }

    public class ServerConfig
    {
        public List<string> Mods { get; set; } = new ();

        public List<string> Plugins { get; set; } = new();

        public string ResourcePack { get; set; } = string.Empty;

        public Dictionary<string, string> Properties { get; set; } = new();

        public string DefaultWorld { get; set; } = string.Empty;
    }

    public class ClientConfig
    {
        public List<string> Mods { get; set; } = new();

        public List<string> ResourcePacks { get; set; } = new();

        public List<string> Worlds { get; set;} = new();
    }

    public class Asset
    {
        public string Name { get; set; } = string.Empty;

        public string Link { get; set; } = string.Empty;
    }
}
