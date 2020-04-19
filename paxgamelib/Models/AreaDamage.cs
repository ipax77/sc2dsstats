using paxgamelib.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace paxgamelib.Models
{
    [Serializable]
    public class AreaDamage
    {
        public float Distance1 { get; set; } = 0.4687f / paxgame.Battlefieldmodifier;
        public float Distance2 { get; set; } = 0.7812f / paxgame.Battlefieldmodifier;
        public float Distance3 { get; set; } = 1.25f / paxgame.Battlefieldmodifier;
        public bool FriendlyFire = false;

        public object Shallowcopy()
        {
            return this.MemberwiseClone();
        }
    }
}
