using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot.Helpers
{
    public static class RestartHelper
    {
        public static void RestartBot(ulong channelId)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = Assembly.GetExecutingAssembly().Location.Replace(".dll", OperatingSystem.IsWindows() ? ".exe" : string.Empty),
                UseShellExecute = true,
                Arguments = $"--restartChannelId {channelId}",
            });

            Environment.Exit(0);
        }
    }
}
