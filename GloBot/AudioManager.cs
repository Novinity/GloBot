using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System;

namespace DiscordBotTest {
    internal class AudioManager {
        public static Dictionary<DiscordGuild, CancellationTokenSource> ctss = new Dictionary<DiscordGuild, CancellationTokenSource>();

        public static async Task PlaySong(string query, VoiceNextConnection connection) {
            // Windows audio playing

            /*if (!File.Exists("yt-dlp.exe"))
                await YoutubeDLSharp.Utils.DownloadYtDlp();
            if (!File.Exists("ffmpeg.exe"))
                await YoutubeDLSharp.Utils.DownloadFFmpeg();
            var ffmpeg = Process.Start(new ProcessStartInfo {
                FileName = "cmd.exe",
                Arguments = $"/C yt-dlp.exe --default-search ytsearch -o - \"{query}\" | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });*/


            // Linux audio playing

            var ffmpeg = Process.Start(new ProcessStartInfo {
                FileName = "/bin/bash",
                Arguments = $"-c \" yt-dlp --default-search ytsearch -o - \"{query}\" | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1 \"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            });

            Stream stream = ffmpeg.StandardOutput.BaseStream;

            VoiceTransmitSink transmit = connection.GetTransmitSink();

            CancellationTokenSource cancellation = new CancellationTokenSource();
            if (ctss.ContainsKey(connection.TargetChannel.Guild)) {
                ctss[connection.TargetChannel.Guild].Dispose();
                ctss.Remove(connection.TargetChannel.Guild);
            }
            ctss.Add(connection.TargetChannel.Guild, cancellation);

            try {
                await stream.CopyToAsync(transmit, null, cancellation.Token);
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
            stream.Dispose();
        }

        public static void CancelStream(DiscordGuild guild) {
            ctss[guild].Cancel();
        }
    }
}
