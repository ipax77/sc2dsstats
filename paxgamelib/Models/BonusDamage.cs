using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace paxgamelib.Models
{
    [Serializable]
    public class BonusDamage
    {
        private float _Damage;
        public UnitAttributes Attribute { get; set; }
        public UnitUpgrades Type { get; set; } 
        public float Damage { get { return GetDamage(Ownerplayer); } set { _Damage = value; } }
        public float Modifier { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        public Player Ownerplayer { get; set; }

        public float GetDamage(Player pl)
        {
            if (pl != null && pl.Upgrades.Count() > 0)
            {
                UnitUpgrade upgrade = pl.Upgrades.Where(x => x.Upgrade == Type).FirstOrDefault();
                if (upgrade != null)
                    return _Damage + (Modifier * upgrade.Level);
            }
            return _Damage;
        }

        public BonusDamage Deepcopy()
        {
            BonusDamage bd = new BonusDamage();
            bd._Damage = _Damage;
            bd.Attribute = Attribute;
            bd.Type = Type;
            bd.Modifier = Modifier;
            bd.Ownerplayer = Ownerplayer; // no deepcopy
            return bd;
        }
    }
}
