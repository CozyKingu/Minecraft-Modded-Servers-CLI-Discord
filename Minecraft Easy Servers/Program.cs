using CommandLine;
using Minecraft_Easy_Servers;
using Minecraft_Easy_Servers.Helpers;
using Minecraft_Easy_Servers.Managers;

bool debug = false;

var configManager = new ConfigManager();
var executeManager = new ExecuteManager();
var serverManger = new ServerManager(executeManager);
var commandLineRunner = new CLI(serverManger, configManager, executeManager);
SetupCommandLineRunner(args, commandLineRunner);

if (debug)
{
    //serverManger.RemoveServer("server1");
    //await serverManger.CreateServer("server1", "1.20.4");
    var status = serverManger.StatusServer("server1");
    serverManger.UpServer("server1");
}

static void SetupCommandLineRunner(string[] args, CLI minecraftCommandLineRunner)
{
    var verbs = CommandLineHelper.GetRunnerTypes(typeof(CLI)).ToArray();
    Parser.Default.ParseArguments(args, verbs)
                       .WithParsed(o =>
                       {
                           minecraftCommandLineRunner.Run((dynamic)o); // dispatch
                       });
}