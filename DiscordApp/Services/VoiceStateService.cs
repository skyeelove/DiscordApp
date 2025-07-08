using Discord.Audio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordApp.Services
{
    public class VoiceStateService
    {
        private readonly ConcurrentDictionary<ulong, IAudioClient> _connectedClients = new();
        private readonly ConcurrentDictionary<ulong, bool> _isPlaying = new();

        public bool IsPlaying(ulong guildId) =>
                    _isPlaying.TryGetValue(guildId, out var val) && val;

        public void SetPlaying(ulong guildId, bool value)
        {
            _isPlaying[guildId] = value;
        }

        public void SetClient(ulong guildId, IAudioClient client)
        {
            _connectedClients[guildId] = client;
        }

        public IAudioClient? GetClient(ulong guildId)
        {
            return _connectedClients.TryGetValue(guildId, out var client) ? client : null;
        }

        public void RemoveClient(ulong guildId)
        {
            _connectedClients.TryRemove(guildId, out _);
        }
    }
}
