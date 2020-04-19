using paxgamelib.Data;
using paxgamelib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;


namespace paxgamelib.Service
{
    public static class RESTService
    {
        internal static ILibData _data = LibData.Instance;


        public static int RandomAction(Player pl)
        {
            List<int> PossibleAImoves = Enumerable.Range(1, 3 * 20 * 50 + 5).ToList();
            foreach (int num in pl.GetAIMoves())
                PossibleAImoves.Remove(num);
            Random rand = new Random();
            int action = 0;
            int index = rand.Next(0, PossibleAImoves.Count);
            action = (PossibleAImoves[index]);
            return action;
        }

        public static int ActionToMove(int action)
        {
            string move = "";
            int num = 0;
            int minerals = 0;
            if (action == 3001)
            {
                move = "-1X20X50";
                minerals = 100;
            }
            else if (action == 3002)
            {
                move = "-1X21X50";
                minerals = 50;
            }
            else if (action == 3003)
            {
                move = "-1X22X50";
                minerals = 25;
            }
            else if (action == 3004)
            {
                move = "-1X23X50";
                minerals = 100;
            }
            else if (action == 3005)
            {
                move = "-1X24X50";
                minerals = 100;
            }
            else
            {
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 20; j++)
                        for (int k = 0; k < 50; k++)
                        {
                            if (action == num)
                            {
                                float unit = -1 + ((float)i * 0.1f);
                                move = unit + "X" + j + "X" + k;
                                if (i == 0)
                                    minerals = 50;
                                else if (i == 1)
                                    minerals = 95;
                                else if (i == 2)
                                    minerals = 65;
                            }
                            num++;
                        }
            }
            return minerals;
        }

        public static long NewRESTGame()
        {
            Player _player = new Player();
            _player.Pos = 1;
            _player.Race = UnitRace.Terran;
            _player.ID = paxgame.GetPlayerID();
            _player.Name = "Terran AI " + _player.ID;
            _player.Mode = new GameMode();

            Player _opp = new Player();
            _opp.Pos = 4;
            _opp.Race = UnitRace.Terran;
            _opp.ID = paxgame.GetPlayerID();
            _opp.Name = "Terran Bot " + _opp.ID;

            GameHistory game = new GameHistory();
            game.ID = paxgame.GetGameID();
            game.Players.Add(_player);
            game.Players.Add(_opp);
            _player.Game = game;
            _opp.Game = game;
            _player.Units = UnitPool.Units.Where(x => x.Race == _player.Race && x.Cost > 0).ToList();
            _opp.Units = UnitPool.Units.Where(x => x.Race == _opp.Race && x.Cost > 0).ToList();

            _data.SetGame(game);
            _data.SetPlayer(_player);
            _data.SetPlayer(_opp);
            return (long)game.ID;
        }
    }
}
