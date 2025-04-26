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


        public static async Task<string> DownloadFile(string targetDirectoryPath, string link)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(link, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                // Extract the file name from the Content-Disposition header if available
                var contentDisposition = response.Content.Headers.ContentDisposition;
                var fileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName;

                if (string.IsNullOrEmpty(fileName) && !UrlLooksLikeFile(link, out fileName))
                    throw new Exception("Filname cannot be determined.");

                // Combine the directory path with the file name
                var targetFilePath = Path.Combine(targetDirectoryPath, fileName !);

                using var fileStream = new FileStream(targetFilePath, FileMode.Create, FileAccess.Write, FileShare.None);
                await response.Content.CopyToAsync(fileStream);

                return targetFilePath;
            }
        }
        public static bool UrlLooksLikeFile(string url, out string? fileName)
        {
            fileName = null;
            if (string.IsNullOrWhiteSpace(url))
                return false;

            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var lastSegment = System.IO.Path.GetFileName(path);
                var isFile = lastSegment.Contains('.') && !lastSegment.EndsWith(".");
                fileName = lastSegment;
                return isFile;
            }
            catch
            {
                return false;
            }
        }
    }
}
