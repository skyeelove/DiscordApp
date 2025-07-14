using Discord.Audio;
using System.Diagnostics;

namespace DiscordApp.Services
{
    public class AudioStreamService(FfmpegProcessManager processManager)
    {
        private readonly FfmpegProcessManager _processManager = processManager;

        public async Task SendAsync(ulong guildId, IAudioClient client, Song? song)
        {
            using var ffmpeg = _processManager.CreateStream(song);
            _processManager.Add(guildId, ffmpeg);
            using var output = ffmpeg.StandardOutput.BaseStream;
            using var discord = client.CreatePCMStream(AudioApplication.Mixed);
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
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error by FlushAsync: {ex.Message}");
                }
            }
        }
    }
}