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
        VoiceStateService stateService, MusicPlayerService musicPlayer) : ModuleBase<SocketCommandContext>
    {
        private readonly MusicQueueService _queue = queue;
        private readonly AudioStreamService _audioService = audioService;
        private readonly VoiceStateService _voiceState = stateService;
        private readonly MusicPlayerService _musicPlayer = musicPlayer;

        private void VoiceContext(out SocketVoiceChannel? currentBotsChannel, out IVoiceChannel? currentUserChannel)
        {
            var botUser = Context.Guild.GetUser(Context.Client.CurrentUser.Id);
            currentBotsChannel = botUser.VoiceChannel;
            currentUserChannel = (Context.User as IGuildUser)?.VoiceChannel;
        }

        // The command's Run Mode MUST be set to RunMode.Async, otherwise, being connected to a voice channel will block the gateway thread.
        [Command("play", RunMode = RunMode.Async)]
        [Summary("Play track/Add track to queue")]
        public async Task PlayMusic([Remainder] string input)
        {
            Console.WriteLine($"{input}");
            VoiceContext(out SocketVoiceChannel? currentBotsChannel, out IVoiceChannel? currentUserChannel);
            var guildId = Context.Guild.Id; 
            if (currentUserChannel == null)
            {
                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.DarkRed)
                    .WithDescription("You must be in a voice channel.")
                    .Build()
                    );
                return;
            }

            if (currentBotsChannel != null)
            {
                if (currentBotsChannel.Id != currentUserChannel.Id)
                {
                    await ReplyAsync("", false, new EmbedBuilder()
                        .WithColor(Color.DarkRed)
                        .WithDescription($"Bot already in {currentBotsChannel.Name} you can try to use !stop")
                        .Build());
                    return;
                }
            }

            if (_voiceState.GetClient(guildId) == null)
            {
                var audioClient = await currentUserChannel.ConnectAsync(selfDeaf: true);
                _voiceState.SetClient(guildId, audioClient);
            }

            _queue.AddSong(guildId, await AudioDownloadService.GetAudioDataAsync(input));
            if (_voiceState.IsPlaying(guildId))
            {
                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.Orange)
                    .WithDescription($"Song was added to queue.")
                    .Build()
                    );
                return;
            }

            while(_queue.TryGetNextSong(guildId, out var song))
            {
                await _musicPlayer.PlaybackLoop(guildId, song);
            }
            

            if (_queue.GetQueue(guildId).Count == 0)
            {
                _voiceState.SetPlaying(guildId, false);
                _voiceState.RemoveClient(guildId);                
                await currentUserChannel.DisconnectAsync();

                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.DarkRed)
                    .WithDescription($"There are no more tracks")
                    .Build()
                    );

            }
        }

        [Command("queue")]
        [Summary("Display queue using embed")]
        public async Task DisplayQueue()
        {
            var embed = new EmbedBuilder();
            var fields = _queue.GetAllTitles(Context.Guild.Id);
            for(int i = 0; i < fields.Count; i++)
            {
                embed.AddField($"{i+1}. {fields[i]}", "ㅤ", false);
            }

            await ReplyAsync("ㅤ", false,
                embed
                .WithColor(Color.DarkOrange)
                .Build()
            );
        }


        [Command("stop", RunMode = RunMode.Async)]
        public async Task StopMusic(IVoiceChannel? channel = null)
        {
            VoiceContext(out SocketVoiceChannel? currentBotsChannel, out IVoiceChannel? currentUserChannel);

            if (currentUserChannel == null)
            {
                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.DarkRed)
                    .WithDescription("You must be in a voice channel.")
                    .Build()
                    );
                return;
            }

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
                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.DarkRed)
                    .WithDescription($"You should be in the same room as the bot")
                    .Build()
                    );
                return;
            }

            _queue.GetQueue(Context.Guild.Id).Clear();
            _audioService.KillFfmpeg();
            await ReplyAsync("", false, new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription($"Bye bye..")
                .Build()
                );
            await currentBotsChannel.DisconnectAsync();
        }

        [Command("skip", RunMode = RunMode.Async)]
        public async Task SkipMusic()
        {
            _musicPlayer.Skip(Context.Guild.Id);
            await ReplyAsync("", false, new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithDescription($"Song was skipped")
                .Build()
                );
        }
    }
}
