using BrusselMusicBot.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System;
using System.Threading.Tasks;
using DSharpPlus.Net;
using DSharpPlus.Lavalink;
using System.Xml;

namespace BrusselMusicBot
{
    class Program
    {
        private static BotSettings settings;

        static void Main()
        {
            MainAsync().GetAwaiter().GetResult();
        }

        static async Task MainAsync()
        {
            settings = new BotSettings("settings.xml");

            var discord = new DiscordClient(new DiscordConfiguration()
            {
                Token = settings.token,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.AllUnprivileged
            });

            var commands = discord.UseCommandsNext(new CommandsNextConfiguration()
            {
                StringPrefixes = new[] { settings.prefix }
            });
            commands.RegisterCommands<MusicCommands>();

            // Lavalink
            var endpoint = new ConnectionEndpoint
            {
                Hostname = settings.lavalinkHostName,
                Port = settings.lavalinkPort
            };

            var lavalinkConfig = new LavalinkConfiguration
            {
                Password = settings.lavalinkPassword,
                RestEndpoint = endpoint,
                SocketEndpoint = endpoint
            };
            var lavalink = discord.UseLavalink();

            Console.WriteLine($"Running BrusselBot Version {settings.version}");
            await discord.ConnectAsync();
            Console.WriteLine($"Logged In");
            await lavalink.ConnectAsync(lavalinkConfig);
            Console.WriteLine($"Lavalink Connection Established");

            // Handle console commands
            while (true)
            {
                await GetInputAsync();
            }
        }

        /// <summary>
        /// We call this repeatedly to handle console commands.
        /// </summary>
        /// <returns></returns>
        private static async Task GetInputAsync()
        {
            string input = Task.Run(() => Console.ReadLine()).Result;
            switch (input)
            {
                case "help":
                    Console.WriteLine(
                        "----- Commands -----" +
                        "\nsettings: Displays the currently used bot settings, loaded at startup" +
                        "\nversion: Displays the currently running bot version" + 
                        "\n--------------------");
                    break;
                case "settings":
                    Console.WriteLine(
                        $"----- Settings -----" + 
                        $"\n{settings}" +
                        "\n--------------------");
                    break;
                case "version":
                    Console.WriteLine($"Current Version: {settings.version}");
                    break;
                default:
                    Console.WriteLine("Command unknown. Type \"help\" for a list of commands.");
                    break;
            }
        }
    }

    public class BotSettings
    {
        public string version;
        public string token;
        public string prefix;
        public string lavalinkHostName;
        public int lavalinkPort;
        public string lavalinkPassword;

        /// <summary>
        /// Creates a new BotSettings data class from the passed in XML filepath.
        /// Writes warnings to the console if the file is not loaded properly or
        /// any settings are missing.
        /// </summary>
        /// <param name="filepath"></param>
        public BotSettings(string filepath)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filepath);
                XmlElement root = doc.DocumentElement;

                version = root.SelectSingleNode("version").InnerText;
                token = root.SelectSingleNode("token").InnerText;
                prefix = root.SelectSingleNode("prefix").InnerText;
                lavalinkHostName = root.SelectSingleNode("lavalink/hostname").InnerText;
                lavalinkPort = int.Parse(root.SelectSingleNode("lavalink/port").InnerText);
                lavalinkPassword = root.SelectSingleNode("lavalink/password").InnerText;
            }
            catch
            {
                Console.WriteLine("Failed to load settings from settings.xml! " +
                    "Make sure settings.xml is properly formatted and in the " +
                    "same directory as the exe file!");
            }

            // Warn the user if any settings are missing
            if (string.IsNullOrEmpty(token.Trim()))
            {
                Console.WriteLine("[ERROR]: No bot token specified");
            }
            if (string.IsNullOrEmpty(prefix.Trim()))
            {
                Console.WriteLine("[ERROR]: No command prefix specified");
            }
            if (string.IsNullOrEmpty(lavalinkHostName.Trim()))
            {
                Console.WriteLine("[ERROR]: No Lavalink Host Name specified");
            }
            if (string.IsNullOrEmpty(lavalinkPassword.Trim()))
            {
                Console.WriteLine("[ERROR]: No Lavalink Password specified");
            }

        }

        public override string ToString()
        {
            return $"Version: {version}\nToken: {token}\nPrefix: {prefix}\nLL Host: {lavalinkHostName}" +
                $"\nLL Port: {lavalinkPort}\nLL Password: {lavalinkPassword}";
        }
    }
}
