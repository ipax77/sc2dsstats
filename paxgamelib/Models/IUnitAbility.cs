using System;
using System.Collections.Generic;

namespace paxgamelib.Models
{
    public interface IUnitAbility
    {
        UnitAbilities Ability { get; }
        DateTime Activated { get; }
        float AttacDamageModifier { get; }
        float AttacSpeedModifier { get; }
        bool CastOnEnemy { get; }
        TimeSpan Cooldown { get; }
        int Cost { get; }
        TimeSpan Duration { get; }
        float Healthmodifier { get; }
        bool isActive { get; }
        float MoveSpeedModifier { get; }
        float PosModifier { get; }
        float Radius { get; }
        float Regeneration { get; }
        int Tier { get; }
        List<UnitAbilityTrigger> Triggers { get; }
        ICollection<UnitAbilityTypes> Type { get; }
    }
}