namespace Minecraft_Easy_Servers.Helpers
{
    public static class DebugHelper
    {
        public static void DeleteFolder(string folder)
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
