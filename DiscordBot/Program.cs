using DiscordBot;
using Minecraft_Easy_Servers;
using NetCord;
using NetCord.Gateway;
using NetCord.Rest;
using NetCord.Services;
using NetCord.Services.ApplicationCommands;
using System.Reflection;
using System.Text.Json;

var botConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText("bot.json"));
if (botConfig == null || (!botConfig.TryGetValue("token", out var token) && string.IsNullOrEmpty(token.GetString())))
{
    throw new InvalidOperationException("Token not found in bot.json");
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

// Create the commands so that you can use them in the Discord client
await applicationCommandService.CreateCommandsAsync(client.Rest, client.Id);

client.Log += message =>
{
    Console.WriteLine(message);
    return default;
};

await client.StartAsync();
await Task.Delay(-1);