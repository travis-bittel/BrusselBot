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
            Console.WriteLine($"[Music]: Play Command by {ctx.Member.DisplayName} with search {search}");
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
                await ctx.RespondAsync($"Track search failed for {search}.");
                return;
            }
            LavalinkTrack track = GetTrackAsync(ctx, search).Result;
            await Music.EnqueueTrack(conn, track);

            await ctx.RespondAsync($"Now playing: *{track.Title}*");
        }

        /*[Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            Console.WriteLine($"[Music]: Pause Command by {ctx.Member.DisplayName} set isPaused to {!isPaused}");
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            if (conn.CurrentState.CurrentTrack == null)
            {
                await ctx.RespondAsync("There are no tracks loaded.");
                return;
            }

            isPaused = !isPaused;
            await ctx.RespondAsync($"Paused: {isPaused}");
            if (isPaused)
            {
                await conn.ResumeAsync();
            } else
            {
                await conn.PauseAsync();
            }
        }*/

        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
            Console.WriteLine($"[Music]: Pause Command by {ctx.Member.DisplayName}");
            await Music.TogglePauseAsync(GetConn(ctx));
            await ctx.RespondAsync($"Paused: {Music.GetMusicInstance(GetConn(ctx)).IsPaused}");
        }

        [Command("loop")]
        public async Task Loop(CommandContext ctx)
        {
            Console.WriteLine($"[Music]: Loop Command by {ctx.Member.DisplayName}");
            Music.ToggleLooping(GetConn(ctx));
            await ctx.RespondAsync($"Looping: {Music.GetMusicInstance(GetConn(ctx)).IsLooping}");
        }

        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
            Console.WriteLine($"[Music]: Join Command by {ctx.Member.DisplayName} into channel {ctx.Member.VoiceState.Channel}");
            var lava = ctx.Client.GetLavalink();
            var channel = ctx.Member.VoiceState.Channel;
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
                return;
            }

            var node = lava.ConnectedNodes.Values.First();

            if (channel.Type != ChannelType.Voice)
            {
                await ctx.RespondAsync("Not a valid voice channel.");
                return;
            }

            await node.ConnectAsync(channel);
            await ctx.RespondAsync($"Joined {channel.Name}!");
        }

        [Command("leave")]
        public async Task Leave(CommandContext ctx)
        {
            Console.WriteLine($"[Music]: Leave Command by {ctx.Member.DisplayName} from channel {ctx.Member.VoiceState.Channel}");
            var lava = ctx.Client.GetLavalink();
            var channel = ctx.Member.VoiceState.Channel;
            if (!lava.ConnectedNodes.Any())
            {
                await ctx.RespondAsync("The Lavalink connection is not established");
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
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            await conn.DisconnectAsync();
            await ctx.RespondAsync($"Left {channel.Name}!");
        }

        [Command("queue")]
        public async Task Queue(CommandContext ctx)
        {
            string str = "";
            int i = 1;
            LavalinkTrack[] tracks = Music.GetMusicInstance(GetConn(ctx)).trackList.ToArray();
            foreach (LavalinkTrack track in tracks)
            {
                str += $"{i}. {track.Title}\n";
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
            await GetConn(ctx).SeekAsync(GetConn(ctx).CurrentState.CurrentTrack.Length);
        }

        [Command("playskip")]
        public async Task PlaySkip(CommandContext ctx, [RemainingText] string search)
        {
            LavalinkTrack track = GetTrackAsync(ctx, search).Result;
            Music.GetMusicInstance(GetConn(ctx)).trackList.Insert(0, track);
            await Skip(ctx);
        }

        [Command("seek")]
        public async Task Seek(CommandContext ctx, string position)
        {
            int pos = int.Parse(position);
            await GetConn(ctx).SeekAsync(TimeSpan.FromSeconds(pos));
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
                    //Description = $"**{instance.LastTrack.Title}** \n{GetConn(ctx).CurrentState.PlaybackPosition.Minutes}:{GetConn(ctx).CurrentState.PlaybackPosition.Seconds} / {instance.LastTrack.Length.Minutes}:{instance.LastTrack.Length.Seconds}",
                    Description = $"**{GetConn(ctx).CurrentState.CurrentTrack.Title}**" +
                        $"\n{GetConn(ctx).CurrentState.PlaybackPosition} " +
                        $"/ {GetConn(ctx).CurrentState.CurrentTrack.Length}",
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
