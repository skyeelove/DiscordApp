using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordApp.Services;
using Microsoft.Extensions.DependencyInjection;

class Program
{
    static async Task Main(string[] args)
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent
        };

        var _client = new DiscordSocketClient(config);
        var services = new ServiceCollection()
            .AddSingleton(_client)
            .AddSingleton<CommandHandler>()
            .AddSingleton<CommandService>()
            .AddSingleton<MusicQueueService>()
            .AddSingleton<VoiceStateService>()
            .AddSingleton<MusicPlayerService>()
            .AddSingleton<FfmpegProcessManager>()
            .AddTransient<AudioStreamService>()
            .BuildServiceProvider();

        var handler = services.GetRequiredService<CommandHandler>();
        await handler.InstallCommandsAsync();

        await _client.LoginAsync(TokenType.Bot, Environment.GetEnvironmentVariable("token"));
        await _client.StartAsync();
        _client.Ready += async () => await OnReady(_client);

        await Task.Delay(-1);
    }

    private static async Task OnReady(DiscordSocketClient client)
    {
        var logDir = Path.Combine("logs");
        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir!);
        }
        Logger.Info("Bot started life cycle.");
    }
}

