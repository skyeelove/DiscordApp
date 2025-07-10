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
        VoiceStateService stateService, MusicPlayerService musicPlayer, FfmpegProcessManager processManager) : ModuleBase<SocketCommandContext>
    {
        private readonly MusicQueueService _queue = queue;
        private readonly AudioStreamService _audioService = audioService;
        private readonly VoiceStateService _voiceState = stateService;
        private readonly MusicPlayerService _musicPlayer = musicPlayer;
        private readonly FfmpegProcessManager _processManager = processManager;

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
                    .WithTitle("You must be in a voice channel.")
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
                        .WithTitle($"Bot already in {currentBotsChannel.Name} you can try to use !stop")
                        .Build());
                    return;
                }
            }            
            
            if (_voiceState.GetClient(guildId) == null)
            {
                IAudioClient audioClient = await currentUserChannel.ConnectAsync(selfDeaf: true);
                _voiceState.SetClient(guildId, audioClient);
            }

            _queue.AddSong(guildId, await AudioDownloadService.GetAudioDataAsync(input));
            if (_voiceState.IsPlaying(guildId))
            {
                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.Orange)
                    .WithTitle($"Song was added to queue.")
                    .Build()
                    );
               return;
            }

            while(_queue.TryGetNextSong(guildId, out var song))
            {
                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.Orange)
                    .WithTitle($"Now playing: {song.Value.Title}")
                    .Build()
                    );
                await _musicPlayer.PlaybackLoop(guildId, song);
            }
            

            if (_queue.GetQueue(guildId).Count == 0)
            {
                _voiceState.SetPlaying(guildId, false);
                _voiceState.RemoveClient(guildId);                
                await currentUserChannel.DisconnectAsync();

                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.DarkRed)
                    .WithTitle($"There are no more tracks")
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
                .WithColor(Color.Orange)
                .Build()
            );
        }


        [Command("stop", RunMode = RunMode.Async)]
        public async Task StopMusic(IVoiceChannel? channel = null)
        {
            VoiceContext(out SocketVoiceChannel? currentBotsChannel, out IVoiceChannel? currentUserChannel);
            if (!await ValidateVoiceChannelAsync(currentBotsChannel, currentUserChannel))
            {
                return;
            }

            _queue.GetQueue(Context.Guild.Id).Clear();
            _processManager.Kill(Context.Guild.Id);
            await ReplyAsync("", false, new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle($"Bye bye..")
                .Build()
                );
            await currentBotsChannel.DisconnectAsync();
        }

        [Command("skip", RunMode = RunMode.Async)]
        public async Task SkipMusic()
        {
            VoiceContext(out SocketVoiceChannel? currentBotsChannel, out IVoiceChannel? currentUserChannel);
            if (!await ValidateVoiceChannelAsync(currentBotsChannel, currentUserChannel))
            {
                return;
            }
            _musicPlayer.Skip(Context.Guild.Id);
            _processManager.Kill(Context.Guild.Id);
            await ReplyAsync("", false, new EmbedBuilder()
                .WithColor(Color.Orange)
                .WithTitle($"Song was skipped")
                .Build()
                );
        }

        private async Task<bool> ValidateVoiceChannelAsync(SocketVoiceChannel? currentBotsChannel, IVoiceChannel? currentUserChannel)
        {
            if (currentUserChannel == null)
            {
                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.DarkRed)
                    .WithTitle("You must be in a voice channel.")
                    .Build());
                return false;
            }

            if (currentBotsChannel == null)
            {
                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.DarkRed)
                    .WithTitle("I'm not in the voice room")
                    .Build());
                return false;
            }

            if (currentBotsChannel.Id != currentUserChannel.Id)
            {
                await ReplyAsync("", false, new EmbedBuilder()
                    .WithColor(Color.DarkRed)
                    .WithTitle("You should be in the same room as the bot")
                    .Build());
                return false;
            }

            return true;
        }
    }
}
