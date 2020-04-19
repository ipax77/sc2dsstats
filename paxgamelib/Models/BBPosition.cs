using System;
using System.Collections.Generic;
using System.Text;
using paxgamelib;
using System.Linq;
using System.Numerics;

namespace paxgamelib.Models
{
    class BBPosition
    {

        public int[][] Pos { get; set; }

        public BBPosition()
        {
            Pos = new int[(int)paxgame.Buildareasize.X][];
            for (int i = 0; i < (int)paxgame.Buildareasize.Y; i++)
                Pos[i] = new int[(int)paxgame.Buildareasize.Y];
        }

        public BBPosition(Player pl) : this()
        {
            foreach (Unit unit in pl.Units.Where(x => x.Status != UnitStatuses.Available)) {

            }
        }

        public Vector2 PlaceUnit(int x, int y, float size)
        {
            Vector2 vec = Vector2.Zero;



            return vec;
        }
    }
}
