using paxgamelib.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using paxgamelib.Data;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace paxgamelib.Service
{
    public static class UnitService
    {
        internal static ILogger _logger;

        public static KeyValuePair<Unit, Unit> EnemyinRange(Unit unit, List<Unit> enemies)
        {
            float attac_distance = -1;
            float vision_distance = -1;
            float minattac_distance = 0.25f;
            if (unit.Attributes.Contains(UnitAttributes.Suicide))
                minattac_distance = 0.25f;
            Unit myattac_enemy = new Unit();
            Unit myvision_enemy = new Unit();
            foreach (Unit enemy in enemies)
            {
                //float d = Vector2.Distance(unit.RealPos, enemy.RealPos) - enemy.Size / StartUp.Battlefieldmodifier;
                float d = Vector2.Distance(unit.RealPos, enemy.RealPos);
                d -= enemy.Size / paxgame.Battlefieldmodifier;

                if (d <= minattac_distance || d < unit.Attacrange)
                {
                    if (attac_distance == -1)
                    {
                        attac_distance = d;
                        myattac_enemy = enemy;
                    }

                    else if (d < attac_distance)
                    {
                        attac_distance = d;
                        myattac_enemy = enemy;
                    }
                }
                else if (d < unit.Visionrange)
                {
                    if (vision_distance == -1)
                    {
                        vision_distance = d;
                        myvision_enemy = enemy;
                    }

                    else if (d < vision_distance)
                    {
                        vision_distance = d;
                        myvision_enemy = enemy;
                    }
                }
            }
            return new KeyValuePair<Unit, Unit>(myattac_enemy, myvision_enemy);
        }

        public static async Task Act(Unit unit, Battlefield battlefield, List<Unit> enemies1, List<Unit> enemies2)
        {
            _logger.LogDebug(unit.ID + " act (" + unit.Healthbar + " " + unit.Speed +  ") " + unit.Name);

            if (unit.Healthbar > 0)
            {
                List<Unit> enemies = new List<Unit>();
                List<Unit> allies = new List<Unit>();
                if (unit.Owner <= 3)
                {
                    enemies = enemies1;
                    allies = enemies2;
                }
                else
                {
                    enemies = enemies2;
                    allies = enemies1;
                }

                await AbilityService.UseAbilities(unit, battlefield, enemies, allies);

                if (unit.Target != null && unit.Target.Healthbar > 0)
                {
                    await AbilityService.TriggerAbilities(unit, unit.Target, UnitAbilityTrigger.EnemyInVision, battlefield, enemies);
                    await FightService.Fight(unit, unit.Target, battlefield, enemies);
                }
                else
                {
                    KeyValuePair<Unit, Unit> myenemy = EnemyinRange(unit, enemies);

                    if (myenemy.Key.Name != null)
                    {
                        await AbilityService.TriggerAbilities(unit, myenemy.Key, UnitAbilityTrigger.EnemyInVision, battlefield, enemies);
                        unit.Target = myenemy.Key;
                        await FightService.Fight(unit, myenemy.Key, battlefield, enemies);
                    }
                    else
                    {
                        if (myenemy.Value.Name != null)
                            await AbilityService.TriggerAbilities(unit, myenemy.Value, UnitAbilityTrigger.EnemyInVision, battlefield, enemies);
                        await MoveService.Move(unit, myenemy.Value, battlefield);
                    }
                }
            }
            unit.Path.Add(new KeyValuePair<float, float>(unit.RelPos.Key, unit.RelPos.Value));
            Interlocked.Increment(ref battlefield.Done);
            _logger.LogDebug(unit.ID + " act done (" + unit.Healthbar + " " + unit.Speed + ") " + unit.Name);
        }

        public static List<Vector2> ResetUnits(List<Unit> units)
        {
            List<Vector2> pos = new List<Vector2>();
            foreach (Unit u in units)
            {
                lock (u)
                {
                    u.Healthbar = u.Healthpoints;
                    u.Shieldbar = u.Shieldpoints;
                    if (u.Energypoints > 0)
                        u.Energybar = UnitPool.Units.SingleOrDefault(x => x.Name == u.Name).Energybar;
                    u.Target = null;
                    u.Pos = u.BuildPos;
                    u.RealPos = u.BuildPos;
                    u.RelPos = MoveService.GetRelPos(u.RealPos);
                    u.Status = UnitStatuses.Spawned;
                    pos.Add(u.Pos);
                    u.Path = new List<KeyValuePair<float, float>>();

                    foreach (var ability in u.Abilities.ToArray())
                    {
                        ability.TargetPos = Vector2.Zero;
                        bool deactivated = ability.Deactivated;
                        ability.isActive = false;
                        ability.Deactivate(u);

                        if (UnitPool.Units.SingleOrDefault(x => x.Name == u.Name) != null && UnitPool.Units.SingleOrDefault(x => x.Name == u.Name).Abilities.SingleOrDefault(x => x.Ability == ability.Ability) != null)
                        {
                            UnitAbility reset = new UnitAbility();
                            reset = AbilityPool.Abilities.Where(x => x.Ability == ability.Ability).FirstOrDefault().DeepCopy();
                            reset.Deactivated = deactivated;
                            u.Abilities.Remove(ability);
                            u.Abilities.Add(reset);
                        }
                    }
                }
            }
            return pos;
        }

        public static void NewUnit(Player _player, Unit myunit)
        {
            myunit.ID = _player.Game.GetUnitID();
            myunit.Status = UnitStatuses.Placed;
            NewUnitPos(_player, myunit);
            myunit.Owner = _player.Pos;
            myunit.Ownerplayer = _player;
            if (myunit.Bonusdamage != null)
                myunit.Bonusdamage.Ownerplayer = myunit.Ownerplayer;
            _player.UpgradesAvailable.Add(myunit.AttacType);
            _player.UpgradesAvailable.Add(myunit.ArmorType);
            if (myunit.Shieldpoints > 0)
                _player.UpgradesAvailable.Add(UnitUpgrades.ShieldArmor);

            _player.AbilitiesSingleDeactivated[myunit.ID] = new Dictionary<UnitAbilities, bool>();
            foreach (UnitAbility ability in myunit.Abilities)
            {
                _player.AbilityUpgradesAvailable.Add(ability.Ability);
                if (!_player.AbilitiesGlobalDeactivated.ContainsKey(ability.Ability))
                    _player.AbilitiesGlobalDeactivated[ability.Ability] = false;
                else
                    ability.Deactivated = _player.AbilitiesGlobalDeactivated[ability.Ability];

                if (!_player.AbilitiesSingleDeactivated[myunit.ID].ContainsKey(ability.Ability))
                    _player.AbilitiesSingleDeactivated[myunit.ID][ability.Ability] = false;
                else
                    ability.Deactivated = _player.AbilitiesSingleDeactivated[myunit.ID][ability.Ability];
            }

            UnitAbility imageability = myunit.Abilities.SingleOrDefault(x => x.Type.Contains(UnitAbilityTypes.Image));
            if (imageability != null)
                if (_player.AbilityUpgrades.SingleOrDefault(x => x.Ability == imageability.Ability) != null)
                    myunit.Image = imageability.Image;
        }

        public static void NewUnitPos(Player _player, Unit myunit)
        {
            myunit.BuildPos = new Vector2(myunit.PlacePos.Y, 30 - myunit.PlacePos.X - 2.5f);
            if (_player.Pos > 3)
                myunit.BuildPos = mirrorImage(myunit.BuildPos);

            myunit.RealPos = myunit.BuildPos;
            myunit.Pos = myunit.BuildPos;
            myunit.SerPos = new Vector2Ser();
            myunit.SerPos.x = myunit.BuildPos.X;
            myunit.SerPos.y = myunit.BuildPos.Y;
            myunit.RelPos = MoveService.GetRelPos(myunit.RealPos);
        }

        public static int UpgradeUnit(UnitUpgrades upgrade, Player _player)
        {
            (int cost, int lvl) = GetUpgradeCost(upgrade, _player);

            Upgrade myupgrade = UpgradePool.Upgrades.Where(x => x.Race == _player.Race && x.Name == upgrade).FirstOrDefault();
            if (myupgrade == null) return 0;

            UnitUpgrade plup = _player.Upgrades.Where(x => x.Upgrade == myupgrade.Name).FirstOrDefault();
            if (plup != null)
            {
                if (plup.Level < 3)
                    plup.Level++;
            }
            else
            {
                UnitUpgrade newup = new UnitUpgrade();
                newup.Upgrade = myupgrade.Name;
                newup.Level = 1;
                _player.Upgrades.Add(newup);
            }
            return cost;
        }

        public static (int, int) GetUpgradeCost(UnitUpgrades upgrade, Player _player)
        {
            Upgrade myupgrade = UpgradePool.Upgrades.Where(x => x.Race == _player.Race && x.Name == upgrade).FirstOrDefault();

            if (myupgrade == null) return (0, 0);

            if (_player.Upgrades != null && _player.Upgrades.Count() > 0)
            {
                UnitUpgrade plup = _player.Upgrades.Where(x => x.Upgrade == myupgrade.Name).FirstOrDefault();
                if (plup != null)
                {
                    if (plup.Level == 3)
                    {
                        return (0, plup.Level);
                    }
                    else
                        return (myupgrade.Cost.SingleOrDefault(x => x.Key == plup.Level + 1).Value, plup.Level + 1);
                }
            }

            return (myupgrade.Cost[0].Value, 1);
        }

        public static int AbilityUpgradeUnit(UnitAbility ability, Player _player)
        {
            _player.AbilityUpgrades.Add(ability.DeepCopy());
            if (ability.Tandem != null)
                foreach (UnitAbilities myab in ability.Tandem)
                {
                    UnitAbility tandemability = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == myab);
                    if (tandemability != null)
                    {
                        _player.AbilityUpgrades.Add(tandemability.DeepCopy());
                        if (tandemability.Type.Contains(UnitAbilityTypes.Image))
                            foreach (Unit unit in _player.Units.Where(x => x.Abilities.SingleOrDefault(y => y.Ability == tandemability.Ability) != null))
                                unit.Image = tandemability.Image;

                    }
                }

            if (ability.Ability == UnitAbilities.Tier3)
                _player.Tier = 3;
            else if (ability.Ability == UnitAbilities.Tier2)
                _player.Tier = 2;

            if (ability.Type.Contains(UnitAbilityTypes.Image))
                foreach (Unit unit in _player.Units.Where(x => x.Abilities.SingleOrDefault(y => y.Ability == ability.Ability) != null))
                    unit.Image = ability.Image;



            return ability.Cost;
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

        public static Vector2 mirrorImage(Vector2 vec)
        {
            float a = 1;
            float b = 0;
            float c = ((float)Battlefield.Xmax / -2);
            float x1 = vec.X;
            float y1 = vec.Y;
            float temp = -2 * (a * x1 + b * y1 + c) /
                               (a * a + b * b);
            float x = temp * a + x1;
            float y = temp * b + y1;
            return new Vector2(x, y);
        }
    }
}
