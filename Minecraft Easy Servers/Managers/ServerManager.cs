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
            if (ServerExists(name))
                throw new ManagerException($"Server with name {name} already exists. To remove it, run: $ remove server {name}");

            Directory.CreateDirectory(GetFolderPath(name));
            const string eulaAsset = "eula.txt";
            CopyAssetToFolder(GetFolderPath(name), eulaAsset);

            await MinecraftDownloader.DownloadMinecraftServer(version, GetFolderPath(name));
            Console.WriteLine($"Download of server version {version} finished. Initializing server with first boot-up...");

            FirstRunServer(name);
        }

        private static bool ServerExists(string name)
        {
            return Directory.Exists(GetFolderPath(name));
        }

        public void UpServer(string name)
        {
            if (!ServerExists(name))
                throw new ManagerException($"Server with name {name} doesn't exists. To create it, run: $ add server {name}");

            string serverJar = GetServerJar(name);
            var pid = executeManager.RunBackgroundJar(serverJar, "Done", killIfAckFailed: true);
            if (pid is null)
                throw new ManagerException($"Server up command failed. Jar execution failed.");

            Console.WriteLine($"Server jar with PID {pid} is running.");
        }

        public ServerStatus StatusServer(string name)
        {
            if (!ServerExists(name))
                return ServerStatus.NONE;

            string serverJar = GetServerJar(name);
            var jarStatus = executeManager.JarStatus(serverJar, out _);
            if (!jarStatus)
                return ServerStatus.NONE;

            return ServerStatus.PROCESS_RUNNING;
            // TODO: Check is server is listening using MinecraftRCON communications on port.
        }

        private static string GetFolderPath(string name)
        {
            return Path.Combine(FolderName, name);
        }

        private void FirstRunServer(string name)
        {
            string serverJar = GetServerJar(name);
            executeManager.ExecuteJarAndStop(serverJar, "Done");
        }

        private static string GetServerJar(string name)
        {
            var jarFiles = Directory.GetFiles(GetFolderPath(name), "*.jar");
            var serverJar = jarFiles.Where(x => x.Contains("server")).FirstOrDefault() ?? throw new ManagerException($"No jar file in {name} server folder");
            return serverJar;
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
