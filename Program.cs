using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using CsvHelper;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

namespace DiscordBotExample
{
    class Program
    {
        private static List<string> _messages;
        private static Random _random = new Random();
        private static DiscordSocketClient _client;
        private static ulong _channelId;
        private static string _fileId;
        private static string _credentialsPath;
        private static TimeSpan _postTimeSpain;
        private static TimeZoneInfo _spainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time");

        static async Task Main(string[] args)
        {
            // Read environment variables
            var token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
            var channelIdStr = Environment.GetEnvironmentVariable("DISCORD_CHANNEL_ID");
            _fileId = Environment.GetEnvironmentVariable("GOOGLE_DRIVE_FILE_ID");
            _credentialsPath = Environment.GetEnvironmentVariable("GOOGLE_CREDENTIALS_PATH");
            var postTimeStr = Environment.GetEnvironmentVariable("POST_TIME");

            // Check if token, channelId, fileId, credentialsPath, or postTime is null or empty
            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(channelIdStr) || string.IsNullOrEmpty(_fileId) || string.IsNullOrEmpty(_credentialsPath) || string.IsNullOrEmpty(postTimeStr))
            {
                Console.WriteLine("Environment variables are not set correctly.");
                return;
            }

            // Parse channel ID
            if (!ulong.TryParse(channelIdStr, out _channelId))
            {
                Console.WriteLine("Invalid DISCORD_CHANNEL_ID format.");
                return;
            }

            // Parse post time
            if (!TimeSpan.TryParse(postTimeStr, out _postTimeSpain))
            {
                Console.WriteLine("Invalid POST_TIME format. It must be in the format HH:mm:ss.");
                return;
            }

            // Initialize the Discord client
            _client = new DiscordSocketClient();
            _client.Log += Log;
            _client.Ready += OnReady;

            // Start the bot
            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            // Block the application until it is closed
            await Task.Delay(-1);
        }

        private static Task Log(LogMessage log)
        {
            Console.WriteLine(log);
            return Task.CompletedTask;
        }

        private static async Task OnReady()
        {
            Console.WriteLine("Bot is connected.");

            // Download and process the CSV file from Google Drive
            var csvData = await DownloadCsvFromGoogleDrive();

            if (csvData != null)
            {
                using (var reader = new StringReader(csvData))
                using (var csv = new CsvReader(reader, new CsvHelper.Configuration.CsvConfiguration(CultureInfo.InvariantCulture)))
                {
                    var records = csv.GetRecords<YourRecordClass>().ToList();

                    // Filter records with at least 8 reactions
                    _messages = records.Where(record => record.total_reactions >= 8)
                                       .Select(record =>
                                       {
                                           string result = $"{record.name}: {record.message}\n{record.message_link.Trim()}";
                                           if (!string.IsNullOrWhiteSpace(record.image_url))
                                           {
                                               result += $"\n{record.image_url.Trim()}";
                                           }
                                           return result;
                                       }).ToList();
                }

                Console.WriteLine("Filtered messages read from CSV:");
                foreach (var message in _messages)
                {
                    Console.WriteLine(message);
                }
            }
            else
            {
                Console.WriteLine("Failed to download or read the CSV file. Exiting...");
                return;
            }

            // Check if messages is empty
            if (_messages.Count == 0)
            {
                Console.WriteLine("No valid messages available. Exiting...");
                return;
            }

            // Schedule the first post
            await ScheduleNextPost();
        }

        private static async Task ScheduleNextPost()
        {
            var nowUtc = DateTime.UtcNow;
            var spainTime = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, _spainTimeZone);

            var nextPostTimeSpain = DateTime.SpecifyKind(DateTime.Today.Add(_postTimeSpain), DateTimeKind.Unspecified);

            if (nextPostTimeSpain <= spainTime)
            {
                // If the time has already passed for today, schedule for tomorrow
                nextPostTimeSpain = nextPostTimeSpain.AddDays(1);
            }

            nextPostTimeSpain = TimeZoneInfo.ConvertTimeToUtc(nextPostTimeSpain, _spainTimeZone);

            var delay = nextPostTimeSpain - nowUtc;

            Console.WriteLine($"Scheduling next post in {delay.TotalMinutes} minutes.");

            await Task.Delay(delay);

            await PostRandomMessage();

            // Schedule the next post
            await ScheduleNextPost();
        }

        private static async Task<string> DownloadCsvFromGoogleDrive()
        {
            try
            {
                var credential = GoogleCredential.FromFile(_credentialsPath)
                    .CreateScoped(DriveService.Scope.DriveReadonly);

                var service = new DriveService(new BaseClientService.Initializer()
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "DiscordBotExample",
                });

                var request = service.Files.Get(_fileId);
                var stream = new MemoryStream();
                request.MediaDownloader.ProgressChanged += progress =>
                {
                    if (progress.Status == Google.Apis.Download.DownloadStatus.Completed)
                    {
                        Console.WriteLine("Download complete.");
                    }
                };

                await request.DownloadAsync(stream);

                stream.Position = 0;
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
                return null;
            }
        }

        private static async Task PostRandomMessage()
        {
            var channel = _client.GetChannel(_channelId) as IMessageChannel;

            if (channel != null && _messages.Count > 0)
            {
                int index = _random.Next(_messages.Count);
                string randomMessage = _messages[index];
                await channel.SendMessageAsync(randomMessage);
            }
            else
            {
                Console.WriteLine("No messages available.");
            }
        }
    }

    // Define a class that matches the CSV structure
    public class YourRecordClass
    {
        public string name { get; set; }
        public string message { get; set; }
        public string image_url { get; set; }
        public string message_link { get; set; }
        public string has_spoilers { get; set; }
        public int total_reactions { get; set; }
    }
}
