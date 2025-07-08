using Discord.Audio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordApp.Services
{
    public class AudioService
    {
        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }

        public async Task SendAsync(IAudioClient client, string path)
        {
            //if (!File.Exists(path))
            //{
            //    Console.WriteLine("[WARNING] Path not found.");
            //    return;
            //}
            using var ffmpeg = CreateStream(path);
            using var output = ffmpeg.StandardOutput.BaseStream;
            using var discord = client.CreatePCMStream(AudioApplication.Mixed);

            try
            {
                await output.CopyToAsync(discord);
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

                try
                {
                    var exited = ffmpeg.WaitForExit(5000);
                    if (!exited)
                    {
                        ffmpeg.Kill(entireProcessTree: true);
                        Console.WriteLine("[WARNING] ffmpeg was killed manually.");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[WARNING]Error  ffmpeg: " + ex.Message);
                }
            }
        }


    }
}
