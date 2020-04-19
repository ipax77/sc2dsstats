using paxgamelib.Data;
using paxgamelib.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace paxgamelib.Models
{
    [Serializable]
    public class BBuild
    {
        public HashSet<UnitAbilities> Abilities { get; set; } = new HashSet<UnitAbilities>();
        public HashSet<UnitUpgrades> Upgrades { get; set; } = new HashSet<UnitUpgrades>();
        public List<KeyValuePair<int, int>> UpgradesLevel { get; set; } = new List<KeyValuePair<int, int>>();
        public List<KeyValuePair<int, int>> Units { get; set; } = new List<KeyValuePair<int, int>>();
        public List<KeyValuePair<int, Vector2Ser>> Position { get; set; } = new List<KeyValuePair<int, Vector2Ser>>();
        public List<int> AImoves { get; set; } = new List<int>();
        public string Name { get; set; }
        public int Pos { get; set; }
        public int Race { get; set; }
        public int MineralsCurrent { get; set; }
        public int Tier { get; set; } = 1;

        public BBuild()
        {

        }

        public BBuild(Player player) : this()
        {
            this.GetBuild(player);
        }

        ///<summary>
        ///Restore player from this
        ///</summary>
        public void SetBuild(Player pl)
        {
            pl.SoftReset();
            lock (this)
            {
                foreach (var ent in Abilities)
                    pl.MineralsCurrent += UnitService.AbilityUpgradeUnit(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == ent), pl);

                foreach (var ent in Upgrades)
                    pl.MineralsCurrent += UnitService.UpgradeUnit(ent, pl);

                foreach (var ent in Units)
                {
                    if (ent.Value > 0)
                    {
                        for (int i = 0; i < ent.Value; i++)
                        {
                            Unit unit = UnitPool.Units.SingleOrDefault(x => x.ID == ent.Key).DeepCopy();
                            var positions = Position.Where(x => x.Key == unit.ID).ToList();
                            if (positions.Count() >= i)
                            {
                                Vector2Ser vec = positions.ElementAt(i).Value;
                                unit.PlacePos = new Vector2(vec.x, vec.y);
                                UnitService.NewUnit(pl, unit);
                                pl.Units.Add(unit);
                                pl.MineralsCurrent += unit.Cost;
                            }
                        }
                    }
                }
            }
        }

        ///<summary>
        ///Save Player to this
        ///</summary>
        public BBuild GetBuild(Player pl)
        {

            Abilities = new HashSet<UnitAbilities>();
            Upgrades = new HashSet<UnitUpgrades>();
            UpgradesLevel = new List<KeyValuePair<int, int>>();
            Units = new List<KeyValuePair<int, int>>();
            Position = new List<KeyValuePair<int, Vector2Ser>>();


            Dictionary<int, int> bunits = new Dictionary<int, int>();
            foreach (Unit unit in pl.Units.Where(x => x.Status != UnitStatuses.Available))
            {
                Unit defaultunit = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name);
                if (defaultunit == null)
                    defaultunit = UnitPool.Units.SingleOrDefault(x => x.Name == "NA");
                int id = defaultunit.ID;
                if (!bunits.ContainsKey(id))
                    bunits[id] = 1;
                else
                    bunits[id]++;

                Vector2Ser vec = new Vector2Ser();
                vec.x = unit.PlacePos.X;
                vec.y = unit.PlacePos.Y;
                this.Position.Add(new KeyValuePair<int, Vector2Ser>(id, vec));
            }

            foreach (var ent in bunits)
                this.Units.Add(new KeyValuePair<int, int>(ent.Key, ent.Value));

            foreach (UnitAbility ability in pl.AbilityUpgrades)
                this.Abilities.Add(ability.Ability);

            foreach (UnitUpgrade upgrade in pl.Upgrades)
            {
                this.Upgrades.Add(upgrade.Upgrade);
                this.UpgradesLevel.Add(new KeyValuePair<int, int>(UpgradePool.Upgrades.SingleOrDefault(x => x.Race == pl.Race && x.Name == upgrade.Upgrade).ID, upgrade.Level));
            }

            this.Name = pl.Name;
            this.Pos = pl.Pos;
            this.Race = (int)pl.Race;
            this.MineralsCurrent = pl.MineralsCurrent;
            this.Tier = pl.Tier;
            return this;
        }

        public string GetString(Player pl)
        {
            string build = "";
            foreach (Unit unit in pl.Units.Where(x => x.Status == UnitStatuses.Spawned || x.Status == UnitStatuses.Placed))
            {
                Unit defaultunit = UnitPool.Units.SingleOrDefault(x => x.Name == unit.Name);
                if (defaultunit == null)
                    defaultunit = UnitPool.Units.SingleOrDefault(x => x.Name == "NA");
                int id = defaultunit.ID;

                Vector2 pos = new Vector2(unit.PlacePos.Y, unit.PlacePos.X);
                build += String.Format("{0}|{1}|{2},", id, pos.X, pos.Y);
            }

            //for (int i = 0; i < 50 - pl.Units.Count; i++)
            //    build += String.Format("{0}|{1}|{2},", 0, 0, 0);

            foreach (UnitUpgrade upgrade in pl.Upgrades)
            {
                build += String.Format("{0}|{1},", (int)upgrade.Upgrade, upgrade.Level);
            }

            //for (int i = 0; i < 5 - pl.Upgrades.Count; i++)
            //    build += String.Format("{0}|{1},", 0, 0);

            foreach (UnitAbility ability in pl.AbilityUpgrades)
            {
                build += String.Format("{0},", (int)ability.Ability);
            }

            //for (int i = 0; i < 5 - pl.AbilityUpgrades.Count; i++)
            //    build += String.Format("{0},", 0);

            if (build.Any())
                build = build.Remove(build.Length - 1, 1);

            return build;
        }

        public void SetString(string build, Player pl)
        {
            if (build == null || build == "")
                return;

            pl.SoftReset();

            var ents = build.Split(",");
            foreach (var ent in ents)
            {

                if (ent.Count(x => x == '|') > 1)
                {
                    var unitents = ent.Split('|');
                    Unit unit = UnitPool.Units.SingleOrDefault(x => x.ID == int.Parse(unitents[0]));
                    if (unit != null)
                    {
                        Unit myunit = unit.DeepCopy();
                        myunit.PlacePos = new Vector2(float.Parse(unitents[2]), float.Parse(unitents[1]));
                        UnitService.NewUnit(pl, myunit);
                        pl.Units.Add(myunit);
                        pl.MineralsCurrent += myunit.Cost;
                    }
                }
                else if (ent.Count(x => x == '|') == 1)
                {
                    var upgradeents = ent.Split('|');
                    UnitUpgrade upgrade = new UnitUpgrade();
                    upgrade.Upgrade = (UnitUpgrades)int.Parse(upgradeents[0]);
                    upgrade.Level = int.Parse(upgradeents[1]);
                    pl.MineralsCurrent += UnitService.UpgradeUnit(upgrade.Upgrade, pl);
                }
                else
                {
                    int ua = -1;
                    if (int.TryParse(ent, out ua))
                    {
                        UnitAbility myability = AbilityPool.Abilities.SingleOrDefault(x => x.Ability == (UnitAbilities)ua).DeepCopy();
                        pl.MineralsCurrent += UnitService.AbilityUpgradeUnit(myability, pl);
                    }
                }
            }
        }

        public List<int> GetAIMoves(Player pl)
        {
            float[][] fmatrix = new float[20][];
            for (int i = 0; i < 20; i++)
            {
                fmatrix[i] = new float[51];
            }

            foreach (var ent in pl.AbilityUpgrades)
                if (!AImoves.Contains(3000 + 1 + (int)ent.Ability))
                    AImoves.Add(3000 + 1 + (int)ent.Ability);

            foreach (var ent in pl.Upgrades)
                if (!AImoves.Contains(3000 + 4 + (int)ent.Upgrade))
                    AImoves.Add(3000 + 4 + (int)ent.Upgrade);

            foreach (var ent in pl.Units.Where(x => x.Status != UnitStatuses.Available))
            {
                int ui = UnitPool.Units.SingleOrDefault(x => x.Name == ent.Name).ID - 1;
                int uj = (int)(ent.PlacePos.Y * 2);
                int uk = (int)(ent.PlacePos.X * 2);
                int num = 0;
                for (int i = 0; i < 3; i++)
                    for (int j = 0; j < 20; j++)
                        for (int k = 0; k < 50; k++)
                        {
                            if (ui == i && uj == j && uk == k)
                                if (!AImoves.Contains(num))
                                    AImoves.Add(num);
                            num++;
                        }
            }
            int mins = 0;


            bool Test = false;
            if (Test)
            {
                foreach (var ent in AImoves)
                    mins += UnitService.ActionToMove(ent);
                Player plbab = pl.Deepcopy();
                BBuild bab = new BBuild(plbab);
                bab.SetString(bab.GetString(plbab), plbab);
                if (mins != plbab.MineralsCurrent)
                    Console.WriteLine("A2: " + mins + " <=> " + plbab.MineralsCurrent);
            }

            return AImoves;
        }

        public void SetAIMoves(List<int> moves, Player pl)
        {
            pl.SoftReset();

            if (moves.Contains(3001))
                pl.MineralsCurrent += UnitService.AbilityUpgradeUnit(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.Stimpack), pl);
            if (moves.Contains(3002))
                pl.MineralsCurrent += UnitService.AbilityUpgradeUnit(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.CombatShield), pl);
            if (moves.Contains(3003))
                pl.MineralsCurrent += UnitService.AbilityUpgradeUnit(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.ConcussiveShells), pl);
            if (moves.Contains(3004))
                pl.MineralsCurrent += UnitService.UpgradeUnit(UnitUpgrades.GroundAttac, pl);
            if (moves.Contains(3005))
                pl.MineralsCurrent += UnitService.UpgradeUnit(UnitUpgrades.GroundArmor, pl);

            int num = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 20; j++)
                    for (int k = 0; k < 50; k++)
                    {
                        if (moves.Contains(num))
                        {
                            Unit unit = UnitPool.Units.SingleOrDefault(x => x.ID == i + 1);
                            if (unit != null)
                            {
                                Unit myunit = unit.DeepCopy();
                                myunit.PlacePos = new Vector2((float)k / 2, (float)j / 2);
                                UnitService.NewUnit(pl, myunit);
                                pl.Units.Add(myunit);
                                pl.MineralsCurrent += myunit.Cost;
                            }
                        }
                        num++;
                    }
        }

        public void AddAIMove(int move, Player pl)
        {
            int myminerals = 0;
            if (move == 3001)
                myminerals = UnitService.AbilityUpgradeUnit(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.Stimpack), pl);
            if (move == 3002)
                myminerals = UnitService.AbilityUpgradeUnit(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.CombatShield), pl);
            if (move == 3003)
                myminerals = UnitService.AbilityUpgradeUnit(AbilityPool.Abilities.SingleOrDefault(x => x.Ability == UnitAbilities.ConcussiveShells), pl);
            if (move == 3004)
                myminerals = UnitService.UpgradeUnit(UnitUpgrades.GroundAttac, pl);
            if (move == 3005)
                myminerals = UnitService.UpgradeUnit(UnitUpgrades.GroundArmor, pl);

            int num = 0;
            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 20; j++)
                    for (int k = 0; k < 50; k++)
                    {
                        if (move == num)
                        {
                            Unit unit = UnitPool.Units.SingleOrDefault(x => x.ID == i + 1);
                            if (unit != null)
                            {
                                Unit myunit = unit.DeepCopy();
                                myunit.PlacePos = new Vector2((float)k / 2, (float)j / 2);
                                UnitService.NewUnit(pl, myunit);
                                pl.Units.Add(myunit);
                                myminerals = myunit.Cost;
                            }
                        }
                        num++;
                    }
            pl.MineralsCurrent -= myminerals;
        }


        public bool Test(Player pl)
        {
            Player testpl = new Player();
            testpl.Game = new GameHistory();
            testpl.SetBuild(this);
            int m1 = testpl.MineralsCurrent;
            testpl.SetAIMoves(this.GetAIMoves(pl));
            int m2 = testpl.MineralsCurrent;
            testpl.SetString(this.GetString(pl));
            int m3 = testpl.MineralsCurrent;

            if (m1 == m2 && m2 == m3)
                return true;
            else
                return false;
        }

    }



    public class BBuildJob
    {
        public string PlayerBuild { get; set; }
        public string OppBuild { get; set; }
        public int Minerals { get; set; }
    }

    public class RESTResult
    {
        public int Result { get; set; }
        public double DamageP1 { get; set; }
        public double MinValueP1 { get; set; }
        public double DamageP2 { get; set; }
        public double MinValueP2 { get; set; }

        public void Deconstruct()
        {
        }
    }
}
