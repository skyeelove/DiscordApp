using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using DiscordApp.Services;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DiscordApp.Modules
{
    public class MusicModule(DiscordSocketClient client) : ModuleBase<SocketCommandContext>
    {
        private static bool isPlaying;
        private readonly DiscordSocketClient _client = client;

        [Command("embed")]
        [Summary("Display embed window")]
        public async Task DisplayEmbed()
        {
            await ReplyAsync("", false,
                new EmbedBuilder()
                    .WithDescription("Results:")
                    .WithAuthor(_client.CurrentUser)
                    .WithTimestamp(DateTimeOffset.Now)
                    .AddField("Field 1", "Value 1")
                    .AddField("Field 2", "Value 2")
                    .AddField("Field 3", "Value 3")
                    .WithColor(Color.DarkBlue)
                    .Build()
                );
        }

        // The command's Run Mode MUST be set to RunMode.Async, otherwise, being connected to a voice channel will block the gateway thread.
        [Command("play", RunMode = RunMode.Async)]
        public async Task PlayMusic()
        {
            if (isPlaying)
            {
                await ReplyAsync("Something playing already.");
                return;
            }

            var botUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var currentBotsChannel = botUser?.VoiceChannel;
            var currentUserChannel = (Context.User as IGuildUser)?.VoiceChannel;

            if (currentUserChannel == null)
            {
                await ReplyAsync("You must be in a voice channel.");
                return;
            }

            if (currentBotsChannel != null && currentBotsChannel.Id != currentUserChannel.Id)
            {
                await ReplyAsync($"Bot already in another voice channel: {currentBotsChannel.Name}");
                return;
            }
            try
            {
                isPlaying = true;
                var audioClient = await currentUserChannel.ConnectAsync();
                var audioService = new AudioService();
                await audioService.SendAsync(audioClient, "C:\\Users\\skyfalling\\Downloads\\Telegram Desktop\\song1.mp3");
            }
            finally
            {
                isPlaying = false;
            }
        }


        [Command("stop", RunMode = RunMode.Async)]
        public async Task StopMusic(IVoiceChannel? channel = null)
        {
            isPlaying = false;
            try
            {
                channel = channel ?? Context.Guild.GetUser(Context.Client.CurrentUser.Id).VoiceChannel;
                await channel.DisconnectAsync();
            }
            catch
            {
                ReplyAsync("I'm not in the voice room");
            }

        }
    }
}
