using BrusselMusicBot.Commands;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using System;
using System.Threading.Tasks;
using DSharpPlus.Net;
using DSharpPlus.Lavalink;
using System.Xml;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Enums;
using BrusselMusicBot.Source;
using System.Threading;

namespace BrusselMusicBot
{
    class Program
    {
        private static BotSettings settings;

        private static CommandsNextExtension commands;

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

            commands = discord.UseCommandsNext(new CommandsNextConfiguration()
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

            discord.UseInteractivity(new InteractivityConfiguration()
            {
                PollBehaviour = PollBehaviour.KeepEmojis,
                Timeout = TimeSpan.FromSeconds(30)
            });

            Console.WriteLine($"Running BrusselBot Version {settings.version}");
            await discord.ConnectAsync();
            Console.WriteLine($"Logged In");
            await lavalink.ConnectAsync(lavalinkConfig);
            Console.WriteLine($"Lavalink Connection Established");

            // Handle console commands in separate thread
            ThreadStart childref = new ThreadStart(GetInput);
            Thread consoleCommandsThread = new Thread(childref);
            consoleCommandsThread.Start();
        }

        /// <summary>
        /// We call this infinite loop in a separate thread to handle console commands.
        /// </summary>
        /// <returns></returns>
        private static void GetInput()
        {
            while (true)
            {
                // Break our input into words
                string[] input = Console.ReadLine().Split(' ');
                switch (input[0])
                {
                    case "help":
                        Console.WriteLine(
                            "----- Commands -----" +
                            "\nsettings: Displays the currently used bot settings, loaded at startup" +
                            "\nversion: Displays the currently running bot version" +
                            "\ngetConns: Displays all current bot connections (aka servers the bot is currently connected in)" +
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
                    case "getConns":
                        LavalinkGuildConnection[] conns = Music.GetMusicInstancesAsArray();
                        Console.WriteLine($"Current Connections ({conns.Length}):");
                        Array.ForEach(conns, (instance) => { Console.WriteLine($"- {instance.Guild}"); });
                        break;
                    case "execute":
                        _ = CommandExecute(input);
                        break;

                    default:
                        Console.WriteLine("Command unknown. Type \"help\" for a list of commands.");
                        break;
                }
            }
        }

        /// <summary>
        /// Used for the execute command since it's fairly complex and looks better as a separate method.
        /// Currently, this only supports the play command.
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        private static async Task CommandExecute(string[] input)
        {
            try
            {
                // Command formatted as: "execute <connIndex> <command> [command args]
                int connIndex = int.Parse(input[1]);
                LavalinkGuildConnection[] conns = Music.GetMusicInstancesAsArray();
                if (connIndex > conns.Length)
                {
                    throw new Exception();
                }
                switch (input[2])
                {
                    case "play":
                        await MusicCommands.ConsolePlay(conns[connIndex], input[3]);
                        break;
                    default:
                        Console.WriteLine("Invalid Command Entered!");
                        break;
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Invalid Conn Index Entered. Use getConns to get the list of conn indexes.");
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
