using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Discord;
using Discord.WebSocket;
using Discord.Commands;

namespace localbot
{
    class Program
    {

        static string path = @"./token.txt"; // put your own config w auth token
        static string token = File.ReadAllText(path);

        public static void Main(string[] args)
            => new Program().MainAsync().GetAwaiter().GetResult();

        public async Task MainAsync()
        {
            using (var services = ConfigureServices())
            {
                var client = services.GetRequiredService<DiscordSocketClient>();
                client.Log += LogAsync;
                client.Disconnected += Disconnected;
                services.GetRequiredService<CommandService>().Log += LogAsync;
                await client.LoginAsync(TokenType.Bot, token);
                await client.StartAsync();

                await services.GetRequiredService<CommandHandlingService>().InitializeAsync();
                services.GetRequiredService<PinService>();

                await Task.Delay(-1);
            }
        }

        private Task LogAsync(LogMessage log)
        {
            Console.WriteLine(log.ToString());

            return Task.CompletedTask;
        }
        
        private Task Disconnected(Exception e)
        {
            Console.WriteLine("Closing because of disconnection. Exception: " + e.ToString());
            Environment.Exit(1);
            return Task.CompletedTask;
        }

        private ServiceProvider ConfigureServices()
        {
            var config = new DiscordSocketConfig
            {
                LogLevel = LogSeverity.Debug
            };
            return new ServiceCollection()
                .AddSingleton<DiscordSocketClient>(new DiscordSocketClient(config))
                .AddSingleton<CommandService>()
                .AddSingleton<CommandHandlingService>()
                .AddSingleton<PinService>()
                .AddSingleton<HttpClient>()
                .BuildServiceProvider();
        }
    }
}