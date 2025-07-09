using Discord.Audio;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DiscordApp.Services
{
    public struct Song(string title, string link)
    {
        public string Title { get; set; } = title; 
        public string Link { get; set; } = link;
    }

    public class MusicQueueService
    {
        private readonly ConcurrentDictionary<ulong, Queue<Song>> _queues = new();

        public void RemoveCurrent(ulong guildId)
        {
            _queues[guildId].Dequeue();
        }

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
                song = queue.Peek();
                return true;
            }

            song = null;
            return false;
        }

        public List<string> GetAllTitles(ulong guildId)
        {
            var result = new List<string>();
            if (_queues.TryGetValue(guildId, out Queue<Song>? value))
            {
                foreach (var item in value)
                {
                    result.Add(item.Title);
                }
                if (result.Count > 0)
                {
                    return result;
                }
            }
            return ["No Elements in queue"];
        }
    }
}
