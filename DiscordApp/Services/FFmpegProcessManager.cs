using FFMpegCore;
using System.Diagnostics;

public class FfmpegProcessManager
{
    private readonly Dictionary<ulong, Process> _processes = new();

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
            }
        }
    }
}