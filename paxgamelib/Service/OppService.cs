using Microsoft.VisualBasic;
using paxgamelib.Data;
using paxgamelib.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace paxgamelib.Service
{
    public static class OppService
    {
        public static async Task PositionRandom(List<Unit> Units, Player _opp)
        {
            int[][] Pos = new int[20][];
            for (int i = 0; i < 20; i++)
            {
                Pos[i] = new int[50];
                for (int j = 0; j < 50; j++)
                    Pos[i][j] = 0;
            }

            Random rnd = new Random();
            int x = 0;
            int y = 0;
            Vector2 vec = new Vector2(x, y);

            foreach (Unit unit in Units) {
                bool valid = false;
                while (valid == false)
                {
                    x = rnd.Next(0, 19);
                    y = rnd.Next(0, 49);
                    vec = new Vector2(x, y);

                    if (Pos[x][y] == 0)
                    {
                        if (unit.Size == 1)
                        {
                            if (Pos[x - 1][y] == 1 ||
                                Pos[x + 1][y] == 1 ||
                                Pos[x][y - 1] == 1 ||
                                Pos[x][y + 1] == 1)
                                continue;

                            Pos[x - 1][y] = 1;
                            Pos[x + 1][y] = 1;
                            Pos[x][y - 1] = 1;
                            Pos[x][y + 1] = 1;
                        } else if (unit.Size == 2)
                        {
                            if (Pos[x - 1][y] == 1 ||
                                Pos[x + 1][y] == 1 ||
                                Pos[x][y - 1] == 1 ||
                                Pos[x][y + 1] == 1 ||
                                Pos[x - 2][y] == 1 ||
                                Pos[x - 1][y - 1] == 1 ||
                                Pos[x][y - 2] == 1 ||
                                Pos[x + 1][y -1 ] == 1 ||
                                Pos[x + 2][y] == 1 ||
                                Pos[x + 1][y + 1] == 1 ||
                                Pos[x][y+2] == 1 ||
                                Pos[x - 1][y + 1] == 1)
                                continue;

                            Pos[x - 1][y] = 1;
                            Pos[x + 1][y] = 1;
                            Pos[x][y - 1] = 1;
                            Pos[x][y + 1] = 1;
                            Pos[x - 2][y] = 1;
                            Pos[x - 1][y - 1] = 1;
                            Pos[x][y - 2] = 1;
                            Pos[x + 1][y - 1] = 1;
                            Pos[x + 2][y] = 1;
                            Pos[x + 1][y + 1] = 1;
                            Pos[x][y + 2] = 1;
                            Pos[x - 1][y + 1] = 1;
                        }
                        Pos[x][y] = 1;
                        unit.PlacePos = new Vector2(x, y);
                        valid = true;
                    } else
                    {

                    }

                }
            }

            foreach (Unit myunit in Units.Where(x => x.Status != UnitStatuses.Available))
            {
                myunit.PlacePos = new Vector2(myunit.PlacePos.Y / 2, myunit.PlacePos.X / 2);
                UnitService.NewUnitPos(_opp, myunit);
            }
        }

        public static async Task PositionRandomDistmod(List<Unit> Units, Player _opp, float sx = -1, float sy = -1)
        {
            Unit punit;
            int[][] Pos = new int[20][];
            for (int i = 0; i < 20; i++)
            {
                Pos[i] = new int[50];
                for (int j = 0; j < 50; j++)
                {
                    if (sx != -1)
                    {
                        punit = _opp.Units.FirstOrDefault(x => x.Status == UnitStatuses.Spawned && x.PlacePos.Y == (float)i / 2 && x.PlacePos.X == (float)j / 2);
                        // TODO unit size fill
                        if (punit != null)
                            Pos[i][j] = 1;
                        else
                            Pos[i][j] = 0;
                    }
                    else
                        Pos[i][j] = 0;
                }
            }

            Random rnd = new Random();
            int x = 0;
            int y = 0;
            if (sx != -1)
            {
                x = (int)(sy * 2);
                y = (int)(sx * 2);

            }
            else
            {
                x = rnd.Next(0, 19);
                y = rnd.Next(0, 49);
            }
            Vector2 vec = new Vector2(x, y);
            Vector2 lastvec = new Vector2(x, y);

            foreach (Unit unit in Units.Where(x => x.Status == UnitStatuses.Placed))
            {
                rnd = new Random();
                bool valid = false;
                while (valid == false)
                {
                    x = rnd.Next(0, 19);
                    y = rnd.Next(0, 49);
                    vec = new Vector2(x, y);
                    int d = rnd.Next(7, 33);
                    float shortdistance = Vector2.DistanceSquared(vec, lastvec);
                    for (int k = 0; k <= d; k++)
                    {
                        int dx = rnd.Next(0, 19);
                        int dy = rnd.Next(0, 49);
                        vec = new Vector2(dx, dy);
                        float distance = Vector2.DistanceSquared(vec, lastvec);
                        if (distance < shortdistance && Pos[dx][dy] == 0)
                        {
                            shortdistance = distance;
                            x = dx;
                            y = dy;
                            vec = new Vector2(x, y);
                        }
                    }

                    if (Pos[x][y] == 0)
                    {
                        if (unit.Size == 1)
                        {
                            if (x + 1 > 19 || x - 1 < 0 || y + 1 > 49 || y - 1 < 0)
                                continue;
                            if (Pos[x - 1][y] == 1 ||
                                Pos[x + 1][y] == 1 ||
                                Pos[x][y - 1] == 1 ||
                                Pos[x][y + 1] == 1)
                                continue;

                            Pos[x - 1][y] = 1;
                            Pos[x + 1][y] = 1;
                            Pos[x][y - 1] = 1;
                            Pos[x][y + 1] = 1;
                        }
                        else if (unit.Size == 2)
                        {
                            if (x + 1 > 19 || x - 1 < 0 || y + 1 > 49 || y - 1 < 0
                                || x + 2 > 19 || x - 2 < 0 || y + 2 > 49 || y - 2 < 0)
                                continue;

                            if (Pos[x - 1][y] == 1 ||
                                Pos[x + 1][y] == 1 ||
                                Pos[x][y - 1] == 1 ||
                                Pos[x][y + 1] == 1 ||
                                Pos[x - 2][y] == 1 ||
                                Pos[x - 1][y - 1] == 1 ||
                                Pos[x][y - 2] == 1 ||
                                Pos[x + 1][y - 1] == 1 ||
                                Pos[x + 2][y] == 1 ||
                                Pos[x + 1][y + 1] == 1 ||
                                Pos[x][y + 2] == 1 ||
                                Pos[x - 1][y + 1] == 1)
                                continue;

                            Pos[x - 1][y] = 1;
                            Pos[x + 1][y] = 1;
                            Pos[x][y - 1] = 1;
                            Pos[x][y + 1] = 1;
                            Pos[x - 2][y] = 1;
                            Pos[x - 1][y - 1] = 1;
                            Pos[x][y - 2] = 1;
                            Pos[x + 1][y - 1] = 1;
                            Pos[x + 2][y] = 1;
                            Pos[x + 1][y + 1] = 1;
                            Pos[x][y + 2] = 1;
                            Pos[x - 1][y + 1] = 1;
                        }
                        Pos[x][y] = 1;
                        unit.PlacePos = new Vector2(x, y);
                        valid = true;
                        lastvec = new Vector2(x, y);
                    }
                }
            }

            foreach (Unit myunit in Units.Where(x => x.Status == UnitStatuses.Placed))
            {
                myunit.PlacePos = new Vector2(myunit.PlacePos.Y / 2, myunit.PlacePos.X / 2);
                UnitService.NewUnitPos(_opp, myunit);
            }
        }

        public static async Task PositionRandomLinemod(List<Unit> Units, Player _opp, float sx = -1, float sy = -1)
        {
            int[][] Pos = new int[20][];
            for (int i = 0; i < 20; i++)
            {
                Pos[i] = new int[50];
                for (int j = 0; j < 50; j++)
                {
                    Unit unit = _opp.Units.Where(x => x.Status == UnitStatuses.Spawned && x.PlacePos.Y == (float)i / 2 && x.PlacePos.X == (float)j / 2).FirstOrDefault();
                    if (sx != -1 && unit != null) {
                        // TODO unit size fill
                        Pos[i][j] = 1;
                    }
                    else 
                        Pos[i][j] = 0;
                }
            }
            int x = 0;
            int y = 0;
            Random rnd = new Random();
            if (sx != -1)
            {
                x = (int)(sy * 2);
                y = (int)(sx * 2);

            }
            else
            {
                x = rnd.Next(0, 19);
                y = rnd.Next(0, 49);
            }
            Vector2 vec = new Vector2(x, y);
            Vector2 firstvec = new Vector2(x, y);

            foreach (Unit unit in Units)
            {
                bool reverse = false;
                bool valid = false;

                while (valid == false)
                {
                    if (reverse == false)
                    {
                        int newx = x;
                        int newy = y + 2;
                        if (newy > 49)
                        {
                            newx++;
                            newy = 0;
                            if (newx > 19)
                                reverse = true;
                        }
                        if (reverse == false)
                        {
                            x = newx;
                            y = newy;
                        }
                    }

                    if (reverse == true)
                    {
                        int newx = x;
                        int newy = y - 2;
                        if (newy < 0)
                        {
                            newx--;
                            newy = 49;
                            if (newx < 0)
                                reverse = false;
                        }
                        x = newx;
                        y = newy;
                    }

                    vec = new Vector2(x, y);

                    if (Pos[x][y] == 0)
                    {
                        if (unit.Size == 1)
                        {
                            if (x + 1 > 19 || x - 1 < 0 || y + 1 > 49 || y - 1 < 0)
                                continue;
                            if (Pos[x - 1][y] == 1 ||
                                Pos[x + 1][y] == 1 ||
                                Pos[x][y - 1] == 1 ||
                                Pos[x][y + 1] == 1)
                                continue;

                            Pos[x - 1][y] = 1;
                            Pos[x + 1][y] = 1;
                            Pos[x][y - 1] = 1;
                            Pos[x][y + 1] = 1;
                        }
                        else if (unit.Size == 2)
                        {
                            if (x + 1 > 19 || x - 1 < 0 || y + 1 > 49 || y - 1 < 0
                              || x + 2 > 19 || x - 2 < 0 || y + 2 > 49 || y - 2 < 0)
                                continue;
                            if (Pos[x - 1][y] == 1 ||
                                Pos[x + 1][y] == 1 ||
                                Pos[x][y - 1] == 1 ||
                                Pos[x][y + 1] == 1 ||
                                Pos[x - 2][y] == 1 ||
                                Pos[x - 1][y - 1] == 1 ||
                                Pos[x][y - 2] == 1 ||
                                Pos[x + 1][y - 1] == 1 ||
                                Pos[x + 2][y] == 1 ||
                                Pos[x + 1][y + 1] == 1 ||
                                Pos[x][y + 2] == 1 ||
                                Pos[x - 1][y + 1] == 1)
                                continue;

                            Pos[x - 1][y] = 1;
                            Pos[x + 1][y] = 1;
                            Pos[x][y - 1] = 1;
                            Pos[x][y + 1] = 1;
                            Pos[x - 2][y] = 1;
                            Pos[x - 1][y - 1] = 1;
                            Pos[x][y - 2] = 1;
                            Pos[x + 1][y - 1] = 1;
                            Pos[x + 2][y] = 1;
                            Pos[x + 1][y + 1] = 1;
                            Pos[x][y + 2] = 1;
                            Pos[x - 1][y + 1] = 1;
                        }
                        Pos[x][y] = 1;
                        unit.PlacePos = new Vector2(x, y);
                        valid = true;
                    }
                }
            }

            foreach (Unit myunit in Units)
            {
                myunit.PlacePos = new Vector2(myunit.PlacePos.Y / 2, myunit.PlacePos.X / 2);
                UnitService.NewUnitPos(_opp, myunit);

            }
        }

        public static async Task BPRandom(Player player, bool distmod = false)
        {
            var startUnits = player.Units.Where(x => x.Status == UnitStatuses.Spawned);
            Unit startUnit = startUnits.FirstOrDefault();

            List<Unit> Units = UnitPool.Units.Where(x => x.Race == player.Race && x.Cost > 0).ToList();
            Dictionary<UnitAbilities, int> AbilityCount = new Dictionary<UnitAbilities, int>();
            Dictionary<UnitUpgrades, int> UpgradeCount = new Dictionary<UnitUpgrades, int>();
            int minerals = player.MineralsCurrent;
            Random rnd = new Random();
            int armyvalue = 0;
            while (player.MineralsCurrent > 0)
            {
                Units = new List<Unit>(Units.Where(x => x.Cost <= player.MineralsCurrent));
                if (!Units.Any())
                    break;

                int doups = rnd.Next(0, Units.Count);
                Unit unit = Units.ElementAt(doups);
                if (player.MineralsCurrent >= unit.Cost)
                {
                    Unit myunit = unit.DeepCopy();
                    UnitService.NewUnit(player, myunit);
                    player.MineralsCurrent -= myunit.Cost;
                    player.Units.Add(myunit);
                    armyvalue += myunit.Cost;
                    foreach (UnitAbility ability in unit.Abilities.Where(x => x.Cost > 0))
                    {
                        if (player.AbilityUpgrades.SingleOrDefault(s => s.Ability == ability.Ability) == null)
                            if (!AbilityCount.ContainsKey(ability.Ability))
                                AbilityCount[ability.Ability] = unit.Cost;
                            else
                                AbilityCount[ability.Ability] += unit.Cost;
                    }
                    if (!UpgradeCount.ContainsKey(myunit.ArmorType))
                        UpgradeCount[myunit.ArmorType] = 1;
                    else
                        UpgradeCount[myunit.ArmorType]++;
                    if (!UpgradeCount.ContainsKey(myunit.AttacType))
                        UpgradeCount[myunit.AttacType] = 1;
                    else
                        UpgradeCount[myunit.AttacType]++;
                }
            }
            
            // only T1 Upgrades for now
            int UpgradesPossible = player.UpgradesAvailable.Count() - player.Upgrades.Count();
            /*
            foreach (UnitUpgrade upgrade in player.Upgrades)
            {
                UpgradesPossible -= upgrade.Level;
                if (upgrade.Level == 4)
                    Upgrades.Remove(upgrade.Upgrade);
            }
            */

            int AbilityUpgradesPossible = player.AbilityUpgradesAvailable.Count();
            foreach (UnitAbility ability in player.AbilityUpgrades)
            {
                AbilityUpgradesPossible -= 1;
                player.AbilityUpgradesAvailable.Remove(ability.Ability);
            }
            int rndi = rnd.Next(20);
            double upgrademod = (double)rndi / 100;
            int minsavailableforupgrades = (int)(armyvalue * upgrademod);

            if (minsavailableforupgrades > 0)
            {
                while (minsavailableforupgrades > 0)
                {
                    if (rnd.Next(100) < 50)
                        break;

                    if (AbilityUpgradesPossible > 0 && AbilityCount.Any())
                    {
                        AbilityCount = new Dictionary<UnitAbilities, int>(AbilityCount.OrderBy(o => o.Value));

                        UnitAbility ability1 = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == AbilityCount.Last().Key).DeepCopy();

                        List<Unit> RemoveUnits = new List<Unit>(player.Units.Where(x => x.Status == UnitStatuses.Placed && x.Abilities.SingleOrDefault(s => s.Ability == ability1.Ability) == null));
                        if (RemoveUnits.Sum(s => s.Cost) <= ability1.Cost)
                            while (RemoveUnits.Sum(s => s.Cost) <= ability1.Cost)
                                RemoveUnits.Add(player.Units.Where(x => x.Status == UnitStatuses.Placed && x.Abilities.SingleOrDefault(s => s.Ability == ability1.Ability) != null).First());

                        while (player.MineralsCurrent <= ability1.Cost)
                        {
                            Unit removeunit = RemoveUnits.First();
                            player.Units.Remove(removeunit);
                            player.MineralsCurrent += removeunit.Cost;
                            RemoveUnits.Remove(removeunit);
                        }
                        AbilityCount.Remove(AbilityCount.Single(s => s.Key == ability1.Ability).Key);
                        player.MineralsCurrent -= UnitService.AbilityUpgradeUnit(ability1, player);
                        minsavailableforupgrades -= ability1.Cost;
                    } else
                        break;
                }
            }

            if (minsavailableforupgrades > 0)
            {
                while (minsavailableforupgrades > 0)
                {
                    if (rnd.Next(100) < 50)
                        break;

                    if (UpgradesPossible > 0 && UpgradeCount.Any())
                    {
                        Upgrade upgrade1 = UpgradePool.Upgrades.FirstOrDefault(x => x.Race == player.Race && x.Name == UpgradeCount.ElementAt(rnd.Next(UpgradeCount.Count - 1)).Key);
                        if (upgrade1 == null)
                            continue;
                        (int upgradecost, int level) = UnitService.GetUpgradeCost(upgrade1.Name, player);
                        // only t1 upgrades for now
                        if (level > 1)
                            continue;

                        List<Unit> RemoveUnits = new List<Unit>(player.Units.Where(x => x.Status == UnitStatuses.Placed && x.ArmorType != upgrade1.Name && x.AttacType != upgrade1.Name));

                        if (!RemoveUnits.Any() || RemoveUnits.Sum(x => x.Cost) <= upgradecost)
                        {
                            while (RemoveUnits.Sum(s => s.Cost) <= upgradecost)
                                RemoveUnits.Add(player.Units.Where(x => x.Status == UnitStatuses.Placed && (x.ArmorType == upgrade1.Name || x.AttacType == upgrade1.Name)).First());
                        }
                        while (player.MineralsCurrent <= upgradecost)
                        {
                            Unit removeunit = RemoveUnits.First();
                            player.Units.Remove(removeunit);
                            player.MineralsCurrent += removeunit.Cost;
                            RemoveUnits.Remove(removeunit);
                        }
                        UpgradeCount.Remove(UpgradeCount.Single(s => s.Key == upgrade1.Name).Key);
                        player.MineralsCurrent -= UnitService.UpgradeUnit(upgrade1.Name, player);
                        minsavailableforupgrades -= upgradecost;
                    }
                    else
                        break;
                }
            }

            if (distmod)
                if (startUnit != null)
                    await PositionRandomDistmod(player.Units, player, startUnit.PlacePos.X, startUnit.PlacePos.Y).ConfigureAwait(false);
                else
                    await PositionRandomDistmod(player.Units, player).ConfigureAwait(false);
            else
            {

                if (startUnit != null)
                {
                    await PositionRandomLinemod(player.Units.Where(x => x.Status == UnitStatuses.Placed).ToList(), player, startUnit.PlacePos.X, startUnit.PlacePos.Y).ConfigureAwait(false);
                    // await PositionRandomDistmod(player.Units.Where(x => x.Status == UnitStatuses.Placed).ToList(), player, startUnit.PlacePos.X, startUnit.PlacePos.Y).ConfigureAwait(false);
                }
                else
                {
                    await PositionRandomLinemod(player.Units.Where(x => x.Status == UnitStatuses.Placed).ToList(), player).ConfigureAwait(false);
                    // await PositionRandomDistmod(player.Units.Where(x => x.Status == UnitStatuses.Placed).ToList(), player).ConfigureAwait(false);
                }
            }
        }

 
    }
}
