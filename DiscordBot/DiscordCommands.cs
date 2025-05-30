﻿using DiscordBot.Helpers;
using Minecraft_Easy_Servers;
using Minecraft_Easy_Servers.Commands;
using Minecraft_Easy_Servers.Helpers;
using NetCord.Rest;
using NetCord.Services.ApplicationCommands;
using System.IO.Pipelines;
using System.Text.Json;

namespace DiscordBot
{
    public class DiscordCommands : ApplicationCommandModule<ApplicationCommandContext>
    {
        private const int DISCORD_MESSAGE_LIMIT = 2000;
        private const int ONE_MESSAGE_PER_MILLISECONDS = 1000;
        private static readonly CLI CLI = GetCLI();

        private static CLI GetCLI()
        {
            var botConfig = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(File.ReadAllText("bot.json"));
            var rootPath = botConfig?.TryGetValue("rootPath", out var path) == true ? path.GetString() : null;

            return string.IsNullOrEmpty(rootPath) ? CLI.Create() : CLI.Create(rootPath);
        }

        private static volatile bool IsRunning = false;

        [SlashCommand("add-server", "Creates a new Minecraft server.")]
        public async Task<string> AddServer(string serverName, string configName)
        {
            return RunBigCommand(() => CLI.Run(new AddServer
            {
                Name = serverName,
                Config = configName
            }), "Add Server", await Context.Channel.SendMessageAsync("Creating server..."), TimeSpan.FromSeconds(60));
        }

        [SlashCommand("add-config", "Creates a new server configuration.")]
        public async Task<string> AddConfig(string configName, string modLoader, string version)
        {
            return RunBigCommand(() => CLI.Run(new AddConfig
            {
                Name = configName,
                ModLoader = modLoader,
                Version = version
            }), "add-config", await Context.Channel.SendMessageAsync("Creating config..."), TimeSpan.FromMinutes(5), lastNumberLines: 5);
        }

        [SlashCommand("status-server", "Checks the status of a server.")]
        public async Task<string> StatusServer(string serverName)
        {
            return await RunSmallCommand(() => CLI.Run(new CheckStatus
            {
                Name = serverName
            }));
        }

        [SlashCommand("down-server", "Stops a running server.")]
        public async Task<string> DownServer(string serverName)
        {
            return await RunSmallCommand(() => CLI.Run(new DownServer
            {
                Name = serverName
            }));
        }

        [SlashCommand("up-server", "Starts a server.")]
        public async Task<string> UpServer(string serverName, int port)
        {
            return RunBigCommand(() => CLI.Run(new UpServer
            {
                Name = serverName,
                Port = port
            }), "Up Server", await Context.Channel.SendMessageAsync("Launching server ..."), TimeSpan.FromSeconds(60));
        }

        [SlashCommand("send-command", "Sends a command to a server.")]
        public async Task<string> SendCommand(string serverName, string command)
        {
            // Not a big command, but need to display large text.
            if (command.Contains("help"))
            {
                return RunBigCommand(() => CLI.Run(new SendCommand
                {
                    ServerName = serverName,
                    Command = command
                }), "Help command", await Context.Channel.SendMessageAsync("Help command result:"), TimeSpan.FromSeconds(5));
            }
            return await RunSmallCommand(() => CLI.Run(new SendCommand
            {
                ServerName = serverName,
                Command = command
            }), displayAllLines: true);
        }

        [SlashCommand("remove-server", "Removes a server.")]
        public async Task<string> RemoveServer(string serverName)
        {
            return await RunSmallCommand(() => CLI.Run(new RemoveServer
            {
                Name = serverName
            }));
        }

        [SlashCommand("add-mod", "Adds a mod to a configuration.")]
        public async Task<string> AddMod(string configName, string modName, string link, bool clientSide = false, bool serverSide = false)
        {
            return await RunSmallCommand(() => CLI.Run(new AddMod
            {
                ConfigName = configName,
                Name = modName,
                Link = link,
                ClientSide = clientSide,
                ServerSide = serverSide
            }));
        }

        [SlashCommand("remove-config", "Removes a server configuration.")]
        public async Task<string> RemoveConfig(string configName)
        {
            return await RunSmallCommand(() => CLI.Run(new RemoveConfig
            {
                Name = configName
            }));
        }

        [SlashCommand("remove-mod", "Removes a mod from a configuration.")]
        public async Task<string> RemoveMod(string configName, string modName)
        {
            return await RunSmallCommand(() => CLI.Run(new RemoveMod
            {
                ConfigName = configName,
                Name = modName
            }));
        }

        [SlashCommand("add-plugin", "Adds a plugin to a configuration.")]
        public async Task<string> AddPlugin(string configName, string pluginName, string link)
        {
            return await RunSmallCommand(() => CLI.Run(new AddPlugin
            {
                ConfigName = configName,
                Name = pluginName,
                Link = link
            }));
        }

        [SlashCommand("add-resource-pack", "Adds a resource pack to a configuration.")]
        public async Task<string> AddResourcePack(string configName, string resourcePackName, string link, bool serverDefault = false)
        {
            return await RunSmallCommand(() => CLI.Run(new AddResourcePack
            {
                ConfigName = configName,
                Name = resourcePackName,
                Link = link,
                ServerDefault = serverDefault
            }));
        }

        [SlashCommand("remove-plugin", "Removes a plugin from a configuration.")]
        public async Task<string> RemovePlugin(string configName, string pluginName)
        {
            return await RunSmallCommand(() => CLI.Run(new RemovePlugin
            {
                ConfigName = configName,
                Name = pluginName
            }));
        }

        [SlashCommand("remove-resource-pack", "Removes a resource pack from a configuration.")]
        public async Task<string> RemoveResourcePack(string configName, string resourcePackName)
        {
            return await RunSmallCommand(() => CLI.Run(new RemoveResourcePack
            {
                ConfigName = configName,
                Name = resourcePackName
            }));
        }

        [SlashCommand("remove-world", "Removes a world from a configuration.")]
        public async Task<string> RemoveWorld(string configName, string worldName)
        {
            return await RunSmallCommand(() => CLI.Run(new RemoveWorld
            {
                ConfigName = configName,
                Name = worldName
            }));
        }

        [SlashCommand("add-world", "Adds a world to a configuration.")]
        public async Task<string> AddWorld(string configName, string worldName, string link, bool serverDefault = false)
        {
            return await RunSmallCommand(() => CLI.Run(new AddWorld
            {
                ConfigName = configName,
                Name = worldName,
                Link = link,
                ServerDefault = serverDefault
            }));
        }

        [SlashCommand("set-server-world", "Sets the world for a server.")]
        public async Task<string> SetServerWorld(string serverName, string link)
        {
            return await RunSmallCommand(() => CLI.Run(new SetServerWorld
            {
                ServerName = serverName,
                Link = link
            }));
        }

        [SlashCommand("set-server-resource-pack", "Sets the resource pack for a server.")]
        public async Task<string> SetServerResourcePack(string serverName, string link)
        {
            return await RunSmallCommand(() => CLI.Run(new SetServerResourcePack
            {
                ServerName = serverName,
                Link = link
            }));
        }

        [SlashCommand("list-servers", "Lists all servers.")]
        public async Task<string> ListServers()
        {
            return await RunSmallCommand(() => CLI.Run(new ListServers()), displayAllLines: true);
        }

        [SlashCommand("list-configs", "Lists all configs.")]
        public async Task<string> ListConfigs()
        {
            return await RunSmallCommand(() => CLI.Run(new ListConfigs()), displayAllLines: true);
        }

        [SlashCommand("list-assets", "Lists all assets of config.")]
        public async Task<string> ListAssets(string configName)
        {
            return await RunSmallCommand(() => CLI.Run(new ListAssets() { ConfigName = configName }), displayAllLines: true);
        }

        [SlashCommand("list-server-assets", "Lists all assets of server.")]
        public async Task<string> ListServerAssets(string serverName)
        {
            return await RunSmallCommand(() => CLI.Run(new ListServerAssets() { ServerName = serverName }), displayAllLines: true);
        }

        [SlashCommand("set-server-property", "Sets a property for a server.")]
        public async Task<string> SetServerProperty(string serverName, string keyValue)
        {
            return await RunSmallCommand(() => CLI.Run(new SetServerProperty
            {
                ServerName = serverName,
                KeyValue = keyValue
            }));
        }

        [SlashCommand("add-server-mod", "Adds a mod to a server.")]
        public async Task<string> AddServerMod(string serverName, string modName, string link)
        {
            return await RunSmallCommand(() => CLI.Run(new AddServerMod
            {
                ServerName = serverName,
                Name = modName,
                Link = link
            }));
        }

        [SlashCommand("add-server-plugin", "Adds a plugin to a server.")]
        public async Task<string> AddServerPlugin(string serverName, string pluginName, string link)
        {
            return await RunSmallCommand(() => CLI.Run(new AddServerPlugin
            {
                ServerName = serverName,
                Name = pluginName,
                Link = link
            }));
        }

        [SlashCommand("remove-server-mod", "Removes a mod from a server.")]
        public async Task<string> RemoveServerMod(string serverName, string modName)
        {
            return await RunSmallCommand(() => CLI.Run(new RemoveServerMod
            {
                ServerName = serverName,
                Name = modName
            }));
        }

        [SlashCommand("remove-server-plugin", "Removes a plugin from a server.")]
        public async Task<string> RemoveServerPlugin(string serverName, string pluginName)
        {
            return await RunSmallCommand(() => CLI.Run(new RemoveServerPlugin
            {
                ServerName = serverName,
                Name = pluginName
            }));
        }

        [SlashCommand("restart", "restart the discord bot")]
        public string Restart()
        {
            var channelId = Context.Channel.Id;
            _ = Task.Run(() =>
            {
                Thread.Sleep(2000);
                RestartHelper.RestartBot(channelId);
            });

            return "Restarting bot... Don't overuse restart, the bot can take a long time to connect again.";
        }

        public string RunBigCommand(Func<Task> operation, string commandName, RestMessage editableMessage, TimeSpan timeout, int? lastNumberLines = null)
        {
            if (IsRunning)
                return "Another command is already running.";

            IsRunning = true;

            var cancellationToken = new CancellationTokenSource(timeout);
            var pipe = new Pipe();
            _ = Task.Run(async () =>
            {
                using var newOut = new StdOutAndStreamWriter(new StreamWriter(pipe.Writer.AsStream()));
                Console.SetOut(newOut);
                using var streamReader = new StreamReader(pipe.Reader.AsStream());
                var currentMessage = editableMessage.Content + "\n";
                var currentMessageId = editableMessage.Id;
                var lastUpdateTime = DateTime.UtcNow;

                var messageLastEditOffset = 0;
                while (!cancellationToken.IsCancellationRequested)
                {
                    var newLine = await streamReader.ReadLineAsync(cancellationToken.Token);
                    if (newLine == null)
                        break;

                    if (lastNumberLines != null)
                    {
                        currentMessage += newLine + "\n";
                        continue;
                    }

                    if ((currentMessage.Length + newLine.Length) >= DISCORD_MESSAGE_LIMIT)
                    {
                        var messageToSend = currentMessage.Substring(messageLastEditOffset);
                        var newMessage = await Context.Channel.SendMessageAsync(messageToSend);
                        currentMessage = messageToSend + "\n";
                        messageLastEditOffset = currentMessage.Length;
                        currentMessageId = newMessage.Id;
                        lastUpdateTime = DateTime.UtcNow;
                    }
                    else
                    {
                        currentMessage += newLine + "\n";

                        // Ensure ModifyMessageAsync is called with a max frequency
                        if ((DateTime.UtcNow - lastUpdateTime).TotalMilliseconds >= ONE_MESSAGE_PER_MILLISECONDS)
                        {
                            await Context.Channel.ModifyMessageAsync(currentMessageId, m =>
                            {
                                m.Content = currentMessage;
                            }, cancellationToken: cancellationToken.Token);

                            messageLastEditOffset = currentMessage.Length;

                            lastUpdateTime = DateTime.UtcNow;
                        }
                    }
                }
                var toEnd = await streamReader.ReadToEndAsync();
                var finalMessage = currentMessage += toEnd;

                finalMessage = lastNumberLines != null ?
                    string.Join("\n", currentMessage.Split('\n').Skip(Math.Max(0, finalMessage.Split('\n').Length - lastNumberLines.Value))) :
                    finalMessage;

                if (lastNumberLines != null)
                {
                    await Context.Channel.SendMessageAsync($"Command finished ! Last {lastNumberLines} lines output: \n" + finalMessage);
                }
                else
                {
                    await Context.Channel.ModifyMessageAsync(currentMessageId, m =>
                    {
                        m.Content = finalMessage;
                    });
                }
            });

            _ = Task.Run(async () =>
            {
                try
                {
                    await operation().WaitAsync(cancellationToken.Token);
                }
                catch (Exception e)
                {
                    await Context.Channel.SendMessageAsync($"{commandName}: CLI Error: {e.Message}");
                }
                finally
                {
                    if (cancellationToken.IsCancellationRequested)
                    {
                        await Context.Channel.SendMessageAsync($"{commandName}: Command timed-out");
                    }
                    pipe.Writer.Complete();
                    Thread.Sleep(1000);
                    cancellationToken.Cancel();
                    IsRunning = false;
                }
            }, cancellationToken: cancellationToken.Token);

            return $"{commandName}: Command is running";
        }

        // Run a small command that returns only the last line ignoring other lines from the stream
        public async Task<string> RunSmallCommand(Func<Task> operation, bool displayAllLines = false)
        {
            if (IsRunning)
                return "Another command is already running.";

            IsRunning = true;
            try
            {
                var memoryStream = new MemoryStream();
                using var newOut = new StdOutAndStreamWriter(new StreamWriter(memoryStream));
                Console.SetOut(newOut);

                await operation();

                // Read the last line from the MemoryStream
                memoryStream.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(memoryStream);
                string? returnMessage = null;
                string? currentLine;

                while ((currentLine = await reader.ReadLineAsync()) != null)
                {
                    returnMessage = displayAllLines ? returnMessage += currentLine + "\n" : currentLine;
                }

                return returnMessage ?? "No output from the command.";
            }
            catch (Exception e)
            {
                return $"CLI Error: {e.Message}";
            }
            finally
            {
                IsRunning = false;
            }
        }
    }
}
