using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using paxgamelib.Data;

namespace paxgamelib.Models
{
    [Serializable]
    public class UnitBase
    {
        public string Name { get; set; }
        public int Kills { get; set; } = 0;
        public float DamageDone { get; set; } = 0;
        public float MineralValueKilled { get; set; } = 0;
        public Vector2Ser SerPos { get; set; }

        public UnitBase GetBase()
        {
            UnitBase unit = new UnitBase();
            unit.Name = Name;
            unit.Kills = Kills;
            unit.DamageDone = DamageDone;
            unit.MineralValueKilled = MineralValueKilled;
            unit.SerPos = new Vector2Ser();
            unit.SerPos.x = SerPos.x;
            unit.SerPos.y = SerPos.y;
            return unit;
        }
    }

    [Serializable]
    public class Vector2Ser
    {
        public float x { get; set; }
        public float y { get; set; }
    }

    [Serializable]
    public class Unit : UnitBase, IUnit
    {
        private float _Attacdamage;
        private float _Attacspeed;
        private float _Armor;
        private float _Speed;
        private float _Healthpoints;
        private float _Shieldarmor;

        public int ID { get; set; }
        public List<UnitAttributes> Attributes { get; set; } = new List<UnitAttributes>();
        public int Tier { get; set; } = 1;
        public UnitRace Race { get; set; }
        public UnitUpgrades AttacType { get; set; }
        public UnitUpgrades ArmorType { get; set; }
        public HashSet<UnitAbility> Abilities { get; set; } = new HashSet<UnitAbility>();
        public float Healthpoints { get { return GetHealthpoints(Ownerplayer); } set { _Healthpoints = value; } }
        public float Shieldpoints { get; set; } = 0;
        public float Attacdamage { get { return GetAttacdamage(Ownerplayer); } set { _Attacdamage = value; } }
        public float Attacspeed { get { return GetAttacspeed(Ownerplayer); } set { _Attacspeed = value; } }
        public float Attacrange { get; set; }
        public int Attacs { get; set; } = 1;
        public AreaDamage Areadamage { get; set; }
        public BonusDamage Bonusdamage { get; set; }
        public float AttacUpgradeModifier { get; set; } = 1;
        public float Armor { get { return GetArmor(Ownerplayer); } set { _Armor = value; } }
        public float ShieldArmor { get { return GetShieldArmor(Ownerplayer); } set { _Shieldarmor = value; } }
        public float ArmorUpgradeModifier { get; set; } = 1;
        public float ShieldArmorUpgradeModifier { get; set; } = 1;
        public float Speed { get { return GetSpeed(Ownerplayer); } set { _Speed = value; } }
        public float Visionrange { get; set; }
        public float Energypoints { get; set; } = 0;
        public float Energybar { get; set; } = 0;
        public float Size { get; set; }
        public int BuildSize { get; set; } = 1;
        public int Cost { get; set; }

        public List<UnitUpgrade> UpgradesEffected { get; set; }
        public UnitStatuses Status { get; set; } = UnitStatuses.Available;
        public string Image { get; set; }
        public List<string> ImagesAlt { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public Vector2 Pos { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public Vector2 RealPos { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public Vector2 BuildPos { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public Vector2 PlacePos { get; set; } = Vector2.Zero;
        public float Healthbar { get; set; }
        public float Shieldbar { get; set; } = 0;
        public int Owner { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public Unit Target { get; set; }
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public Player Ownerplayer { get; set; } = new Player();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public KeyValuePair<float, float> RelPos { get; set; } = new KeyValuePair<float, float>(0, 0);
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public KeyValuePair<float, float> LastRelPos { get; set; } = new KeyValuePair<float, float>(0, 0);
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public List<KeyValuePair<float, float>> Path { get; set; } = new List<KeyValuePair<float, float>>();
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public float DamageDoneRound { get; set; } = 0;
        [JsonIgnore]
        [IgnoreDataMember]
        [NotMapped]
        public float MineralValueKilledRound { get; set; } = 0;

        public float GetAttacdamage(Player pl)
        {
            if (pl != null && pl.Upgrades.Count() > 0)
            {
                UnitUpgrade upgrade = pl.Upgrades.Where(x => x.Upgrade == AttacType).FirstOrDefault();
                if (upgrade != null)
                    return _Attacdamage + (AttacUpgradeModifier * upgrade.Level);
            }
            return _Attacdamage;
        }

        public float GetAttacspeed(Player pl)
        {
            if (pl != null && pl.AbilityUpgrades.Count() > 0)
            {
                foreach (UnitAbility ability in Abilities.Where(x => x.Triggers.Contains(UnitAbilityTrigger.Always) && x.Type.Contains(UnitAbilityTypes.Attacspeed)))
                {
                    if (pl.AbilityUpgrades.SingleOrDefault(x => x.Ability == ability.Ability) != null)
                        return _Attacspeed * ability.AttacSpeedModifier;
                }
            }
            return _Attacspeed;
        }

        public float GetArmor(Player pl)
        {
            if (pl != null && pl.Upgrades.Count() > 0)
            {
                UnitUpgrade upgrade = pl.Upgrades.Where(x => x.Upgrade == ArmorType).FirstOrDefault();
                if (upgrade != null)
                    return _Armor + (ArmorUpgradeModifier * upgrade.Level);
            }
            return _Armor;
        }

        public float GetShieldArmor(Player pl)
        {
            if (pl != null && pl.Upgrades.Count() > 0)
            {
                UnitUpgrade upgrade = pl.Upgrades.Where(x => x.Upgrade == UnitUpgrades.ShieldArmor).FirstOrDefault();
                if (upgrade != null)
                    return _Shieldarmor + (ShieldArmorUpgradeModifier * upgrade.Level);
            }
            return _Shieldarmor;
        }

        public float GetHealthpoints(Player pl)
        {
            if (pl != null && pl.AbilityUpgrades.Count() > 0)
            {
                foreach (UnitAbility ability in Abilities.Where(x => x.Triggers.Contains(UnitAbilityTrigger.Always) && x.Type.Contains(UnitAbilityTypes.Healthpoints)).ToArray())
                {
                    if (pl.AbilityUpgrades.SingleOrDefault(x => x.Ability == ability.Ability) != null)
                        return _Healthpoints + ability.Healthmodifier;
                }
            }
            return _Healthpoints;
        }

        public float GetSpeed(Player pl)
        {
            if (pl != null && pl.AbilityUpgrades.Count() > 0)
            {
                foreach (UnitAbility ability in Abilities.Where(x => x.Triggers.Contains(UnitAbilityTrigger.Always) && x.Type.Contains(UnitAbilityTypes.Speed)))
                {
                    if (pl.AbilityUpgrades.SingleOrDefault(x => x.Ability == ability.Ability) != null)
                        return _Speed * ability.MoveSpeedModifier;
                }
            }
            return _Speed;
        }

        // method for cloning object 
        public object Shallowcopy()
        {
            return this.MemberwiseClone();
        }

        public Unit()
        {

        }

        public Unit DeepCopy()
        {
            Unit dc = new Unit();
            dc.Name = Name;
            dc.Kills = Kills;
            dc.DamageDone = DamageDone;
            dc.MineralValueKilled = MineralValueKilled;
            if (SerPos != null)
            {
                dc.SerPos = new Vector2Ser();
                dc.SerPos.x = SerPos.x;
                dc.SerPos.y = SerPos.y;
            }

            dc._Attacdamage = _Attacdamage;
            dc._Attacspeed = _Attacspeed;
            dc._Armor = _Armor;
            dc._Speed = _Speed;
            dc._Healthpoints = _Healthpoints;
            dc._Shieldarmor = _Shieldarmor;
            dc.ID = ID;
            dc.Attributes = new List<UnitAttributes>(Attributes);
            dc.Tier = Tier;
            dc.Race = Race;
            dc.AttacType = AttacType;
            dc.ArmorType = ArmorType;
            foreach (UnitAbility ability in Abilities)
                dc.Abilities.Add(ability.DeepCopy());
            dc.Shieldpoints = Shieldpoints;
            dc.Attacrange = Attacrange;
            dc.Attacs = Attacs;
            dc.Areadamage = Areadamage;
            if (Bonusdamage != null)
                dc.Bonusdamage = Bonusdamage.Deepcopy();
            dc.AttacUpgradeModifier = AttacUpgradeModifier;
            dc.ShieldArmor = ShieldArmor;
            dc.ArmorUpgradeModifier = ArmorUpgradeModifier;
            dc.ShieldArmorUpgradeModifier = ShieldArmorUpgradeModifier;
            dc.Visionrange = Visionrange;
            dc.Energybar = Energybar;
            dc.Energypoints = Energypoints;
            dc.Size = Size;
            dc.BuildSize = BuildSize;
            dc.Cost = Cost;
            if (UpgradesEffected != null)
                dc.UpgradesEffected = new List<UnitUpgrade>(UpgradesEffected);
            dc.Status = Status;
            dc.Image = Image;
            if (ImagesAlt != null)
                dc.ImagesAlt = new List<string>(ImagesAlt);
            dc.PlacePos = PlacePos;
            dc.Pos = Pos;
            dc.RealPos = RealPos;
            dc.BuildPos = BuildPos;
            dc.Healthbar = Healthbar;
            dc.Shieldbar = Shieldbar;
            dc.Owner = Owner;
            dc.Target = Target;
            dc.Ownerplayer = Ownerplayer;
            dc.RelPos = RelPos;
            dc.LastRelPos = LastRelPos;
            dc.DamageDoneRound = DamageDoneRound;
            dc.MineralValueKilledRound = MineralValueKilledRound;

            return dc;
        }
    }

    public enum UnitStatuses
    {
        Available,
        Placed,
        Spawned,
        Deleted
    }

    public enum UnitRace
    {
        Protoss,
        Terran,
        Zerg,
        Defence,
        Neutral,
        Decoy,
    }

    public enum UnitAttributes
    {
        Biological,
        Light,
        Armored,
        Suicide, // only if Areadamage defined
        Defence,
        Massive,
        Neutral,
        Mechanical,
        Psionic,
        Decoy,
    }
    /*
    public class UnitEvent
    {
        public int Gameloop { get; set; }
        public int PlayerId { get; set; } = 0;
        public int KilledId { get; set; } = 0;
        public int KilledBy { get; set; } = 0;
        public int KillerRecycleTag { get; set; } = 0;
        public int Index { get; set; }
        public int RecycleTag { get; set; }
        public string Name { get; set; } = "";
        public int x { get; set; }
        public int y { get; set; }
        public int GameloopDied { get; set; }
        public int x_died { get; set; }
        public int y_died { get; set; }
    }
    */
}
