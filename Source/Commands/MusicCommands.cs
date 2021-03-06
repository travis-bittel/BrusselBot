using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BrusselMusicBot.Source;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Humanizer;

namespace BrusselMusicBot.Commands
{
    class MusicCommands : BaseCommandModule
    {
        /// <summary>
        /// Maximum number of search results to list when performing the search command.
        /// </summary>
        public const int SEARCH_NUM_TRACKS_TO_LIST = 5;

        [Command("play")]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            Console.WriteLine($"[Music]: Play Command by {ctx.Member.DisplayName} in guild {ctx.Member.Guild} in channel {ctx.Message.Channel.Name} with search {search}");
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var conn = GetConn(ctx);

            // Join the user's channel if we aren't already in it
            if (conn == null || ctx.Member.VoiceState.Channel != conn.Channel)
            {
                await Join(ctx);

                // Get the new conn after we join
                conn = GetConn(ctx);
            }

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            // Not sure why we need to do this to be honest
            var loadResult = await conn.Node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
                || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for **{search}**!");
                return;
            }
            LavalinkTrack track = GetTrackAsync(ctx, search).Result;

            if (GetConn(ctx).CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync($"Now playing **{track.Title}**");
            } else
            {
                await ctx.RespondAsync($"Queued **{track.Title}**");
            }

            await Music.EnqueueTrack(conn, track);
        }

        /// <summary>
        /// A variant of the Play command to be used from the console.
        /// </summary>
        /// <param name="search"></param>
        /// <returns></returns>
        public static async Task ConsolePlay(LavalinkGuildConnection conn, string search)
        {
            if (conn != null && conn.Channel != null)
            {
                LavalinkTrack track = GetTrackAsync(conn, search).Result;
                await Music.EnqueueTrack(conn, track);
            }
        }

        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            Console.WriteLine($"[Music]: Pause Command by {ctx.Member.DisplayName}");
            await Music.TogglePauseAsync(GetConn(ctx));
            if (Music.GetMusicInstance(GetConn(ctx)).IsPaused)
            {
                await ctx.RespondAsync($"Paused!");
            } else
            {
                await ctx.RespondAsync($"Resuming!");
            }
        }

        [Command("loop")]
        public async Task Loop(CommandContext ctx)
        {
            Console.WriteLine($"[Music]: Loop Command by {ctx.Member.DisplayName}");
            Music.ToggleLooping(GetConn(ctx));
            if (Music.GetMusicInstance(GetConn(ctx)).IsLooping)
            {
                await ctx.RespondAsync($"Now Looping: **{GetConn(ctx).CurrentState.CurrentTrack.Title}**");
            } else
            {
                await ctx.RespondAsync($"Stopped Looping");
            }
        }

        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            Console.WriteLine($"[Music]: Join Command by {ctx.Member.DisplayName} into channel {ctx.Member.VoiceState.Channel}");
            var lava = ctx.Client.GetLavalink();
            var channel = ctx.Member.VoiceState.Channel;
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established!");
                return;
            }

            var node = lava.ConnectedNodes.Values.First();

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel!");
                return;
            }

            await node.ConnectAsync(channel);
            await ctx.RespondAsync($"Joined **{channel.Name}**!");
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            Console.WriteLine($"[Music]: Leave Command by {ctx.Member.DisplayName} from channel {ctx.Member.VoiceState.Channel}");
            var lava = ctx.Client.GetLavalink();
            var channel = ctx.Member.VoiceState.Channel;
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established!");
                return;
            }

            var node = lava.ConnectedNodes.Values.First();

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("I'm not currently in a voice channel!");
                return;
            }

            var conn = node.GetGuildConnection(channel.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected!");
                return;
            }

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Disconnecting From **{channel.Name}**!");
            Music.RemoveMusicInstance(conn);
        }

        [Command("queue")]
        public async Task Queue(CommandContext ctx)
        {
            string str = "";
            int i = 1;
            LavalinkTrack[] tracks = Music.GetMusicInstance(GetConn(ctx)).trackList.ToArray();
            str += $"**Now Playing: {GetConn(ctx).CurrentState.CurrentTrack.Title}**\n\n";
            foreach (LavalinkTrack track in tracks)
            {
                str += $"{i}. {track.Title}\n";
                i++;
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor("#FF0000"),
                Title = "Current Queue",
                Description = str,
                Timestamp = DateTime.UtcNow
            };

            await ctx.RespondAsync(embed);
        }

        [Command("skip")]
        public async Task Skip(CommandContext ctx)
        {
            await ctx.RespondAsync($"Skipping **{GetConn(ctx).CurrentState.CurrentTrack.Title}**");
            await GetConn(ctx).SeekAsync(GetConn(ctx).CurrentState.CurrentTrack.Length);
        }

        [Command("playskip")]
        public async Task PlaySkip(CommandContext ctx, [RemainingText] string search)
        {
            LavalinkTrack track = GetTrackAsync(ctx, search).Result;
            await ctx.RespondAsync($"Playing **{track.Title}**");
            Music.GetMusicInstance(GetConn(ctx)).trackList.Insert(0, track);
            await Skip(ctx);
        }

        [Command("seek")]
        public async Task Seek(CommandContext ctx, string position)
        {
            int pos = int.Parse(position);
            await GetConn(ctx).SeekAsync(TimeSpan.FromSeconds(pos));
            await NowPlaying(ctx);
        }

        [Command("np")]
        public async Task NowPlaying(CommandContext ctx)
        {
            MusicInstance instance = Music.GetMusicInstance(GetConn(ctx));

            if (instance != null)
            {
                var embed = new DiscordEmbedBuilder
                {
                    Color = new DiscordColor("#FF0000"),
                    Title = "Now Playing",
                    Description = $"**{GetConn(ctx).CurrentState.CurrentTrack.Title}**" +
                        $"\n{GetConn(ctx).CurrentState.CurrentTrack.Uri}" +
                        $"\n{GetConn(ctx).CurrentState.PlaybackPosition:hh\\:mm\\:ss} " +
                        $"/ {GetConn(ctx).CurrentState.CurrentTrack.Length:hh\\:mm\\:ss}",
                    Timestamp = DateTime.UtcNow
                };

                await ctx.RespondAsync(embed);
            }
        }

        [Command("search")]
        public async Task Search(CommandContext ctx, [RemainingText] string search)
        {
            Console.WriteLine($"[Music]: Search Command by {ctx.Member.DisplayName} in guild {ctx.Member.Guild} in channel {ctx.Message.Channel.Name} with search {search}");
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var conn = GetConn(ctx);

            // Join the user's channel if we aren't already in it
            if (conn == null || ctx.Member.VoiceState.Channel != conn.Channel)
            {
                await Join(ctx);

                // Get the new conn after we join
                conn = GetConn(ctx);
            }

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            var loadResult = await conn.Node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
                || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for **{search}**!");
                return;
            }

            LavalinkTrack[] tracks = loadResult.Tracks.ToArray();

            string trackList = "";
            for (int i = 0; i < SEARCH_NUM_TRACKS_TO_LIST; i++)
            {
                // 1 + i so the list starts counting from 1
                trackList += $"**{1 + i}.** {tracks[i].Title}  ({tracks[i].Length:m\\:ss})\n\n";
            }

            var embed = new DiscordEmbedBuilder
            {
                Color = new DiscordColor("#FF0000"),
                Title = "Search Results",
                Description = trackList,
                Timestamp = DateTime.UtcNow
            };
            var message = await ctx.RespondAsync(embed);

            // This places all of our reactions and sets up the dictionary of emojis to reference later
            // Looks hacky, but getting the name from the reaction returns weird strings, so this
            // seems like our best bet here.
            Dictionary<DiscordEmoji, int> reactionToInt = new Dictionary<DiscordEmoji, int>();
            for (int i = 1; i <= SEARCH_NUM_TRACKS_TO_LIST; i++)
            {
                DiscordEmoji emoji = DiscordEmoji.FromName(ctx.Client, $":{i.ToWords()}:");
                await message.CreateReactionAsync(emoji);
                reactionToInt[DiscordEmoji.FromName(ctx.Client, $":{i.ToWords()}:")] = i;
            }

            // Get the first reaction by the issuing user
            var result = await message.WaitForReactionAsync(ctx.Member);
            if (result.Result.Emoji != null)
            {
                DiscordEmoji reaction = result.Result.Emoji;

                // We delete the embed once the user reacts
                await message.DeleteAsync();

                await ctx.RespondAsync($"Selected: **{tracks[reactionToInt[reaction] - 1].Title}**");

                // -1 because our emojis start from 1
                await Music.EnqueueTrack(conn, tracks[reactionToInt[reaction] - 1]);
            }
        }

        [Command("volume")]
        public async Task SetVolume(CommandContext ctx, int value)
        {
            await GetConn(ctx).SetVolumeAsync(value);
            await ctx.RespondAsync($"Volume set to **{value} / 100**");
        }

        private static LavalinkGuildConnection GetConn(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            return node.GetGuildConnection(ctx.Member.VoiceState.Guild);
        }

        private static async Task<LavalinkTrack> GetTrackAsync(CommandContext ctx, string search)
        {
            if (GetConn(ctx) == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return null;
            }

            var loadResult = await GetConn(ctx).Node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
                || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for {search}.");
                return null;
            }
            return loadResult.Tracks.First();
        }

        /// <summary>
        /// Takes in a conn rather than a context. This means this method cannot create Discord messages.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="search"></param>
        /// <returns></returns>
        private static async Task<LavalinkTrack> GetTrackAsync(LavalinkGuildConnection conn, string search)
        {
            if (conn == null)
            {
                return null;
            }

            var loadResult = await conn.Node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
                || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                return null;
            }
            return loadResult.Tracks.First();
        }
    }
}