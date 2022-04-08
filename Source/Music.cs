using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Contains all of our music functionality (minus the commands).
/// Includes a main player loop which progresses through the current
/// queue of tracks.
/// </summary>
namespace BrusselMusicBot.Source
{
    class Music
    {
        private static ConcurrentDictionary<LavalinkGuildConnection, MusicInstance> musicInstances 
            = new ConcurrentDictionary<LavalinkGuildConnection, MusicInstance>();

        /// <summary>
        /// Plays the passed in track on the passed in conn.
        /// </summary>
        /// <param name="conn"></param>
        /// <param name="track"></param>
        /// <returns></returns>
        public static async Task EnqueueTrack(LavalinkGuildConnection conn, LavalinkTrack track)
        {
            Console.WriteLine("Enqueue called");

            MusicInstance instance = musicInstances.GetValueOrDefault(conn);

            if (instance == null)
            {
                instance = new MusicInstance(conn);
                musicInstances.TryAdd(conn, instance);
            }
            instance.trackList.Add(track);
            if (conn.CurrentState.CurrentTrack == null)
            {
                await instance.PlayNextTrack();
            }
        }

        public static async Task TogglePauseAsync(LavalinkGuildConnection conn)
        {
            await musicInstances.GetValueOrDefault(conn).TogglePaused();
        }

        public static void ToggleLooping(LavalinkGuildConnection conn)
        {
            musicInstances.GetValueOrDefault(conn).ToggleLooping();
        }

        /// <summary>
        /// Called when the bot disconnects from a channel so we remove the MusicInstance
        /// associated with it from the dictionary.
        /// </summary>
        /// <param name="conn"></param>
        public static void RemoveMusicInstance(LavalinkGuildConnection conn)
        {
            musicInstances.TryRemove(conn, out _);
        }

        public static MusicInstance GetMusicInstance(LavalinkGuildConnection conn)
        {
            return musicInstances.GetValueOrDefault(conn);
        }

        public static LavalinkGuildConnection[] GetMusicInstancesAsArray()
        {
            LavalinkGuildConnection[] conns = new LavalinkGuildConnection[musicInstances.Keys.Count];
            musicInstances.Keys.CopyTo(conns, 0);
            return conns;
        }
    }

    /// <summary>
    /// We keep a MusicInstance object for each LavalinkGuildConnection.
    /// Commands call methods here and pass in the guild connection to
    /// let us grab the right MusicInstance to perform the command on.
    /// </summary>
    class MusicInstance
    {
        private LavalinkGuildConnection conn;

        public List<LavalinkTrack> trackList;
        public bool isLooping;
        private bool isPaused;
        public bool IsPaused { get { return isPaused; } }
        public bool IsLooping { get { return isLooping; } }

        public LavalinkTrack LastTrack { get; set; }

        public MusicInstance(LavalinkGuildConnection conn) {
            this.conn = conn;
            trackList = new List<LavalinkTrack>();
            isLooping = false;
            isPaused = false;
            conn.PlaybackFinished += PlayNextTrack;
            conn.DiscordWebSocketClosed += (conn, args) =>
            {
                Music.RemoveMusicInstance(conn);
                return Task.CompletedTask;
            };
        }

        // This feels horribly hacky (the args I mean). Clean this up later
        public async Task PlayNextTrack(LavalinkGuildConnection conn = null, TrackFinishEventArgs args = null)
        {
            if (conn == null)
            {
                conn = this.conn;
            }

            if (isLooping && LastTrack != null)
            {
                await conn.PlayAsync(LastTrack);
            }
            else
            {
                LavalinkTrack track = null;
                if (trackList.Count > 0)
                {
                    track = trackList[0];
                    trackList.RemoveAt(0);
                }

                if (track != null)
                {
                    LastTrack = track;
                    await conn.PlayAsync(track);
                }
            }
        }

        public async Task TogglePaused()
        {
            isPaused = !isPaused;
            if (isPaused)
            {
                await conn.PauseAsync();
            } 
            else
            {
                await conn.ResumeAsync();
            }
        }

        public void ToggleLooping()
        {
            isLooping = !isLooping;
        }
    }
}
