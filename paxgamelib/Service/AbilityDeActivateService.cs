using paxgamelib.Data;
using paxgamelib.Models;
using paxgamelib.Service;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace paxgamelib.Service
{
    public static class AbilityDeActivateService
    {
        public static void ActivateCharge(UnitAbility ability, Unit unit, Unit vs, Battlefield battlefield)
        {
            if (vs != null && Vector2.Distance(unit.RealPos, vs.RealPos) <= 4)
            {
                ability.isActive = true;
                unit.Speed = ability.MoveSpeedModifier;
                UnitAbility impact = unit.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.SunderingImpact);
                if (impact != null)
                    impact.Radius = 1;
            }
        }

        public static void DeactivateCharge(UnitAbility ability, Unit unit)
        {
            unit.Speed = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name).Speed;
        }

        public static void ActivateSunderingImpact(UnitAbility myability, Unit unit, Unit vs, Battlefield battlefield)
        {
            UnitAbility charge = unit.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.Charge);
            if (myability.Radius == 1 && charge != null && charge.isActive == true && charge.Duration > TimeSpan.Zero)
            {
                myability.isActive = true;
                unit.Attacdamage += myability.AttacDamageModifier;
                myability.Radius = 0;
            } else
            {
                unit.Attacdamage = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name).Attacdamage;
            }
        }

        public static void DeactivateSunderingImpact(UnitAbility ability, Unit unit)
        {
            unit.Attacdamage = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name).Attacdamage;
            ability.Deactivated = false;
        }

        public static void ActivateTransfusion(UnitAbility ability, Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies, List<Unit> allies)
        {
            if (unit.Energybar >= ability.EnergyCost)
            {
                Unit healme = new Unit();
                float distance = -1;
                foreach (Unit ally in allies.Where(x => x.Attributes.Contains(UnitAttributes.Biological) && x.Healthpoints >= 75 && x.Healthbar <= x.Healthpoints / 2))
                {
                    float mydistance = Vector2.Distance(unit.RealPos, ally.RealPos);
                    if (mydistance <= ability.Radius)
                    {
                        if (distance == -1)
                            distance = mydistance;

                        if (mydistance <= distance)
                            healme = ally;
                    }
                }
                if (healme.Name != null)
                {
                    lock (healme)
                    {
                        ability.isActive = true;
                        unit.Energybar -= ability.EnergyCost;
                        healme.Healthbar += ability.Healthmodifier;
                        if (healme.Healthbar > healme.Healthpoints)
                            healme.Healthbar = healme.Healthpoints;

                        UnitAbility regen = healme.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.TransfusionRegeneration);
                        if (regen == null)
                            healme.Abilities.Add(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.TransfusionRegeneration).DeepCopy());
                        else
                            regen.Duration = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.TransfusionRegeneration).Duration;

                        Unit vis = UnitPool.Units.SingleOrDefault(x => x.Name == "AbilityVisualization").DeepCopy();
                        vis.ID = unit.Ownerplayer.Game.GetUnitID();
                        vis.Owner = healme.Owner;
                        vis.Pos = healme.Pos;
                        vis.RealPos = healme.RealPos;
                        vis.RelPos = healme.RelPos;
                        vis.SerPos = healme.SerPos;
                        vis.Image = "images/pax_ability_queenregen.png";
                        vis.Abilities.First().isActive = true;
                        battlefield.Units.Add(vis);
                    }
                }
            }
        }

        public static void ActivateHaluzination(UnitAbility ability, Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies)
        {
            if (unit.Energybar >= ability.EnergyCost)
            {
                ability.isActive = true;
                unit.Energybar -= ability.EnergyCost;
                Unit Halu = UnitPool.Units.SingleOrDefault(x => x.Name == "ArchonHaluzination").DeepCopy();
                Halu.Owner = unit.Owner;
                Halu.Ownerplayer = unit.Ownerplayer;
                Halu.ID = unit.Ownerplayer.Game.GetUnitID();
                Halu.Pos = unit.Pos;
                Halu.RealPos = unit.RealPos;
                Halu.RelPos = unit.RelPos;
                Halu.Abilities.First().isActive = true;
                battlefield.Units.Add(Halu);
            }
        }

        public static void DeactivateLifeTime(UnitAbility ability, Unit unit)
        {
            if (ability.isActive == true)
            {
                unit.Shieldbar = 0;
                unit.Healthbar = 0;
                foreach (UnitAbility myability in unit.Abilities)
                    myability.isActive = false;
            }
        }

        public static void ActivateGuardianShieldAttacModifier(UnitAbility ability, Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies)
        {
            if (unit.Attacrange >= 1)
            {
                ability.isActive = true;
                unit.Attacdamage -= 2;
                if (unit.Bonusdamage != null)
                    unit.Bonusdamage.Damage -= 2;
            }
        }

        public static void DeactivateGuardianShieldAttacModifier(UnitAbility ability, Unit unit)
        {
            Unit reference = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name);
            if (reference == null)
                reference = UnitPool.Units.SingleOrDefault(x => x.Name == "NA");
            unit.Attacdamage = reference.Attacdamage;
            if (reference.Bonusdamage != null)
                unit.Bonusdamage.Damage = reference.Bonusdamage.Damage;
        }

        public static void ActivateGuardianShieldMitigation(UnitAbility ability, Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies)
        {
            if (vs.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.GuardianShieldAttacModifier) == null)
                vs.Abilities.Add(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.GuardianShieldAttacModifier).DeepCopy());
        }

        public static void DeactivateGuardianShieldMitigation(UnitAbility ability, Unit unit)
        {
            if (unit.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.GuardianShieldAttacModifier) != null)
                unit.Abilities.Remove(unit.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.GuardianShieldAttacModifier));
        }

        public static void ActivateGuardianShield(UnitAbility ability, Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies)
        {
            if (unit.Energybar > ability.EnergyCost && unit.Healthbar > 0) {
                if (enemies.Where(x => Vector2.Distance(x.RealPos, unit.RealPos) < unit.Visionrange && x.Attacrange >= 1).Count() > 0) {
                    ability.isActive = true;
                    unit.Energybar -= ability.EnergyCost;
                    Unit GuardienShield = UnitPool.Units.SingleOrDefault(x => x.Name == "GuardianShield").DeepCopy();
                    GuardienShield.Owner = unit.Owner;
                    GuardienShield.Ownerplayer = unit.Ownerplayer;
                    GuardienShield.ID = unit.Ownerplayer.Game.GetUnitID();
                    GuardienShield.Pos = unit.Pos;
                    GuardienShield.RealPos = unit.RealPos;
                    GuardienShield.RelPos = unit.RelPos;
                    GuardienShield.Abilities.First().isActive = true;
                    battlefield.Units.Add(GuardienShield);
                    ability.Beacon = GuardienShield;
                }
            }
        }

        public static void DeactivateGuardianShield(UnitAbility ability, Unit unit)
        {
            if (ability.Beacon != null)
            {
                ability.Beacon.Healthbar = 0;
                ability.Beacon = null;
            }
        }                

        public static void ActivateResonatingGlaives(UnitAbility ability, Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies)
        {
            ability.isActive = true;
            unit.Attacspeed *= ability.AttacSpeedModifier;
        }

        public static void DeactivateResonatingGlaives(UnitAbility ability, Unit unit)
        {
            unit.Attacspeed = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name).Attacspeed;
        }

        public static void ActivatePsionicTransfer(UnitAbility ability, Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies)
        {
            if (unit.Shieldbar + unit.Healthbar < (unit.Shieldpoints + unit.Healthpoints) / 3)
            {
                ability.isActive = true;
                unit.Target = null;
                vs.Target = null;

                Vector2 BlinkDirection = MoveService.RotatePoint(vs.RealPos, unit.RealPos, 180);

                float d = Vector2.Distance(unit.RealPos, vs.RealPos);
                if (d < 0)
                    d *= -1;
                float t = 1;
                if (d > 0)
                {
                    t = (8 / paxgame.Battlefieldmodifier) / d;
                    //if (t > 1)
                    //    t = 1;
                }

                Vector2 BlinkPos = new Vector2();
                BlinkPos.X = (1 - t) * unit.RealPos.X + t * BlinkDirection.X;
                BlinkPos.Y = (1 - t) * unit.RealPos.Y + t * BlinkDirection.Y;

                ability.TargetPos = BlinkPos;
            }
        }

        public static void DeactivatePsionicTransfer(UnitAbility ability, Unit unit)
        {
            if (ability.TargetPos != null && ability.TargetPos != Vector2.Zero)
            {
                unit.Target = null;
                unit.RealPos = ability.TargetPos;
                unit.RelPos = MoveService.GetRelPos(unit.RealPos);
            }
        }

        public static void ActivateBlink(UnitAbility ability, Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies)
        {
            if (unit.Shieldbar + unit.Healthbar < (unit.Shieldpoints + unit.Healthpoints) / 3)
            {
                ability.isActive = true;
                unit.Target = null;
                vs.Target = null;

                Vector2 BlinkDirection = MoveService.RotatePoint(vs.RealPos, unit.RealPos, 180);

                float d = Vector2.Distance(unit.RealPos, vs.RealPos);
                if (d < 0)
                    d *= -1;
                float t = 1;
                if (d > 0)
                {
                    t = (8 / 2) / d;
                    //if (t > 1)
                    //    t = 1;
                }

                Vector2 BlinkPos = new Vector2();
                BlinkPos.X = (1 - t) * unit.RealPos.X + t * BlinkDirection.X;
                BlinkPos.Y = (1 - t) * unit.RealPos.Y + t * BlinkDirection.Y;

                unit.RealPos = BlinkPos;
                unit.RelPos = MoveService.GetRelPos(unit.RealPos);
            }
        }

        public static void ActivateKnockBack(UnitAbility ability, Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies)
        {
            //List<Unit> Targets = battlefield.Units.Where(x => Vector2.Distance(unit.RealPos, x.RealPos) < ability.Radius).ToList();
            List<Unit> Targets = enemies.Where(x => Vector2.Distance(unit.RealPos, x.RealPos) <= ability.Radius).ToList();
            foreach (Unit ent in Targets)
            {
                if (ent.Attributes.Contains(UnitAttributes.Massive)) continue;
                lock (ent)
                {
                    ent.Target = null;

                    Vector2 BlinkDirection = MoveService.RotatePoint(unit.RealPos, ent.RealPos, 180);
                    //float distance = Vector2.Distance(unit.RealPos, BlinkDirection);
                    //float t = 1 / distance;
                    float t = 1;
                    Vector2 BlinkPos = new Vector2();
                    BlinkPos.X = (1 - t) * ent.RealPos.X + t * BlinkDirection.X;
                    BlinkPos.Y = (1 - t) * ent.RealPos.Y + t * BlinkDirection.Y;
                    ent.RealPos = BlinkPos;
                    ent.RelPos = MoveService.GetRelPos(ent.RealPos);
                    ent.Path.Remove(ent.Path.Last());
                    ent.Path.Add(new KeyValuePair<float, float>(ent.RelPos.Key, ent.RelPos.Value));
                }
            }
        }

        public static void ActivateKD8Charge(UnitAbility ability, Unit unit, Unit vs, Battlefield battlefield, List<Unit> enemies)
        {
            ability.isActive = true;
            Unit kd8 = UnitPool.Units.SingleOrDefault(x => x.Name == "KD8").DeepCopy();
            kd8.ID = unit.Ownerplayer.Game.GetUnitID();
            kd8.Owner = unit.Owner;
            Vector2 pos = new Vector2();
            Vector2 intpos = new Vector2();
            (pos, intpos) = MoveService.GetTargetPos(10, unit, vs.RealPos, 0);
            kd8.RealPos = pos;
            kd8.Pos = intpos;
            kd8.RelPos = MoveService.GetRelPos(pos);
            battlefield.Units.Add(kd8);

        }

        public static void ActivateConcussiveShells(Unit unit)
        {
            Unit vs = unit.Target;
            if (vs != null && vs.Race != UnitRace.Defence)
            {
                lock (vs)
                {
                    UnitAbility shells = vs.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.ConcussiveShellsModifier);
                    if (shells == null)
                    {
                        shells = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.ConcussiveShellsModifier).DeepCopy();
                        shells.isActive = true;
                        vs.Abilities.Add(shells);

                        vs.Speed *= shells.MoveSpeedModifier;
                    }
                    else
                        shells.Cooldown = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.ConcussiveShells).Cooldown;

                    shells.isActive = true;
                }
            }
        }

        public static void DeactivateConcussiveShellsModifier(UnitAbility ability, Unit unit)
        {
            unit.Speed = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name).Speed;
        }

        public static void ActivateStimpack(UnitAbility ability, Unit unit, Battlefield battlefield)
        {
            if (unit.Healthpoints > 100)
            {
                if (ability.isActive == false && unit.Healthbar > 20)
                {
                    unit.Attacspeed *= ability.AttacSpeedModifier;
                    unit.Healthbar += ability.Healthmodifier * 2;
                    unit.Speed *= ability.MoveSpeedModifier;
                    ability.isActive = true;

                    Unit vis = UnitPool.Units.SingleOrDefault(x => x.Name == "AbilityVisualization").DeepCopy();
                    vis.ID = unit.Ownerplayer.Game.GetUnitID();
                    vis.Owner = unit.Owner;
                    vis.Pos = unit.Pos;
                    vis.RealPos = unit.RealPos;
                    vis.RelPos = unit.RelPos;
                    vis.SerPos = unit.SerPos;
                    vis.Abilities.First().isActive = true;
                    battlefield.Units.Add(vis);
                }
            }
            else
            {
                if (ability.isActive == false && unit.Healthbar > 10)
                {
                    unit.Attacspeed *= ability.AttacSpeedModifier;
                    unit.Healthbar += ability.Healthmodifier;
                    unit.Speed *= ability.MoveSpeedModifier;
                    ability.isActive = true;

                    Unit vis = UnitPool.Units.SingleOrDefault(x => x.Name == "AbilityVisualization").DeepCopy();
                    vis.ID = unit.Ownerplayer.Game.GetUnitID();
                    vis.Owner = unit.Owner;
                    vis.Pos = unit.Pos;
                    vis.RealPos = unit.RealPos;
                    vis.RelPos = unit.RelPos;
                    vis.SerPos = unit.SerPos;
                    vis.Abilities.First().isActive = true;
                    battlefield.Units.Add(vis);
                }
            }
        }

        public static void DeactivateStimpack(UnitAbility ability, Unit unit)
        {
            unit.Attacspeed = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name).Attacspeed;
            unit.Speed = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name).Speed;
        }

        public static void ActivateSuicide(UnitAbility ability, Unit unit)
        {
            unit.Attacdamage = 0;
            unit.Healthbar = 0;
        }

        public static void DeactivateSuicide(UnitAbility ability, Unit unit)
        {
            unit.Attacdamage = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name).Attacdamage;
        }

        public static void ActivateSuicideOnDeath(UnitAbility ability, Unit unit, Battlefield battlefield, List<Unit> enemies)
        {
            if (ability.isActive)
                return;
            else
            {
                ability.isActive = true;
                FightService.Fight(unit, unit, battlefield, enemies);
            }
        }
    }
}
