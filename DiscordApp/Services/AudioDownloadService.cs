using SpotifyAPI.Web;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;

public enum SearchMode
{
    YouTube,
    SoundCloud,
    Spotify,
}

namespace DiscordApp.Services
{
    public static class AudioDownloadService
    {
        public static async Task<Song?> GetAudioDataAsync(string queryOrUrl,SearchMode mode)
        {
            string title, url;
            string Arguments = $"--encoding utf-8 --quiet --no-warnings -f bestaudio --get-title --get-url";
            if (IsUrl(queryOrUrl))
            {
                switch (mode)
                {
                    case SearchMode.YouTube:
                    case SearchMode.SoundCloud:
                        Arguments += $" \"{queryOrUrl}\"";
                            break;
                    case SearchMode.Spotify:
                        {
                            var uri = new Uri(queryOrUrl);
                            var segments = uri.Segments;
                            if (segments.Length < 3 || segments[1] != "track/")
                                return null;

                            var trackId = segments[2].TrimEnd('/');

                            var config = SpotifyClientConfig.CreateDefault();
                            var request = new ClientCredentialsRequest($"{Environment.GetEnvironmentVariable("clientId")}", $"{Environment.GetEnvironmentVariable("clientSecret")}");
                            var response = await new OAuthClient(config).RequestToken(request);

                            var spotify = new SpotifyClient(config.WithToken(response.AccessToken));

                            var track = await spotify.Tracks.Get(trackId);
                            var localTitle = $"{track.Name} — {string.Join(", ", track.Artists.Select(a => a.Name))}";
                            Arguments += $" \"spsearch:{localTitle}\"";
                            break;
                        }
                    default:
                        return null;
                }
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

            using Process process = new Process { StartInfo = psi, EnableRaisingEvents = true };
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

            title = output.Count > 0 ? output[0] : "Unknown title";
            url = output.Count > 1 ? output[1] : "";
            Logger.Debug($"yt-dlp request result: Title: {title}, URL: {url}");
            return new Song(title, url);
        }

        private static bool IsUrl(string input)
        {
            return Uri.TryCreate(input, UriKind.Absolute, out Uri uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }
    }
}
