using Discord.Audio;
using System.Diagnostics;


namespace DiscordApp.Services
{
    public class AudioStreamService(FfmpegProcessManager processManager)
    {
        private readonly FfmpegProcessManager _processManager = processManager;
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

        public async Task SendAsync(ulong guildId, IAudioClient client, Song? song)
        {
            using var ffmpeg = CreateStream(song);
            _processManager.Add(guildId, ffmpeg);
            using var output = ffmpeg.StandardOutput.BaseStream;
            using var discord = client.CreatePCMStream(AudioApplication.Mixed);
            try
            {
                await output.CopyToAsync(discord);
            }
            catch
            {
                Console.WriteLine("[ERROR]Copying stream to discord was cancelled");
                ffmpeg.Kill();
            }
            finally
            {
                try
                {
                    await discord.FlushAsync();
                    _processManager.Kill(guildId);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING]Error by FlushAsync: {ex.Message}");
                }
            }
        }

        //public void Dispose()
        //{
        //    ffmpeg.Dispose();
        //    ffmpeg.Kill();
        //    ffmpeg = null;
        //}
    }
}
