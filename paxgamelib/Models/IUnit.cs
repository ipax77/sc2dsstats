using System.Collections.Generic;
using System.Numerics;

namespace paxgamelib.Models
{
    public interface IUnit
    {
        HashSet<UnitAbility> Abilities { get; }
        AreaDamage Areadamage { get;}
        float Armor { get; }
        UnitUpgrades ArmorType { get; }
        float ArmorUpgradeModifier { get; }
        float Attacdamage { get; }
        float Attacrange { get; }
        int Attacs { get; }
        float Attacspeed { get; }
        UnitUpgrades AttacType { get; }
        float AttacUpgradeModifier { get; }
        List<UnitAttributes> Attributes { get; }
        BonusDamage Bonusdamage { get; }
        Vector2 BuildPos { get; }
        int Cost { get; }
        float Healthbar { get; }
        float Healthpoints { get; }
        int ID { get; }
        string Image { get; }
        int Owner { get; }
        Player Ownerplayer { get; }
        Vector2 Pos { get; }
        UnitRace Race { get; }
        Vector2 RealPos { get; }
        KeyValuePair<float, float> RelPos { get; }
        float ShieldArmor { get; }
        float ShieldArmorUpgradeModifier { get; }
        float Shieldbar { get; }
        float Shieldpoints { get; }
        float Speed { get; }
        UnitStatuses Status { get; }
        Unit Target { get; }
        int Tier { get; }
        List<UnitUpgrade> UpgradesEffected { get; }
        float Visionrange { get; }
        float Size { get;  }

        float GetArmor(Player pl);
        float GetAttacdamage(Player pl);
        float GetAttacspeed(Player pl);
        float GetHealthpoints(Player pl);
        float GetShieldArmor(Player pl);
        float GetSpeed(Player pl);
        object Shallowcopy();
    }
}