using System.Collections.Concurrent;

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
            if (!_queues.ContainsKey(guildId) || _queues[guildId].Count == 0)
            {
                Logger.Error($"Queue for guild {guildId} is empty or does not exist.");
                return;
            }
            _queues[guildId].Dequeue();
        }

        public void AddSong(ulong guildId, Song? song)
        {
            if(song == null)
            {
                return;
            }

            if (!_queues.ContainsKey(guildId))
            {
                _queues[guildId] = new Queue<Song>();
            }

            _queues[guildId].Enqueue(song.Value);
            Logger.Info($"Added song '{song.Value.Title}' to queue for guild {guildId}.");
        }

        public Queue<Song> GetQueue(ulong guildId)
        {
            if (!_queues.ContainsKey(guildId))
            {
                _queues[guildId] = new Queue<Song>();
            }
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
            else
            {
                song = null;
                return false;
            }
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
            Logger.Warning($"No songs found in queue for guild {guildId}.");
            return ["No Elements in queue"];
        }
    }
}
