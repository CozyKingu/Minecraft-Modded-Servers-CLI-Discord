using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("send-command", HelpText = "Send command to server")]
    public class SendCommand : BaseOptions
    {
        [Value(0, MetaName = "server name", Required = true, HelpText = "Server name")]
        public required string ServerName { get; set; }

        [Value(1, MetaName = "command", Required = true, HelpText = "Command to send")]
        public required string Command { get; set; }
    }
}
