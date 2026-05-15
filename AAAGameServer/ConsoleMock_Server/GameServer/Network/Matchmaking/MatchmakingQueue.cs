/*
* ┌──────────────────────────────────┐
* │  描    述: PvP 匹配队列
* │  类    名: MatchmakingQueue.cs
* │  创    建: By qiqizizzz
* └──────────────────────────────────┘
*/

using System.Collections.Generic;

namespace Network.Matchmaking
{
    internal class MatchmakingQueue
    {
        private readonly List<string> _queue = new();

        public bool CanMatch => _queue.Count >= 2;

        public void Enqueue(string username)
        {
            if (string.IsNullOrEmpty(username)) return;
            if (_queue.Contains(username)) return;
            _queue.Add(username);
        }

        public void Dequeue(string username)
        {
            _queue.Remove(username);
        }

        public (string p1, string p2) PopMatch()
        {
            var p1 = _queue[0];
            var p2 = _queue[1];
            _queue.RemoveRange(0, 2);
            return (p1, p2);
        }

        public int Count => _queue.Count;
    }
}
