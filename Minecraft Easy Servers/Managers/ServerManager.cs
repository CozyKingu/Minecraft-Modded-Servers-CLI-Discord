using Minecraft_Easy_Servers.Exceptions;
using Minecraft_Easy_Servers.Helpers;
using Salaros.Configuration;

namespace Minecraft_Easy_Servers.Managers
{
    public class ServerManager
    {
        private readonly ExecuteManager executeManager;
        private readonly CommandManager commandManager;
        public const string FolderName = "servers";
        private const string AssetsFolder = "Assets";

        public ServerManager(ExecuteManager executeManager, CommandManager commandManager)
        {
            this.executeManager = executeManager;
            this.commandManager = commandManager;
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

            if (StatusServer(name).Result != ServerStatus.NONE)
                throw new ManagerException($"Server with name {name} is already running. To stop it, run: $ down {name}");

            string serverJar = GetServerJar(name);
            var pid = executeManager.RunBackgroundJar(serverJar, "Done", killIfAckFailed: true);
            if (pid is null)
                throw new ManagerException($"Server up command failed. Jar execution failed.");

            Console.WriteLine($"Server jar with PID {pid} is running.");
        }

        public void DownServer(string name)
        {
            if (!ServerExists(name))
                throw new ManagerException($"Server with name {name} doesn't exists. To create it, run: $ add server {name}");

            if (StatusServer(name).Result == ServerStatus.NONE)
                throw new ManagerException($"Server with name {name} is not running. To run it, run: $ up {name}");

            var response = commandManager.StopServer(GetRconPort(name), "password");
            if (response is null)
                executeManager.KillJarProcess(GetServerJar(name));

            Thread.Sleep(2000);

            var newStatus = StatusServer(name).Result;
            if (newStatus != ServerStatus.NONE)
                Console.WriteLine($"Server failed to shutdown. Force shutdown from task manager (java.exe).");
            else
                Console.WriteLine($"Server stopped.");
        }

        public async Task<ServerStatus> StatusServer(string name)
        {
            if (!ServerExists(name))
                return ServerStatus.NONE;

            string serverJar = GetServerJar(name);
            var jarStatus = executeManager.JarStatus(serverJar, out _);
            if (!jarStatus)
                return ServerStatus.NONE;

            var serverStatus = await commandManager.GetStatus(GetRconPort(name), "passwurd");
            return serverStatus != null ? ServerStatus.LISTENING : ServerStatus.PROCESS_RUNNING;
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

        public int GetPort(string name)
        {
            return int.Parse(GetServerPropertiesValue(name, "server-port"));
        }
        
        public int GetRconPort(string name)
        {
            return int.Parse(GetServerPropertiesValue(name, "rcon.port"));
        }

        public string GetServerPropertiesValue(string name, string propertyKey)
        {
            if (!ServerExists(name))
                throw new ManagerException($"Server with name {name} doesn't exists. To create it, run: $ add server {name}");
            string serverPropertiesFilePath = GetServerPropertiesPath(name);
            ConfigParser configParser = new ConfigParser(serverPropertiesFilePath, new ConfigParserSettings()
            {
                MultiLineValues = MultiLineValues.AllowEmptyTopSection
            });
            return configParser.NullSection.GetValue(propertyKey);
        }

        private static string GetServerPropertiesPath(string name)
        {
            string serverPropertiesFilePath = Path.Combine(GetFolderPath(name), "server.properties");
            if (!File.Exists(serverPropertiesFilePath))
                throw new ManagerException($"No server.properties in {name} server folder");
            return serverPropertiesFilePath;
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
