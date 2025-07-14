using DiscordApp.Services;
using System.Diagnostics;

public class FfmpegProcessManager
{
    private readonly Dictionary<ulong, Process> _processes = [];

    public void Add(ulong guildId, Process process)
    {
        lock (_processes)
            _processes[guildId] = process;
    }

    public void Kill(ulong guildId)
    {
        lock (_processes)
        {
            if (_processes.TryGetValue(guildId, out var process))
            {
                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch { }
                process.Dispose();
                _processes.Remove(guildId);
                Logger.Debug("Ffmpeg ended playing music");
            }
        }
    }

    public Process? CreateStream(Song? song)
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
        Logger.Debug("Ffmpeg started playing music");

        return process;
    }
}