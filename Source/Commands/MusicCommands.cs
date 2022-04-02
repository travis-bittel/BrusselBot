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
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;

namespace BrusselMusicBot.Commands
{
    class MusicCommands : BaseCommandModule
    {
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

        private LavalinkGuildConnection GetConn(CommandContext ctx)
        {
            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            return node.GetGuildConnection(ctx.Member.VoiceState.Guild);
        }

        private async Task<LavalinkTrack> GetTrackAsync(CommandContext ctx, string search)
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
    }
}
