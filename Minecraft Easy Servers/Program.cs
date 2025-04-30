using CommandLine;
using Minecraft_Easy_Servers;
using Minecraft_Easy_Servers.Exceptions;
using Minecraft_Easy_Servers.Helpers;


// read rootPath from json file at the root of the project
var rootPath = string.Empty;
rootPath = GlobalConfigHelper.ReadStringProperty("rootPath");


CLI commandLineRunner = string.IsNullOrEmpty(rootPath) ? CLI.Create() : CLI.Create(rootPath);

await SetupCommandLineRunner(args, commandLineRunner);


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