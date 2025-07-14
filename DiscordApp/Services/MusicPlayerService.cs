using Discord;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordApp.Services
{
    public class MusicPlayerService(
            MusicQueueService queue,
            VoiceStateService voiceState,
            AudioStreamService audioService)
    {
        private readonly MusicQueueService _queue = queue;
        private readonly VoiceStateService _voiceState = voiceState;
        private readonly AudioStreamService _audioService = audioService;

        public void Skip(ulong guildId)
        {
            _queue.RemoveCurrent(guildId);
            _voiceState.SetSkippedState(guildId, true);
        }

        public async Task PlaybackLoop(ulong guildId, Song? song)
        {
            if (song == null)
            {
                return;
            }
            _voiceState.SetPlaying(guildId, true);

            await _audioService.SendAsync(
                guildId: guildId,
                client: _voiceState.GetClient(guildId),
                song: song
                );

            if (_voiceState.GetSkippedState(guildId) == true)
            {
                _voiceState.SetSkippedState(guildId, false);
            }
            else
            {
                _queue.RemoveCurrent(guildId);
            }
        }
    }

}
