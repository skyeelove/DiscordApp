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
    public class MusicModule(AudioStreamService audioService, MusicQueueService queue) : ModuleBase<SocketCommandContext>
    {
        private readonly MusicQueueService _queue = queue;
        private readonly AudioStreamService _audioService = audioService;

        // The command's Run Mode MUST be set to RunMode.Async, otherwise, being connected to a voice channel will block the gateway thread.
        [Command("play", RunMode = RunMode.Async)]
        [Summary("Play track/Add track to queue")]
        public async Task PlayMusic(string link)
        {
            VoiceContext(out SocketVoiceChannel? currentBotsChannel, out IVoiceChannel? currentUserChannel);

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
                _queue.AddSong(Context.Guild.Id, await AudioStreamService.GetAudioDataAsync(link));
                await ReplyAsync("Added to queue.");
                return;
            }

            var audioClient = await currentUserChannel.ConnectAsync();
            _queue.AddSong(Context.Guild.Id, await AudioStreamService.GetAudioDataAsync(link));
            while (queue.TryGetNextSong(Context.Guild.Id, out var song))
            {
                await ReplyAsync($"Now playing: {song.Value.Title}");
                await _audioService.SendAsync(Context.Guild.Id, audioClient, song: song);
            }

            if (_queue.GetQueue(Context.Guild.Id).Count == 0)
            {
                await ReplyAsync("There are no more tracks");
                await currentUserChannel.DisconnectAsync();
            }
        }

        [Command("queue")]
        [Summary("Display queue using embed")]
        public async Task DisplayQueue()
        {
            var embed = new EmbedBuilder()
                .WithTimestamp(DateTimeOffset.Now)
                .WithColor(Color.DarkBlue);

            var fields = _queue.GetAllTitles(Context.Guild.Id);

            foreach (var field in fields)
            {
                embed.AddField(field, "\u200B", inline: false);
            }

            await ReplyAsync(" ", false,
                embed.Build()
            );
        }


        [Command("stop", RunMode = RunMode.Async)]
        public async Task StopMusic(IVoiceChannel? channel = null)
        {
            VoiceContext(out SocketVoiceChannel? currentBotsChannel, out IVoiceChannel? currentUserChannel);
            if (currentBotsChannel == null)
            {
                await ReplyAsync("I'm not in the voice room");
                return;
            }

            if (currentBotsChannel.Id != currentUserChannel.Id)
            {
                await ReplyAsync($"You should be in the same room as bot");
                return;
            }

            _queue.GetQueue(Context.Guild.Id).Clear();
            _audioService.KillFfmpeg();
            await ReplyAsync("Bye bye..");
            await currentBotsChannel.DisconnectAsync();
        }

        private void VoiceContext(out SocketVoiceChannel? currentBotsChannel, out IVoiceChannel? currentUserChannel)
        {
            var botUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            currentBotsChannel = botUser.VoiceChannel;
            currentUserChannel = (Context.User as IGuildUser)?.VoiceChannel;
        }
    }
}
