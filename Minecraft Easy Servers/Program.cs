using CommandLine;
using Minecraft_Easy_Servers;
using System.Runtime.CompilerServices;

var commandLineRunner = new MinecraftCommandLineRunner();
SetupCommandLineRunner(args, commandLineRunner);

static void SetupCommandLineRunner(string[] args, ICommandLineRunner commandLineRunner)
{
    Parser.Default.ParseArguments<Options>(args)
                       .WithParsed(commandLineRunner.Run);
}
