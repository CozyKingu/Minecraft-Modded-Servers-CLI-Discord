using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace Minecraft_Easy_Servers.Helpers
{
    // Class that reads minecraft-easy-servers-config.json
    public static class GlobalConfigHelper
    {
        private const string ConfigFileName = "minecraft-easy-servers-config.json";

        public static string? ReadStringProperty(string propertyName)
        {
            if (!File.Exists(ConfigFileName))
                throw new FileNotFoundException($"Configuration file '{ConfigFileName}' not found.");

            string jsonContent = File.ReadAllText(ConfigFileName);
            using JsonDocument document = JsonDocument.Parse(jsonContent);

            if (document.RootElement.TryGetProperty(propertyName, out JsonElement property))
            {
                return property.GetString();
            }

            return null; // Return null if the property does not exist
        }
    }
}
