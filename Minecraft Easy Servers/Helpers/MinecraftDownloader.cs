using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Easy_Servers.Helpers
{
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;

    class MinecraftDownloader
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task DownloadMinecraftServer(string version, string folderPath)
        {
            string manifestUrl = "https://launchermeta.mojang.com/mc/game/version_manifest.json";
            string manifestJson = await client.GetStringAsync(manifestUrl);
            using JsonDocument manifestDoc = JsonDocument.Parse(manifestJson);

            var versionEntry = manifestDoc.RootElement
                .GetProperty("versions")
                .EnumerateArray()
                .FirstOrDefault(v => v.GetProperty("id").GetString() == version);

            if (versionEntry.ValueKind == JsonValueKind.Undefined)
                throw new Exception($"Version {version} introuvable.");

            string versionUrl = versionEntry.GetProperty("url").GetString() !;

            string versionJson = await client.GetStringAsync(versionUrl);
            using JsonDocument versionDoc = JsonDocument.Parse(versionJson);

            string serverJarUrl = versionDoc.RootElement
                .GetProperty("downloads")
                .GetProperty("server")
                .GetProperty("url")
                .GetString() !;

            string outputPath = Path.Combine(folderPath, $"minecraft_server_{version}.jar");

            using var serverStream = await client.GetStreamAsync(serverJarUrl);
            using var fileStream = File.Create(outputPath);
            await serverStream.CopyToAsync(fileStream);
        }
    }

}
