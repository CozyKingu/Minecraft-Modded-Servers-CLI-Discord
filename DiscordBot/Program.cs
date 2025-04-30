using DiscordBot;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using System.Text.Json;

var botConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText("bot.json"));
if (botConfig == null || !botConfig.TryGetValue("token", out var token) || string.IsNullOrEmpty(token.GetString()))
{
    Console.WriteLine("Token not found in bot.json");
    return;
}

GatewayClient client = new(new BotToken(token.GetString()!), new GatewayClientConfiguration()
{
    Intents = default
});

// Create the application command service
ApplicationCommandService<ApplicationCommandContext> applicationCommandService = new();

// Add commands using minimal APIs
// Please note that names of slash commands must be lowercase.
applicationCommandService.AddSlashCommand("ping", "Ping!", () => "Pong!");

applicationCommandService.AddModule<DiscordCommands>();

// Add the handler to handle interactions
client.InteractionCreate += async interaction =>
{
    // Check if the interaction is an application command interaction
    if (interaction is not ApplicationCommandInteraction applicationCommandInteraction)
        return;

    // Execute the command
    var result = await applicationCommandService.ExecuteAsync(new ApplicationCommandContext(applicationCommandInteraction, client));

    // Check if the execution failed
    if (result is not IFailResult failResult)
        return;

    // Return the error message to the user if the execution failed
    try
    {
        await interaction.SendResponseAsync(InteractionCallback.Message(failResult.Message));
    }
    catch
    {
    }
};

// When ready send Hello message
client.Ready += async (e) =>
{
    // get channelId from args  --restartChannelId 
    var args = Environment.GetCommandLineArgs();
    string? channelId = null;
    for (int i = 0; i < args.Length; i++)
    {
        if (args[i] == "--restartChannelId" && i + 1 < args.Length)
        {
            channelId = args[i + 1];
            break;
        }
    }
    if (channelId != null && ulong.TryParse(channelId, out var id))
    {
        await client.Rest.SendMessageAsync(id, "Hello! I am back online!");
    }
};


// Create the commands so that you can use them in the Discord client
try
{
    while (true)
    {
        try
        {
            await applicationCommandService.CreateCommandsAsync(
                client.Rest,
                client.Id,
                cancellationToken: new CancellationTokenSource(TimeSpan.FromSeconds(4)).Token);
            break; // Exit the loop if successful
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Command creation timed out. Retrying in 2 seconds...");
            Thread.Sleep(2000); // Wait before retrying
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"An unexpected error occurred: {ex.Message}");
}

client.Log += message =>
{
    Console.WriteLine(message);
    return default;
};


await client.StartAsync();
await Task.Delay(-1);