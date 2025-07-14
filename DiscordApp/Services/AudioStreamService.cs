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
                Arguments = $"-hide_banner -allowed_extensions ALL -extension_picky 0 " +
                  "-protocol_whitelist file,http,https,tcp,tls,crypto " +
                  "-user_agent \"Mozilla/5.0\" " +
                  $"-i \"{song.Value.Link}\" -ac 2 -ar 48000 -f s16le pipe:1",

                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
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
            Logger.Debug("ffmpeg started playing music");
            try
            {
                await output.CopyToAsync(discord);
            }
            catch
            {
                Logger.Error("Copying stream to discord was cancelled");
                ffmpeg.Kill();
            }
            finally
            {
                try
                {
                    await discord.FlushAsync();
                    _processManager.Kill(guildId);
                    Logger.Debug("ffmpeg ended playing music");
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error by FlushAsync: {ex.Message}");
                }
            }
        }
    }
}
