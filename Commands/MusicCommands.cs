using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;

namespace BrusselMusicBot.Commands
{
    class MusicCommands : BaseCommandModule
    {
        private static bool isPaused = false;
        private static bool isLooping = false;
        private static LavalinkTrack currentTrack = null;

        [Command("play")]
        public async Task Play(CommandContext ctx, [RemainingText] string search)
        {
            if (ctx.Member.VoiceState == null || ctx.Member.VoiceState.Channel == null)
            {
                await ctx.RespondAsync("You are not in a voice channel.");
                return;
            }

            var lava = ctx.Client.GetLavalink();
            var node = lava.ConnectedNodes.Values.First();
            var conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);

            // Join the user's channel if we aren't already in it
            if (conn == null || ctx.Member.VoiceState.Channel != conn.Channel)
            {
                await Join(ctx);

                // Get the new conn after we join
                lava = ctx.Client.GetLavalink();
                node = lava.ConnectedNodes.Values.First();
                conn = node.GetGuildConnection(ctx.Member.VoiceState.Guild);
            }

            if (conn == null)
            {
                await ctx.RespondAsync("Lavalink is not connected.");
                return;
            }

            var loadResult = await node.Rest.GetTracksAsync(search);

            if (loadResult.LoadResultType == LavalinkLoadResultType.LoadFailed
                || loadResult.LoadResultType == LavalinkLoadResultType.NoMatches)
            {
                await ctx.RespondAsync($"Track search failed for {search}.");
                return;
            }

            currentTrack = loadResult.Tracks.First();

            await PlayTrack(conn);
            conn.PlaybackFinished += HandleLooping;

            await ctx.RespondAsync($"Now playing {currentTrack.Title}!");
        }

        /// <summary>
        /// Plays the passed in track on the passed in conn.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="track"></param>
        /// <returns></returns>
        public async Task PlayTrack(LavalinkGuildConnection conn)
        {
            await conn.PlayAsync(currentTrack);
        }

        /// <summary>
        /// If isLooping is true, calls PlayTrack with the current track on the current conn.
        /// The Play command subscribes this to conn.PlaybackFinished.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public async Task HandleLooping(LavalinkGuildConnection conn, TrackFinishEventArgs args)
        {
            if (isLooping)
            {
                await PlayTrack(conn);
            }
        }

        [Command("pause")]
        public async Task Pause(CommandContext ctx)
        {
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
        }

        [Command("loop")]
        public async Task Loop(CommandContext ctx)
        {
            isLooping = !isLooping;
            await ctx.RespondAsync($"Looping: {isLooping}");
        }

        [Command("join")]
        public async Task Join(CommandContext ctx)
        {
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
    }
}
