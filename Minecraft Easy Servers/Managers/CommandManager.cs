using CoreRCON.Parsers.Standard;
using CoreRCON;
using System.Net;
namespace Minecraft_Easy_Servers.Managers
{
    public class CommandManager
    {
        private const string LOCALHOST = "127.0.0.1";

        public CommandManager()
        {
        }

        public async Task<string?> GetStatus(int port, string rconPassword)
        {
            var host = LOCALHOST;

            try
            {
                var rcon = new RCON(new IPEndPoint(IPAddress.Parse(host), port), rconPassword);
                await rcon.ConnectAsync();
                // Send a simple command and retrive response as string
                string response = await rcon.SendCommandAsync("/list");
                return IsSuccess(response) ? response : null;
            }
            catch (AuthenticationException)
            {
                Console.WriteLine("RCON password is wrong.");
            }
            catch (Exception ex) {
                Console.Write(ex.ToString());
            }

            return null;
        }

        public async Task<string?> StopServer(int port, string rconPassword)
        {
            var host = LOCALHOST;

            var rcon = new RCON(new IPEndPoint(IPAddress.Parse(host), port), rconPassword);
            await rcon.ConnectAsync();

            // Send a simple command and retrive response as string
            string response = await rcon.SendCommandAsync("stop");

            // Send "status" and try to parse the response
            return IsSuccess(response) ? response : null;
        }

        private static bool IsSuccess(string response)
        {
            return !response.Contains("Unknown or incomplete command");
        }
    }
}
