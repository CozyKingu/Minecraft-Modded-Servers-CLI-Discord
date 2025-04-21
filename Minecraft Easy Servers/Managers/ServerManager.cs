using Minecraft_Easy_Servers.Exceptions;
using Minecraft_Easy_Servers.Helpers;

namespace Minecraft_Easy_Servers.Managers
{
    public class ServerManager
    {
        private readonly ExecuteManager executeManager;
        public const string FolderName = "servers";
        private const string AssetsFolder = "Assets";

        public ServerManager(ExecuteManager executeManager)
        {
            this.executeManager = executeManager;
        }

        public async Task CreateServer(
            string name,
            string version)
        {
            string folderPath = GetFolderPath(name);
            if (Directory.Exists(folderPath))
                throw new ManagerException($"Server with name {name} already exists. To remove it, run: $ remove server {name}");

            Directory.CreateDirectory(folderPath);
            const string eulaAsset = "eula.txt";
            CopyAssetToFolder(folderPath, eulaAsset);

            await MinecraftDownloader.DownloadMinecraftServer(version, folderPath);
            Console.WriteLine($"Download of server version {version} finished. Initializing server with first boot-up...");

            FirstRunServer(name);
        }

        private static string GetFolderPath(string name)
        {
            return Path.Combine(FolderName, name);
        }

        private void FirstRunServer(string name)
        {
            var jarFiles = Directory.GetFiles(GetFolderPath(name), "*.jar");
            var serverJar = jarFiles.Where(x => x.Contains("minecraft_server")).FirstOrDefault();
            if (serverJar is null)
                throw new ManagerException($"No jar file in {name} server folder");

            executeManager.ExecuteJarAndStop(serverJar, "Done");
        }

        public void RemoveServer(string name)
        {
            Directory.Delete(GetFolderPath(name), recursive: true);
        }

        private static void CopyAssetToFolder(string folderPath, string assetFile)
        {
            File.Copy(Path.Combine(AssetsFolder, assetFile), Path.Combine(folderPath, assetFile));
        }
    }
}
