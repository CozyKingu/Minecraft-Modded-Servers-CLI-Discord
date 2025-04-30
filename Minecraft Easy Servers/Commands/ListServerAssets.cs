using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Easy_Servers.Commands
{
    [Verb("list-server-assets", HelpText = "List all server assets")]
    public class ListServerAssets : BaseOptions
    {
        [Value(0, MetaName = "server name", Required = true, HelpText = "Server name")]
        public required string ServerName { get; set; }
    }
}
