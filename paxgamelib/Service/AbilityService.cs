using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using paxgamelib.Data;
using paxgamelib.Models;


namespace paxgamelib.Service
{
    public static class AbilityService
    {
        internal static ILogger _logger;

        public static async Task TriggerAbilities(Unit unit, Unit vs, UnitAbilityTrigger trigger, Battlefield battlefiled = null, List<Unit> enemies = null, List<Unit> allies = null)
        {
            List<UnitAbility> abilities;
            if (trigger == UnitAbilityTrigger.FightStart)
            {
                abilities = new List<UnitAbility>(vs.Abilities.Where(x => x.Triggers.Contains(UnitAbilityTrigger.OnHitStart)));
                if (abilities.Count() > 0)
                {
                    foreach (var ability in abilities)
                    {
                        if (ability.Cost == 0 || unit.Ownerplayer.AbilityUpgrades.SingleOrDefault(x => x.Ability == ability.Ability) != null)
                        {
                            if (ability.isActive == false || (ability.Cooldown == TimeSpan.Zero && ability.Duration == TimeSpan.Zero))
                            {
                                _logger.LogDebug(unit.ID + " Activating ability " + ability.Ability);
                                ability.Activate(vs, unit, battlefiled, enemies, allies);
                                ability.Cooldown = AbilityPool.IAbilities.SingleOrDefault(x => x.Ability == ability.Ability).Cooldown;

                            }
                        }
                    }
                }
            }

            abilities = new List<UnitAbility>(unit.Abilities.Where(x => x.Triggers.Contains(trigger)));
            if (abilities.Count() > 0)
            {
                foreach (var ability in abilities)
                {
                    if (ability.Deactivated == true)
                        continue;

                    if (ability.Cost == 0 || unit.Ownerplayer.AbilityUpgrades.SingleOrDefault(x => x.Ability == ability.Ability) != null)
                    {
                        if (ability.isActive == false || (ability.Cooldown == TimeSpan.Zero && ability.Duration == TimeSpan.Zero))
                        {
                            _logger.LogDebug(unit.ID + " Activating ability " + ability.Ability);
                            ability.Activate(unit, vs, battlefiled, enemies, allies);
                            ability.Cooldown = AbilityPool.IAbilities.SingleOrDefault(x => x.Ability == ability.Ability).Cooldown;

                        }
                        else if (trigger == UnitAbilityTrigger.OnDeath && ability.Beacon != null)
                            ability.Beacon.Healthbar = 0;

                    }
                }
            }



        }

        public static async Task UseAbilities(Unit unit, Battlefield battlefiled, List<Unit> enemies, List<Unit> allies)
        {
            // TODO: move Trigger Always to DeActivateAbilitesService (or in foreach to start with ..)

            foreach (UnitAbility ability in unit.Abilities.Where(x => x.Type.Contains(UnitAbilityTypes.Aura)).ToArray())
            {
                _logger.LogDebug(unit.ID + " Removing ability " + ability.Ability + " " + unit.Name);
                if (UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name) != null && UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name).Abilities.SingleOrDefault(x => x.Ability == ability.Ability) == null)
                    ability.Deactivate(unit);
            }

            foreach (Unit beacon in allies.Where(x => x.Abilities.SingleOrDefault(x => x.Type.Contains(UnitAbilityTypes.Beacon)) != null && x.Abilities.SingleOrDefault(x => x.Type.Contains(UnitAbilityTypes.Beacon)).isActive == true).ToArray())
            {
                foreach (UnitAbility ability in beacon.Abilities.Where(x => x.Type.Contains(UnitAbilityTypes.Aura)).ToArray())
                {
                    if (unit.Abilities.SingleOrDefault(x => x.Ability == ability.Ability) != null)
                        continue;
                    if (Vector2.Distance(unit.RealPos, beacon.RealPos) <= ability.Radius)
                        unit.Abilities.Add(ability.DeepCopy());
                }
            }


            if (unit.Shieldpoints > 0 && unit.Shieldpoints != unit.Shieldbar)
            {
                _logger.LogDebug(unit.ID + " regenerating shield");
                UnitAbility shield = unit.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.ShieldRegeneration);
                if (shield != null)
                {
                    if (shield.Cooldown <= TimeSpan.Zero)
                        unit.Shieldbar += shield.Regeneration * (float)Battlefield.Ticks.TotalSeconds;
                    else
                        shield.Cooldown -= Battlefield.Ticks;

                    if (unit.Shieldbar > unit.Shieldpoints)
                        unit.Shieldbar = unit.Shieldpoints;
                }
            }

            if (unit.Race == UnitRace.Zerg && unit.Healthbar != unit.Healthpoints)
            {
                _logger.LogDebug(unit.ID + " regenerating hp");
                UnitAbility regen = unit.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.Regeneration);
                if (regen != null)
                {
                    unit.Healthbar += regen.Regeneration * (float)Battlefield.Ticks.TotalSeconds;

                    if (unit.Healthbar > unit.Healthpoints)
                        unit.Healthbar = unit.Healthpoints;
                }
            }

            if (unit.Race == UnitRace.Neutral)
            {
                UnitAbility ability = unit.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.Explode);
                if (ability != null)
                    FightService.Fight(unit, unit, battlefiled, enemies);
            }

            if (unit.Energypoints > 0)
            {
                UnitAbility ability = unit.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.EnergyRegeneration);
                if (ability != null)
                {
                    if (unit.Energybar < unit.Energypoints)
                    {
                        unit.Energybar += ability.Regeneration;
                        if (unit.Energybar > unit.Energypoints)
                            unit.Energybar = unit.Energypoints;
                    }
                }
            }

            if (unit.Abilities.SingleOrDefault(x => x.Beacon != null) != null)
            {
                foreach (UnitAbility ability in unit.Abilities.Where(x => x.Beacon != null))
                {
                    ability.Beacon.Pos = unit.Pos;
                    ability.Beacon.RealPos = unit.RealPos;
                    ability.Beacon.RelPos = unit.RelPos;
                }
            }

            if (unit.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.Transfusion) != null)
                await TriggerAbilities(unit, unit.Target, UnitAbilityTrigger.AllyInRange, battlefiled, enemies, allies);

            UnitAbility TransfusionRegeneration = unit.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.TransfusionRegeneration);
            if (TransfusionRegeneration != null)
            {
                TransfusionRegeneration.isActive = true;
                if (unit.Healthbar < unit.Healthpoints)
                {
                    unit.Healthbar += TransfusionRegeneration.Regeneration;
                    if (unit.Healthbar > unit.Healthpoints)
                        unit.Healthbar = unit.Healthpoints;
                }
            }
                

            foreach (UnitAbility ability in unit.Abilities.ToArray())
            {
                if (ability.isActive == true)
                {
                    if (ability.Duration != TimeSpan.Zero)
                    {
                        ability.Duration -= Battlefield.Ticks;
                        if (ability.Duration <= Battlefield.Ticks)
                        {
                            _logger.LogDebug(unit.ID + " Deactivating ability " + ability.Ability);
                            ability.Duration = TimeSpan.Zero;
                            ability.Deactivate(unit);
                        }
                    }

                    if (ability.Cooldown != TimeSpan.Zero)
                    {
                        if (ability.Ability == UnitAbilities.SunderingImpact)
                            Console.WriteLine(unit.ID + " => " + ability.Cooldown.TotalSeconds);
                        ability.Cooldown -= Battlefield.Ticks;
                        if (ability.Cooldown <= TimeSpan.Zero)
                            ability.Cooldown = TimeSpan.Zero;
                    }
                }
                else if (ability.Ability == UnitAbilities.LifeTime)
                    ability.isActive = true;
            }
        }
    }
}
