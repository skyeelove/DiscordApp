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
    public class MusicModule(DiscordSocketClient client, AudioStreamService audioService, MusicQueueService queue) : ModuleBase<SocketCommandContext>
    {
        private readonly DiscordSocketClient _client = client;
        private readonly MusicQueueService _queue = queue;
        private readonly AudioStreamService _audioService = audioService;


        [Command("embed")]
        [Summary("Here will be list of found songs")]
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
        public async Task PlayMusic(string link)
        {            
            var botUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            var currentBotsChannel = botUser?.VoiceChannel;
            var currentUserChannel = (Context.User as IGuildUser)?.VoiceChannel;

            if (currentUserChannel == null)
            {
                await ReplyAsync("You must be in a voice channel.");
                return;
            }

            if (currentBotsChannel != null)
            {
                if (currentBotsChannel.Id != currentUserChannel.Id)
                {
                    await ReplyAsync($"Bot already in {currentBotsChannel.Name} you can try to use !stop");
                    return;
                }
                _queue.AddSong(Context.Guild.Id, new Song("idk how to catch name", link));
                await ReplyAsync("Added to queue.");
                return;
            }
            _queue.AddSong(Context.Guild.Id, new Song("idk how to catch name", link));
            var audioClient = await currentUserChannel.ConnectAsync();
            await _audioService.SendAsync(Context.Guild.Id, audioClient, queue: _queue);
            if (_queue.GetQueue(Context.Guild.Id).Count == 0)
            {
                await currentUserChannel.DisconnectAsync();
            }
        }



        [Command("stop", RunMode = RunMode.Async)]
        public async Task StopMusic(IVoiceChannel? channel = null)
        {
            channel ??= Context.Guild.GetUser(Context.Client.CurrentUser.Id).VoiceChannel;
            if (channel == null)
            {
                await ReplyAsync("I'm not in the voice room");
                return;
            }
            _audioService.KillFfmpeg();
            await ReplyAsync("Bye bye..");
            await channel.DisconnectAsync();
        }
    }
}
