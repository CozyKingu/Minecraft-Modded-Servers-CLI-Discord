using System.Text.Json.Serialization;

namespace Minecraft_Easy_Servers.Managers.Models
{
    public class ConfigJsonDb
    {
        [JsonPropertyName("modLoader")]
        public required string ModLoader { get; set; }

        [JsonPropertyName("version")]
        public required string Version { get; set; }

        [JsonPropertyName("mods")]
        public List<Asset> Mods { get; set; } = new();

        [JsonPropertyName("resourcePacks")]
        public List<Asset> ResourcePacks { get; set; } = new();

        [JsonPropertyName("plugins")]
        public List<Asset> Plugins { get; set; } = new();

        [JsonPropertyName("worlds")]
        public List<Asset> Worlds { get; set; } = new();

        [JsonPropertyName("client")]
        public ClientConfig Client { get; set; } = new();

        [JsonPropertyName("server")]
        public ServerConfig Server { get; set; } = new();

        // TODO: Mod config modifier.
    }

    public class ServerConfig
    {
        [JsonPropertyName("mods")]
        public List<string> Mods { get; set; } = new();

        [JsonPropertyName("plugins")]
        public List<string> Plugins { get; set; } = new();

        [JsonPropertyName("resourcePack")]
        public string ResourcePack { get; set; } = string.Empty;

        [JsonPropertyName("properties")]
        public Dictionary<string, string> Properties { get; set; } = new();

        [JsonPropertyName("defaultWorld")]
        public string DefaultWorld { get; set; } = string.Empty;
    }

    public class ClientConfig
    {
        [JsonPropertyName("mods")]
        public List<string> Mods { get; set; } = new();

        [JsonPropertyName("resourcePacks")]
        public List<string> ResourcePacks { get; set; } = new();

        [JsonPropertyName("worlds")]
        public List<string> Worlds { get; set; } = new();
    }

    public class Asset
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("link")]
        public string Link { get; set; } = string.Empty;
    }
}
