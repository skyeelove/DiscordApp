using Discord.Audio;
using FFMpegCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordApp.Services
{
    public class AudioStreamService
    {
        private Process? ffmpeg;

        private Process CreateStream(Song? song)
        {
            if (song == null)
            {
                return null;
            }
            var psi = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel info -nostdin -reconnect 1 -reconnect_streamed 1 -reconnect_delay_max 5 -i \"{song.Value.Link}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    Console.WriteLine("[FFMPEG] " + e.Data);
                }
            };
            process.Start();
            process.BeginErrorReadLine();

            return process;
        }

        public void KillFfmpeg()
        {
            if (ffmpeg == null)
            {
                Console.WriteLine("[INFO] ffmpeg is null.");
                return;
            }

            try
            {
                ffmpeg.Refresh();
                try
                {
                    ffmpeg.StandardOutput.BaseStream.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Unable to close stdout: {ex.Message}");
                }

                if (!ffmpeg.HasExited)
                {
                    ffmpeg.Kill();
                    Console.WriteLine("[INFO] ffmpeg was killed.");
                    ffmpeg.WaitForExit(2000);
                }

                ffmpeg.Dispose();
                ffmpeg = null;
                Console.WriteLine("[INFO] ffmpeg was ended.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Error while trying to kill ffmpeg: {ex.Message}");
            }
        }

        public async Task SendAsync(ulong guildId, IAudioClient client, Song? song)
        {
            ffmpeg = CreateStream(song);
            using var output = ffmpeg.StandardOutput.BaseStream;
            using var discord = client.CreatePCMStream(AudioApplication.Mixed);
            try
            {
                await output.CopyToAsync(discord);
            }
            catch
            {
                Console.WriteLine("[ERROR]Copying stream to discord was cancelled");
                //return;
            }
            finally
            {
                try
                {
                    await discord.FlushAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING]Error by FlushAsync: {ex.Message}");
                }
            }
        }
    }
}
