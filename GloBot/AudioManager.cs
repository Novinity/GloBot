using DSharpPlus.Entities;
using DSharpPlus.VoiceNext;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System;
using System.Runtime.InteropServices;

namespace GloBot {
    internal class AudioManager {
        public static Dictionary<DiscordGuild, CancellationTokenSource> ctss = new Dictionary<DiscordGuild, CancellationTokenSource>();

        public static async Task PlaySong(string query, VoiceNextConnection connection) {

            if (!File.Exists("yt-dlp.exe"))
                await YoutubeDLSharp.Utils.DownloadYtDlp();
            if (!File.Exists("ffmpeg.exe"))
                await YoutubeDLSharp.Utils.DownloadFFmpeg();

            Process ffmpeg = null;

            // Windows audio playing
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                ffmpeg = Process.Start(new ProcessStartInfo {
                    FileName = "cmd.exe",
                    Arguments = $"/C yt-dlp.exe --default-search ytsearch -o - \"{query}\" | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            } else if (RuntimeInformation.IsOSPlatform (OSPlatform.Linux)) {
                // Linux audio playing
                ffmpeg = Process.Start(new ProcessStartInfo {
                    FileName = "/bin/bash",
                    Arguments = $"-c \" yt-dlp --default-search ytsearch -o - \"{query}\" | ffmpeg -i pipe:0 -ac 2 -f s16le -ar 48000 pipe:1 \"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                });
            }

            if (ffmpeg == null) {
                Debug.WriteLine("FFMPEG WAS NULL! AAAAA");
                return;
            }

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
            if (!ctss.ContainsKey(guild)) return;
            ctss[guild].Cancel();
        }
    }
}
