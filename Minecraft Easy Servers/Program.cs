using CommandLine;
using Minecraft_Easy_Servers;
using Minecraft_Easy_Servers.Helpers;

bool debug = true;

var commandLineRunner = new MinecraftCommandLineRunner();
SetupCommandLineRunner(args, commandLineRunner);

if (debug)
{ 
    await MinecraftDownloader.DownloadMinecraftServer("1.20.4", "testFolder");
    // DebugHelper.DeleteFolder("testFolder");
}

static void SetupCommandLineRunner(string[] args, MinecraftCommandLineRunner minecraftCommandLineRunner)
{
    var verbs = CommandLineHelper.GetRunnerTypes(typeof(MinecraftCommandLineRunner)).ToArray();
    Parser.Default.ParseArguments(args, verbs)
                       .WithParsed(o =>
                       {
                           minecraftCommandLineRunner.Run((dynamic)o); // dispatch
                       });
}