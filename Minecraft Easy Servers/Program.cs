using CommandLine;
using Minecraft_Easy_Servers;
using Minecraft_Easy_Servers.Exceptions;
using Minecraft_Easy_Servers.Helpers;
using Minecraft_Easy_Servers.Managers;


bool debug = false;

var configManager = new ConfigManager();
var executeManager = new ExecuteManager();
var commandManager = new CommandManager();
var serverManger = new ServerManager(executeManager, commandManager);
var commandLineRunner = new CLI(serverManger, configManager, executeManager);
await SetupCommandLineRunner(args, commandLineRunner);

if (debug)
{
    //serverManger.RemoveServer("server1");
    //await serverManger.CreateServer("server1", "1.20.4");
    // var port = serverManger.GetPort("server1");
    // serverManger.UpServer("server1");
    var status = await serverManger.StatusServer("server1");
    Console.WriteLine(Enum.GetName(typeof(ServerStatus), status));
}

static async Task SetupCommandLineRunner(string[] args, CLI minecraftCommandLineRunner)
{
    var verbs = CommandLineHelper.GetRunnerTypes(typeof(CLI)).ToArray();
    await Parser.Default.ParseArguments(args, verbs)
                       .WithParsedAsync(async o =>
                       {
                           try
                           {
                                Task task = minecraftCommandLineRunner.Run((dynamic)o); // dispatch
                               await task;
                           }
                           catch (ManagerException e)
                           {
                                Console.WriteLine(e.Message);
                           }
                       });
}