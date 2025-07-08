using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordApp.Services
{
    public struct Song
    {
        public string Title { get; set; }
        public string Link { get; set; }

        public Song(string title, string link)
        {
            Title = title; Link = link;
        }
    }

    public class MusicQueueService
    {
        private readonly ConcurrentDictionary<ulong, Queue<Song>> _queues = new();    

        public void AddSong(ulong guildId, Song song)
        {
            if (!_queues.ContainsKey(guildId))
                _queues[guildId] = new Queue<Song>();

            _queues[guildId].Enqueue(song);
            Console.WriteLine($"Size: {_queues[guildId].Count}");
        }

        public Queue<Song> GetQueue(ulong guildId)
        {
            if (!_queues.ContainsKey(guildId))
                _queues[guildId] = new Queue<Song>();

            return _queues[guildId];
        }

        public int GetSize(ulong guildId)
        {
            return _queues[guildId].Count;
        }

        public bool TryGetNextSong(ulong guildId, out Song? song)
        {
            if (_queues.TryGetValue(guildId, out var queue) && queue.Count > 0)
            {
                song = queue.Dequeue();
                return true;
            }

            song = null;
            return false;
        }

        public Song GetNext(ulong guildId) => _queues[guildId].Dequeue();
    }

}
