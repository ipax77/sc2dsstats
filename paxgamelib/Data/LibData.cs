using paxgamelib.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace paxgamelib.Data
{
    public class LibData : ILibData
    {
        private ConcurrentBag<Player> Players = new ConcurrentBag<Player>();
        private ConcurrentBag<GameHistory> Games = new ConcurrentBag<GameHistory>();

        private LibData()
        {

        }

        public GameHistory GetGame(ulong id)
        {
            return Games.SingleOrDefault(x => x.ID == id);
        }

        public void SetGame(GameHistory game)
        {
            Games.Add(game);
        }

        public Player GetPlayer(ulong id)
        {
            return Players.SingleOrDefault(x => x.ID == id);
        }

        public void SetPlayer(Player player)
        {
            Players.Add(player);
        }

        public void Reset()
        {
            Players.Clear();
            Games.Clear();
        }

        public void Delete(ulong id)
        {
            Player pl = GetPlayer(id);
            if (pl != null)
                Players.TryTake(out pl);
        }

        private static Lazy<LibData> instance = new Lazy<LibData>(() => new LibData());

        public static LibData Instance => instance.Value;
    }
}
