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
    public class MusicModule(AudioStreamService audioService, MusicQueueService queue, 
        VoiceStateService stateService) : ModuleBase<SocketCommandContext>
    {
        private readonly MusicQueueService _queue = queue;
        private readonly AudioStreamService _audioService = audioService;
        private readonly VoiceStateService _voiceState = stateService;

        // The command's Run Mode MUST be set to RunMode.Async, otherwise, being connected to a voice channel will block the gateway thread.
        [Command("play", RunMode = RunMode.Async)]
        [Summary("Play track/Add track to queue")]
        public async Task PlayMusic(string link)
        {
            VoiceContext(out SocketVoiceChannel? currentBotsChannel, out IVoiceChannel? currentUserChannel);
            var guildId = Context.Guild.Id; 
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
            }

            if (_voiceState.GetClient(guildId) == null)
            {
                var audioClient = await currentUserChannel.ConnectAsync(selfDeaf: true);
                _voiceState.SetClient(guildId, audioClient);
            }

            _queue.AddSong(guildId, await AudioStreamService.GetAudioDataAsync(link));
            if (_voiceState.IsPlaying(guildId))
            {            
                await ReplyAsync("Added to queue.");
                return;
            }

            while (queue.TryGetNextSong(guildId, out var song))
            {
                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.DarkOrange)
                    .WithDescription($"Now playing: {song.Value.Title}")
                    .Build()
                    );
                await _audioService.SendAsync(guildId,
                    _voiceState.GetClient(Context.Guild.Id), song: song);
            }

            if (_queue.GetQueue(guildId).Count == 0)
            {
                _voiceState.RemoveClient(guildId);
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

            for(int i = 0; i < fields.Count; i++)
            {
                embed.WithDescription($". {fields[i]}");
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
                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.DarkRed)
                    .WithDescription("​I'm not in the voice room")
                    .Build() 
                    );

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
