using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBot
{
    class Program
    {
        private DiscordSocketClient _client;
        private InteractionService _interactionService;
        private IServiceProvider _services;

        static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

        public async Task RunBotAsync()
        {
            var config = new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
            };

            _client = new DiscordSocketClient(config);
            _interactionService = new InteractionService(_client.Rest);

            _client.Log += Log;
            _client.Ready += RegisterCommandsAsync;

            // Fetch the bot token from the environment variable
            var token = Environment.GetEnvironmentVariable("DISCORD_TOKEN");

            if (string.IsNullOrWhiteSpace(token))
            {
                Console.WriteLine("Error: DISCORD_TOKEN environment variable is not set.");
                return;
            }

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            _client.InteractionCreated += HandleInteraction;

            // Block this task until the program is closed.
            await Task.Delay(-1);
        }

        private async Task RegisterCommandsAsync()
        {
            var postCommand = new SlashCommandBuilder()
                .WithName("post")
                .WithDescription("Post something anonymously")
                .AddOption("text", ApplicationCommandOptionType.String, "The text to post", isRequired: true)
                .AddOption("file", ApplicationCommandOptionType.Attachment, "Optional file to attach", isRequired: false);

            // Replace 'your_guild_id_here' with your actual guild ID
            var guildId = ulong.Parse(Environment.GetEnvironmentVariable("GUILD_ID")); // Example: 123456789012345678
            var guild = _client.GetGuild(guildId);

            await guild.DeleteApplicationCommandsAsync(); // Clear any existing commands in the guild
            await _client.Rest.DeleteAllGlobalCommandsAsync();
            await guild.CreateApplicationCommandAsync(postCommand.Build());

            Console.WriteLine("Slash command /post registered for guild");
        }

        private async Task HandleInteraction(SocketInteraction interaction)
        {
            if (interaction is SocketSlashCommand command)
            {
                if (command.CommandName == "post")
                {
                    var text = command.Data.Options.FirstOrDefault(x => x.Name == "text")?.Value?.ToString();
                    var attachmentOption = command.Data.Options.FirstOrDefault(x => x.Name == "file")?.Value as Attachment;

                    var user = command.User; // Get the user who triggered the command

                    // Log the user and the message text
                    Console.WriteLine($"[{DateTime.Now}] {user.Username} ({user.Id}) sent: {text}");

                    await command.RespondAsync("Your post has been sent!", ephemeral: true); // Responds privately

                    if (attachmentOption != null)
                    {
                        using (var httpClient = new HttpClient())
                        using (var stream = await httpClient.GetStreamAsync(attachmentOption.Url))
                        {
                            await command.Channel.SendFileAsync(stream, attachmentOption.Filename, text); // Sends the message with the file in the channel
                        }
                    }
                    else
                    {
                        await command.Channel.SendMessageAsync(text); // Sends the message in the channel
                    }
                }
            }
        }

        private Task Log(LogMessage msg)
        {
            Console.WriteLine(msg.ToString());
            return Task.CompletedTask;
        }
    }
}
