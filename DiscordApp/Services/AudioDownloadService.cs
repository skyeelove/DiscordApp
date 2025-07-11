﻿using System.Diagnostics;
using System.Text;

namespace DiscordApp.Services
{
    public static class AudioDownloadService
    {
        public static async Task<Song?> GetAudioDataAsync(string queryOrUrl)
        {
            string Arguments = string.Empty;
            if (IsValidUrl(queryOrUrl) == false)
            {
                Arguments = $"--encoding utf-8 --quiet --no-warnings -f bestaudio --get-title --get-url ytsearch1:\"{queryOrUrl}\"";
            }
            else
            {
               Arguments = $"--encoding utf-8 --quiet --no-warnings -f bestaudio --get-title --get-url \"{queryOrUrl}\"";
            }

            var psi = new ProcessStartInfo
            {
                FileName = "tools/yt-dlp",
                Arguments = Arguments,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                StandardOutputEncoding = Encoding.UTF8,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var output = new List<string>();

            var tcs = new TaskCompletionSource<bool>();

            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data == null)
                    tcs.TrySetResult(true);
                else
                    output.Add(e.Data);
            };

            process.Start();
            process.BeginOutputReadLine();

            await tcs.Task;
            await process.WaitForExitAsync();

            string title = output.Count > 0 ? output[0] : "Unknown title";
            string url = output.Count > 1 ? output[1] : "";
            //Console.WriteLine($"{title} - {url}");

            return new Song(title, url);
        }

        private static bool IsValidUrl(string input)
        {
            return Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
