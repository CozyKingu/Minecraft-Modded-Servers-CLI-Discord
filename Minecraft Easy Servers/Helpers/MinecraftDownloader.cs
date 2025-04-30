using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Easy_Servers.Helpers
{
    using Microsoft.Extensions.Primitives;
    using Minecraft_Easy_Servers.Exceptions;
    using System;
    using System.IO;
    using System.Net.Http;
    using System.Numerics;
    using System.Text.Json;
    using System.Threading.Tasks;

    class MinecraftDownloader
    {
        private static readonly HttpClient client = new HttpClient();

        public static async Task<string> DownloadVanillaMinecraftServer(string version, string folderPath)
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
            return outputPath;
        }

        public static async Task<string> DownloadNeoforgeMinecraftUniversalInstaller(string minecrafVersion, string folderPath)
        {
            string manifestUrl = GlobalConfigHelper.ReadStringProperty("neoforgeVersions") ?? throw new Exception("Cannot find property neoforgeVersions.");
            string manifestJson = await client.GetStringAsync(manifestUrl);
            using JsonDocument manifestDoc = JsonDocument.Parse(manifestJson);
            var versionValue = manifestDoc.RootElement
                .GetProperty("versions")
                .EnumerateArray()
                .Select(x => x.ToString())

               .OrderBy(x => x, Comparer<string>.Create((a, b) =>
               {
                   bool aIsBeta = a.Contains("beta", StringComparison.OrdinalIgnoreCase);
                   bool bIsBeta = b.Contains("beta", StringComparison.OrdinalIgnoreCase);

                   if (aIsBeta && !bIsBeta) return -1;
                   if (!aIsBeta && bIsBeta) return 1;

                   return string.Compare(a, b, StringComparison.OrdinalIgnoreCase);
               }))
                .LastOrDefault(v => GetMinecraftVersionForNeoforge(v) == minecrafVersion);

            if (string.IsNullOrEmpty(versionValue))
                throw new ManagerException($"Version {minecrafVersion} cannot be found for neoforge. Use forge for compatibility with older versions of Minecraft.");

            var urlDownload = GlobalConfigHelper.ReadStringProperty("neoforgeDownloadJar")?.Replace("{neoforgeVersion}", versionValue) ?? throw new Exception($"Cannot find property neoforgeDownloadJar");

            var outputPath = Path.Combine(folderPath, urlDownload.Split('/').Last());
            using var serverStream = await client.GetStreamAsync(urlDownload);
            using var fileStream = File.Create(outputPath);
            await serverStream.CopyToAsync(fileStream);
            return outputPath;
        }

        public static async Task<string> DownloadMultiMCArchive(string os, string folderPath)
        {
            // Determine the property key based on the OS
            string propertyKey = os.ToLower() switch
            {
                "windows" => "multiMCwin",
                "linux" => "multiMClinux",
                "mac" => "multiMCmac",
                _ => throw new Exception($"Unsupported OS: {os}")
            };

            // Retrieve the download URL from the configuration
            string downloadUrl = GlobalConfigHelper.ReadStringProperty(propertyKey)
                ?? throw new Exception($"Cannot find property {propertyKey}.");

            // Extract the file name from the URL
            string fileName = Path.GetFileName(downloadUrl);
            if (string.IsNullOrEmpty(fileName))
                throw new Exception("Failed to determine the file name from the URL.");

            // Combine the folder path with the file name
            string outputPath = Path.Combine(folderPath, fileName);

            // Download the file
            using var serverStream = await client.GetStreamAsync(downloadUrl);
            using var fileStream = File.Create(outputPath);
            await serverStream.CopyToAsync(fileStream);

            return outputPath;
        }

        public static async Task<string> DownloadForgeMinecraftUniversalInstaller(string minecraftVersion, string folderPath)
        {
            string manifestUrl = GlobalConfigHelper.ReadStringProperty("forgeVersions") ?? throw new Exception("Cannot find property forgeVersions.") ;
            string manifestJson = await client.GetStringAsync(manifestUrl);
            using JsonDocument manifestDoc = JsonDocument.Parse(manifestJson);
            var versionValue = manifestDoc.RootElement
                .GetProperty("promos")
                .EnumerateObject()
                .Where(v => v.Name.StartsWith(minecraftVersion))
                .OrderByDescending(v => v.Name.Contains("recommended") ? 1 : 0) // Prioritize "recommended"
                .ThenByDescending(v => v.Name.Contains("latest") ? 1 : 0)      // Then "latest"
                .Select(v => v.Value.GetString())
                .FirstOrDefault();

            if (string.IsNullOrEmpty(versionValue))
                throw new ManagerException($"Version {minecraftVersion} cannot be found for forge. Use neoforge for compatibility with newer versions of Minecraft.");
            var urlDownload = GlobalConfigHelper.ReadStringProperty("forgeDownloadJar")?.Replace("{forgeVersion}", versionValue)?.Replace("{mineVersion}", minecraftVersion) ?? throw new Exception($"Cannot find property forgeDownloadJar");
            var outputPath = Path.Combine(folderPath, urlDownload.Split('/').Last());
            using var serverStream = await client.GetStreamAsync(urlDownload);
            using var fileStream = File.Create(outputPath);
            await serverStream.CopyToAsync(fileStream);
            return outputPath;
        }

        private static string GetMinecraftVersionForNeoforge(string? neoforgeVersion)
        {
            // 1.{mineMinor}.{minePatch}
                var neoforgeVersionMatchingRule = GlobalConfigHelper.ReadStringProperty("neoforgeVersionMatchingRule") ?? throw new Exception("Cannot find property neoforgeVersionMatchingRule");
            var splittedVersion = neoforgeVersion?.Split('.');

            //TODO: Add more flexibility for version matching.
            return neoforgeVersionMatchingRule
                .Replace("{mineMinor}", splittedVersion?[0] ?? string.Empty)
                .Replace("{minePatch}", splittedVersion?[1] ?? string.Empty);
        }

        public static async Task<string> DownloadFile(string targetDirectoryPath, string link, string? prefixName = null)
        {
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(link, HttpCompletionOption.ResponseHeadersRead);
                response.EnsureSuccessStatusCode();

                if (response.RequestMessage?.RequestUri != null)
                {
                    // Remove query parameters from the URL
                    var uriWithoutQuery = new UriBuilder(response.RequestMessage.RequestUri)
                    {
                        Query = string.Empty
                    }.Uri;

                    // Retry the request with the cleaned URL
                    link = uriWithoutQuery.AbsoluteUri;
                    response = await httpClient.GetAsync(uriWithoutQuery, HttpCompletionOption.ResponseHeadersRead);
                    response.EnsureSuccessStatusCode();
                }

                // Extract the file name from the Content-Disposition header if available
                var contentDisposition = response.Content.Headers.ContentDisposition;
                var fileName = contentDisposition?.FileNameStar ?? contentDisposition?.FileName;

                if (string.IsNullOrEmpty(fileName) && !UrlLooksLikeFile(link, out fileName))
                    throw new Exception("Filename cannot be determined.");

                // Combine the directory path with the file name
                fileName = prefixName != null ? prefixName + "_" + fileName : fileName;
                var targetFilePath = Path.Combine(targetDirectoryPath, fileName!);

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

        public static async Task<string> DownloadModLoaderServer(string modLoader, string version, string folderPath)
        {
            switch (modLoader.ToLower())
            {
                case "vanilla":
                    return await DownloadVanillaMinecraftServer(version, folderPath);
                    break;
                case "forge":
                    return await DownloadForgeMinecraftUniversalInstaller(version, folderPath);
                    break;
                case "neoforge":
                    return await DownloadNeoforgeMinecraftUniversalInstaller(version, folderPath);
                    break;
                default:
                    throw new Exception($"ModLoader {modLoader} non supporté.");
            }
        }
    }
}
