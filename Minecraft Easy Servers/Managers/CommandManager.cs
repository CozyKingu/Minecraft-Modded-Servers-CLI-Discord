using CoreRCON.Parsers.Standard;
using CoreRCON;
using System.Net;
namespace Minecraft_Easy_Servers.Managers
{
    public class CommandManager
    {
        public CommandManager()
        {
        }

        public async Task<string?> GetStatus(int port)
        {
            var host = "127.0.0.1";
            var password = "password";

            var rcon = new RCON(new IPEndPoint(IPAddress.Parse(host), port), password);
            await rcon.ConnectAsync();

            // Send a simple command and retrive response as string
            string response = await rcon.SendCommandAsync("/list");

            // Send "status" and try to parse the response
            return IsSuccess(response) ? response : null;
        }

        private static bool IsSuccess(string response)
        {
            return !response.Contains("Unknown or incomplete command");
        }
    }
}
