using Microsoft.Extensions.Logging;
using paxgamelib.Data;
using paxgamelib.Models;
using paxgamelib.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace paxgamelib.Service
{
    public static class FightService
    {
        internal static ILogger _logger;

        public static async Task Fight(Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies, float damage = 0, float dur = 0.25f)
        {
            if (vs.Healthbar > 0)
            {
                _logger.LogDebug(unit.ID + " fighting " + vs.ID);

                float attacdamage = unit.Attacdamage;
                if (unit.Bonusdamage != null && vs.Attributes.Contains(unit.Bonusdamage.Attribute))
                    attacdamage += unit.Bonusdamage.Damage;

                if (damage != 0)
                    attacdamage /= damage;
                else
                {
                    await AbilityService.TriggerAbilities(unit, vs, UnitAbilityTrigger.FightStart, battlefield, enemies);
                    attacdamage = unit.Attacdamage;
                    if (unit.Bonusdamage != null && vs.Attributes.Contains(unit.Bonusdamage.Attribute))
                        attacdamage += unit.Bonusdamage.Damage;
                    if (unit.Areadamage != null)
                        await FightArea(unit, vs, battlefield, enemies);
                    await AbilityService.TriggerAbilities(unit, vs, UnitAbilityTrigger.FightEnd, battlefield, enemies);
                }
                if (unit.Healthbar == 0)
                    return;

                float attacspeed = unit.Attacspeed;
                float enemyarmor = vs.Armor;
                float enemyshieldarmor = vs.ShieldArmor;

                float attacs = 1;
                if (attacspeed > 0)
                    attacs = dur / attacspeed;
                float restdps = 0;

                for (int i = 0; i < unit.Attacs; i++)
                {
                    if (vs.Healthbar == 0) break;
                    bool hasShield = false;
                    if (vs.Shieldbar > 0)
                    {
                        hasShield = true;
                        float smitigation = attacs * vs.ShieldArmor;
                        float sdps = attacs * attacdamage - smitigation;
                        if (sdps <= 0)
                            sdps = 0;
                        float snewhp = vs.Shieldbar - sdps;
                        if (snewhp < 0)
                        {
                            restdps = snewhp * -1;
                            hasShield = false;
                            snewhp = 0;
                        }
                        vs.Shieldbar = snewhp;

                        // TODO: Move to Fightstart Abilities
                        UnitAbility shield = vs.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.ShieldRegeneration);
                        if (shield != null)
                            shield.Cooldown = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.ShieldRegeneration).Cooldown;

                        unit.DamageDone += sdps;
                    }

                    if (hasShield == false)
                    {
                        float mydps = attacdamage;
                        if (restdps > 0)
                            mydps = restdps;

                        float mitigation = attacs * enemyarmor;
                        float dps = attacs * mydps - mitigation;
                        if (dps <= 0)
                            dps = 0;
                        float newhp = vs.Healthbar - dps;

                        if (newhp <= 0)
                        {
                            newhp = 0;
                            unit.Target = null;

                            lock (battlefield.KilledUnits)
                            {
                                if (!battlefield.KilledUnits.Contains(vs))
                                {
                                    battlefield.KilledUnits.Add(vs);
                                    unit.Kills++;
                                    unit.MineralValueKilledRound += vs.Cost;
                                    AbilityService.TriggerAbilities(vs, unit, UnitAbilityTrigger.OnDeath, battlefield, enemies);
                                }
                            }
                            battlefield.UnitPostions.TryRemove(vs.Pos, out _);
                            _logger.LogDebug(vs.ID + " died (killer: " + unit.ID + ")");
                        }
                        vs.Healthbar = newhp;

                        unit.DamageDoneRound += dps;
                    }
                }
            }
        }

        public static async Task FightArea(Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies, float dur = 0.25f)
        {
            if (unit.Attacdamage == 0) return;

            List<Unit> Targets1 = new List<Unit>();
            List<Unit> Targets2 = new List<Unit>();
            List<Unit> Targets3 = new List<Unit>();

            Vector2 center = vs.RealPos;
            if (unit.Attributes.Contains(UnitAttributes.Suicide))
                center = unit.RealPos;

            if (unit.Areadamage.Distance1 != 0 && unit.Areadamage.Distance2 != 0 && unit.Areadamage.Distance3 != 0)
            {
                if (unit.Areadamage.FriendlyFire == false)
                    Targets3 = enemies.Where(x => Vector2.Distance(center, x.RealPos) < unit.Areadamage.Distance3).ToList();
                else
                    Targets3 = battlefield.Units.Where(x => Vector2.Distance(center, x.RealPos) < unit.Areadamage.Distance3).ToList();
                Targets2 = Targets3.Where(x => Vector2.Distance(vs.RealPos, x.RealPos) < unit.Areadamage.Distance2).ToList();
                Targets1 = Targets2.Where(x => Vector2.Distance(vs.RealPos, x.RealPos) < unit.Areadamage.Distance1).ToList();

                Targets3 = Targets3.Except(Targets2).Except(Targets1).ToList();
                Targets2 = Targets2.Except(Targets1).ToList();

            }
            else if (unit.Areadamage.Distance1 != 0 && unit.Areadamage.Distance2 != 0)
            {
                if (unit.Areadamage.FriendlyFire == false)
                    Targets2 = enemies.Where(x => Vector2.Distance(center, x.RealPos) < unit.Areadamage.Distance2).ToList();
                else
                    Targets2 = battlefield.Units.Where(x => Vector2.Distance(center, x.RealPos) < unit.Areadamage.Distance2).ToList();
                Targets1 = Targets3.Where(x => Vector2.Distance(vs.RealPos, x.RealPos) < unit.Areadamage.Distance1).ToList();

                Targets2 = Targets2.Except(Targets1).ToList();
            }
            else if (unit.Areadamage.Distance1 != 0)
            {
                if (unit.Areadamage.FriendlyFire == false)
                {
                    if (unit.Owner <= 3)
                        Targets1 = battlefield.Units.Where(x => x.Owner > 3 && Vector2.Distance(center, x.RealPos) < unit.Areadamage.Distance1).ToList();
                    else if (unit.Owner > 3)
                        Targets1 = battlefield.Units.Where(x => x.Owner <= 3 && Vector2.Distance(center, x.RealPos) < unit.Areadamage.Distance1).ToList();
                }
                else
                    Targets1 = battlefield.Units.Where(x => Vector2.Distance(center, x.RealPos) < unit.Areadamage.Distance1).ToList();
            }

            if (Targets1.Contains(unit))
                Targets1.Remove(unit);
            if (Targets2.Contains(unit))
                Targets2.Remove(unit);
            if (Targets3.Contains(unit))
                Targets3.Remove(unit);

            foreach (Unit target in Targets1)
                await Fight(unit, target, battlefield, enemies, 1);
            foreach (Unit target in Targets2)
                await Fight(unit, target, battlefield, enemies, 2);
            foreach (Unit target in Targets3)
                await Fight(unit, target, battlefield, enemies, 4);
        }




    }
}
