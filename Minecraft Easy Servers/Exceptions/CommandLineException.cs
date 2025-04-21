using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minecraft_Easy_Servers.Exceptions
{
    internal class CommandLineException : Exception
    {
        public CommandLineException()
        {
        }

        public CommandLineException(string? message) : base(message)
        {
        }
    }
}
