using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordBotTest {
    internal class AudioManager {
        public static Dictionary<DiscordGuild, CancellationTokenSource> ctss = new Dictionary<DiscordGuild, CancellationTokenSource>();

        public static async Task PlaySong(string query, VoiceNextConnection connection) {


            var ffmpeg = Process.Start(new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = $"/C yt-dlp.exe --default-search ytsearch -o - \"{query}\" | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            Stream pcm = ffmpeg.StandardOutput.BaseStream;

            VoiceTransmitSink transmit = connection.GetTransmitSink();

            CancellationTokenSource cancellation = new CancellationTokenSource();
            if (ctss.ContainsKey(connection.TargetChannel.Guild)) {
                ctss[connection.TargetChannel.Guild].Dispose();
                ctss.Remove(connection.TargetChannel.Guild);
            }
            ctss.Add(connection.TargetChannel.Guild, cancellation);

            try {
                await pcm.CopyToAsync(transmit, null, cancellation.Token);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            pcm.Dispose();
        }

        public static void CancelStream(DiscordGuild guild) {
            ctss[guild].Cancel();
        }
    }
}
