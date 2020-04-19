using paxgamelib.Models;

namespace paxgamelib.Data
{
    public interface ILibData
    {
        GameHistory GetGame(ulong id);
        Player GetPlayer(ulong id);
        void Reset();
        void SetGame(GameHistory game);
        void SetPlayer(Player player);
        void Delete(ulong id);
    }
}