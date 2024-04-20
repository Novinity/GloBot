using DSharpPlus.Entities;
using DSharpPlus.SlashCommands;
using DSharpPlus.VoiceNext;
using YoutubeExplode;
using YoutubeExplode.Videos;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.ComponentModel.Design;

namespace DiscordBotTest {
    internal class QueueManager {
        public static Dictionary<DiscordGuild, List<Video>> queues = new Dictionary<DiscordGuild, List<Video>>();
        public static Dictionary<DiscordGuild, int> voteSkips = new Dictionary<DiscordGuild, int>();
        public static List<DiscordGuild> paused = new List<DiscordGuild>();

        public static bool LoopQueue = false;
        public static bool LoopSong = false;

        public static void CreateQueue(DiscordGuild guild) {
            queues[guild] = new List<Video>();
        }

        public static async Task PlayQueue(DiscordGuild guild, DiscordChannel channel) {
            List<Video> queue = queues[guild];
            if (queue == null) return;

            while (queue.Count > 0) {
                Video video = queue.First();
                if (!LoopSong)
                    await new DiscordMessageBuilder()
                        .WithContent($"Now playing [{video.Title}]({video.Url}) - {AbstractFunctions.FormatSeconds((float)video.Duration.Value.TotalSeconds)}")
                        .SendAsync(channel);

                var voiceNext = Program.Client.GetVoiceNext();
                var voiceNextConnection = voiceNext.GetConnection(guild);
                await AudioManager.PlaySong(video.Url, voiceNextConnection);

                await voiceNextConnection.WaitForPlaybackFinishAsync();
                await voiceNextConnection.SendSpeakingAsync(false);

                if (voteSkips.ContainsKey(guild))
                    voteSkips.Remove(guild);

                try {
                    MoveQueueUp(guild);
                } catch (Exception e) {
                    Console.WriteLine("MoveQueueUp failed, probably nothing though. - " + e.Message);
                }
            }

            await new DiscordMessageBuilder()
                    .WithContent($"Queue is finished! Why not keep the tunes going?")
                    .SendAsync(channel);
        }

        public static async Task AddToQueue(InteractionContext context, string url, bool forceNoPlay = false) {
            DiscordGuild guild = context.Guild;
            bool shouldPlayAfter = queues[guild].Count == 0;

            var youtube = new YoutubeClient();
            var res = await youtube.Videos.GetAsync(url);
            Video video = res;

            queues[guild].Add(video);
            if (shouldPlayAfter && !forceNoPlay) await PlayQueue(guild, context.Channel);
        }

        public static void RemoveFromQueue(DiscordGuild guild, int index) {
            queues[guild].RemoveAt(index);
        }

        public static void MoveQueueUp(DiscordGuild guild) {
            if (LoopQueue && LoopSong) {
                LoopSong = false;
                LoopQueue = false;
            }
            if (LoopQueue) {
                queues[guild].Add(queues[guild][0]);
            } else if (LoopSong) {
                return;
            }
            queues[guild].RemoveAt(0);
        }

        public static void ClearQueue(DiscordGuild guild) {
            queues[guild].Clear();
            if (voteSkips.ContainsKey(guild))
                voteSkips.Remove(guild);
            if (paused.Contains(guild))
                paused.Remove(guild);
            if (AudioManager.ctss.ContainsKey(guild))
                AudioManager.ctss.Remove(guild);

            LoopQueue = false;
            LoopSong = false;
        }

        public static List<Video> GetQueue(DiscordGuild guild) {
            return queues[guild];
        }

        public static List<List<Video>> GetPaginatedQueue(DiscordGuild guild) {
            List<Video> queue = queues[guild];
            List<List<Video>> paginated = new List<List<Video>>();
            int curPage = 1;

            for (int i = 1; i < queue.Count; i++) {
                if (paginated.Count < curPage) {
                    paginated.Add(new List<Video>());
                }

                paginated[curPage - 1].Add(queue[i]);
                if (i % 5  == 0) {
                    curPage++;
                }
            }

            return paginated;
        }
    }
}
