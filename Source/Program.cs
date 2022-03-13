using BrusselMusicBot.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System;
using System.Threading.Tasks;
using DSharpPlus.Net;
using DSharpPlus.Lavalink;

namespace BrusselMusicBot
{
    class Program
    {
        // <Current Date>.<New Versions On That Date>
        private const string version = "3-13-2022.0";

        static void Main(string[] args)
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = "OTUxODcxMjM2MzI0MDg5ODY2.YitxKw.oFEnq1t6Na_CuWxIYxXKFWo_4NI",
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged
            });
            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { "!" }
            });
            commands.RegisterCommands<MusicCommands>();

            // Lavalink
            var endpoint = new ConnectionEndpoint
            {
                Hostname = "127.0.0.1", // From your server configuration.
                Port = 2333 // From your server configuration
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = "youshallnotpass", // From your server configuration.
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            var lavalink = discord.UseLavalink();

            Console.WriteLine($"Running BrusselBot Version ${version}");
            await discord.ConnectAsync();
            Console.WriteLine($"Logged In");
            await lavalink.ConnectAsync(lavalinkConfig);
            Console.WriteLine($"Lavalink Established");
            await Task.Delay(-1);
        }
    }
}
