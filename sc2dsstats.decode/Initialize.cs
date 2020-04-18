using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using sc2dsstats.lib.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace sc2dsstats.decode
{
    public static class Initialize
    {

        public static void Replay(DSReplay replay, bool GetDetails)
        {
            int maxleaver = 0;
            int maxkillsum = 0;
            int minkillsum = -1;
            int minarmy = -1;
            int minincome = -1;

            FixPos(replay);

            Dictionary<string, double> Breakpoints = new Dictionary<string, double>(DSdata.BreakpointMid);
            Breakpoints["ALL"] = replay.DURATION * 22.4;
            foreach (var ent in Breakpoints.Keys.ToArray())
            {
                if (ent == "ALL")
                    continue;
                if (Breakpoints[ent] >= Breakpoints["ALL"])
                    Breakpoints.Remove(ent);
            }

            foreach (DSPlayer pl in replay.DSPlayer)
            {
                int opppos = DBFunctions.GetOpp(pl.REALPOS);
                DSPlayer opp = replay.DSPlayer.SingleOrDefault(s => s.REALPOS == opppos);
                if (opp != null)
                    pl.OPPRACE = opp.RACE;
                if (pl.TEAM == replay.WINNER)
                    pl.WIN = true;
                if (pl.Stats.Any())
                    pl.KILLSUM = pl.Stats.OrderBy(o => o.Gameloop).Last().MineralsKilledArmy;
                else
                    pl.KILLSUM = 0;
                pl.INCOME = (int)pl.Stats.Sum(s => s.MineralsCollectionRate / 9.15);
                foreach (DbRefinery r in pl.Refineries.ToArray())
                    if (r.Gameloop == 0)
                        pl.Refineries.Remove(r);
                pl.GAS = (byte)pl.Refineries.Count();

                int diff = replay.DURATION - pl.PDURATION;
                if (diff > maxleaver)
                    maxleaver = diff;

                if (pl.KILLSUM > maxkillsum)
                    maxkillsum = pl.KILLSUM;

                if (minkillsum == -1)
                    minkillsum = pl.KILLSUM;
                else
                {
                    if (pl.KILLSUM < minkillsum)
                        minkillsum = pl.KILLSUM;
                }

                if (minarmy == -1)
                    minarmy = pl.ARMY;
                else
                {
                    if (pl.ARMY < minarmy)
                        minarmy = pl.ARMY;
                }

                if (minincome == -1)
                    minincome = pl.INCOME;
                else
                {
                    if (pl.INCOME < minincome)
                        minincome = pl.INCOME;
                }
                string urace = pl.RACE;
                if (pl.RACE == "Zagara" || pl.RACE == "Abathur" || pl.RACE == "Kerrigan")
                    urace = "Zerg";
                else if (pl.RACE == "Alarak" || pl.RACE == "Artanis" || pl.RACE == "Vorazun" || pl.RACE == "Fenix" || pl.RACE == "Karax")
                    urace = "Protoss";
                else if (pl.RACE == "Raynor" || pl.RACE == "Swann" || pl.RACE == "Nova" || pl.RACE == "Stukov")
                    urace = "Terran";

                HashSet<string> doubles = new HashSet<string>();
                foreach (DbUpgrade upgrade in pl.Upgrades.OrderBy(o => o.Gameloop).ToList())
                {
                    if (doubles.Contains(upgrade.Upgrade))
                    {
                        pl.Upgrades.Remove(upgrade);
                        continue;
                    }
                    doubles.Add(upgrade.Upgrade);
                    UnitModelBase dupgrade = DSdata.Upgrades.FirstOrDefault(s => s.Name == upgrade.Upgrade && s.Race == pl.RACE);
                    if (dupgrade != null)
                        upgrade.Upgrade = dupgrade.ID.ToString();
                    else
                    {
                        UnitModelBase udupgrade = DSdata.Upgrades.FirstOrDefault(s => s.Name == upgrade.Upgrade && s.Race == urace);
                        if (udupgrade != null)
                            upgrade.Upgrade = udupgrade.ID.ToString();
                        else if (upgrade.Upgrade.StartsWith("Tier4WeaponUpgradeLevel"))
                        {
                            UnitModelBase tudupgrade = DSdata.Upgrades.FirstOrDefault(s => s.Name == upgrade.Upgrade && s.Race == "");
                            if (tudupgrade != null)
                                upgrade.Upgrade = tudupgrade.ID.ToString();
                        }

                    }
                    
                }

                pl.Breakpoints = new List<DbBreakpoint>();
                foreach (var ent in Breakpoints)
                {
                    DbBreakpoint bp = GenBreakpoint(pl, (int)ent.Value, ent.Key);
                    bp.Breakpoint = ent.Key;
                    pl.Breakpoints.Add(bp);
                }
            }

            replay.MAXLEAVER = maxleaver;
            replay.MAXKILLSUM = maxkillsum;
            replay.MINKILLSUM = minkillsum;
            replay.MINARMY = minarmy;
            replay.MININCOME = minincome;

            FixWinner(replay);
            replay.HASH = GenHash(replay);

            using (var md5 = MD5.Create())
            {
                string dirHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetDirectoryName(replay.REPLAYPATH)))).Replace("-", "").ToLowerInvariant();
                string fileHash = BitConverter.ToString(md5.ComputeHash(Encoding.UTF8.GetBytes(Path.GetFileName(replay.REPLAYPATH)))).Replace("-", "").ToLowerInvariant();
                replay.REPLAY = dirHash + fileHash;
            }

            if (GetDetails == false)
            {
                foreach (DSPlayer pl in replay.DSPlayer)
                {
                    pl.Stats.Clear();
                    pl.Stats = null;
                    pl.Spawns.Clear();
                    pl.Spawns = null;
                    pl.decUnits.Clear();
                    pl.decUnits = null;
                    pl.Refineries.Clear();
                    pl.Refineries = null;
                    pl.Upgrades.Clear();
                    pl.Upgrades = null;



                    foreach (DbBreakpoint bp in pl.Breakpoints)
                    {
                        bp.Units.Clear();
                        bp.Units = null;
                        bp.DbUnits.Clear();
                        bp.DbUnits = null;
                        bp.DbUpgrades.Clear();
                        bp.DbUpgrades = null;
                    }
                }
            }

        }

        public static DbBreakpoint GenBreakpoint(DSPlayer pl, int bp, string bpstring)
        {
            DbBreakpoint Bp = new DbBreakpoint();
            Bp.Player = pl;
            Bp.Gas = pl.Refineries.Where(x => x.Gameloop > 0 && x.Gameloop <= bp).Count();
            Bp.Income = (int)pl.Stats.Where(x => x.Gameloop <= bp).Sum(s => s.MineralsCollectionRate / 9.15);
            DbStats statlast = null;
            foreach (DbStats stat in pl.Stats)
            {
                if (stat.Gameloop > bp)
                    break;
                statlast = stat;
            }
            if (statlast != null)
            {
                Bp.Army = statlast.Army;
                Bp.Kills = statlast.MineralsKilledArmy;
                Bp.Upgrades = statlast.MineralsUsedCurrentTechnology;
            }

            List<DbUnit> dbUnits = null;
            foreach (var ent in pl.Spawns)
            {
                if (ent.Gameloop > bp)
                    break;
                dbUnits = ent.Units.ToList();
            }
            Bp.Units = new List<DSUnit>();
            Bp.DbUnits = new List<DbUnit>();
            if (dbUnits != null)
            {
                foreach (DbUnit unit in dbUnits)
                {
                    DSUnit dsUnit = Bp.Units.FirstOrDefault(s => s.Name == unit.Name);
                    if (dsUnit == null)
                    {
                        DSUnit newdsUnit = new DSUnit();
                        newdsUnit.Name = unit.Name;
                        newdsUnit.BP = bpstring;
                        newdsUnit.Breakpoint = Bp;
                        newdsUnit.Count = 1;
                        newdsUnit.DSPlayer = pl;
                        Bp.Units.Add(newdsUnit);
                    }
                    else
                        dsUnit.Count++;
                    unit.Breakpoint = Bp;
                    Bp.DbUnits.Add(unit);
                }
            }

            Bp.DbUpgrades = new List<DbUpgrade>();
            foreach (DbUpgrade upgrade in pl.Upgrades.Where(x => x.Breakpoint == null && x.Gameloop < bp))
            {
                upgrade.Breakpoint = Bp;
                Bp.DbUpgrades.Add(upgrade);
            }

            string dsUnitsString = "";
            string dbUnitsString = "";
            string dbUpgradesString = "";

            foreach (DSUnit unit in Bp.Units)
            {
                string name = unit.Name;
                UnitModelBase bunit = DSdata.Units.FirstOrDefault(s => s.Race == pl.RACE && s.Name == unit.Name);
                if (bunit != null)
                    name = bunit.ID.ToString();
                dsUnitsString += name + "," + unit.Count + "|";
            }
            if (dsUnitsString.Any())
                dsUnitsString = dsUnitsString.Remove(dsUnitsString.Length - 1);

            foreach (DbUnit unit in Bp.DbUnits)
            {
                string name = unit.Name;
                UnitModelBase bunit = DSdata.Units.FirstOrDefault(s => s.Race == pl.RACE && s.Name == unit.Name);
                if (bunit != null)
                    name = bunit.ID.ToString();
                dbUnitsString += name + "," + unit.BornX + "," + unit.BornY + "|";
            }
            if (dbUnitsString.Any())
                dbUnitsString = dbUnitsString.Remove(dbUnitsString.Length - 1);

            
            foreach (DbUpgrade upgrade in Bp.DbUpgrades)
                dbUpgradesString += upgrade.Upgrade + "|";
            if (dbUpgradesString.Any())
                dbUpgradesString = dbUpgradesString.Remove(dbUpgradesString.Length - 1);

            Bp.dsUnitsString = dsUnitsString;
            Bp.dbUnitsString = dbUnitsString;
            Bp.dbUpgradesString = dbUpgradesString;

            return Bp;
        }



        public static void FixPos(DSReplay replay)
        {
            foreach (DSPlayer pl in replay.DSPlayer)
            {
                if (pl.REALPOS == 0)
                {
                    for (int j = 1; j <= 6; j++)
                    {
                        if (replay.PLAYERCOUNT == 2 && (j == 2 || j == 3 || j == 5 || j == 6)) continue;
                        if (replay.PLAYERCOUNT == 4 && (j == 3 || j == 6)) continue;

                        List<DSPlayer> temp = new List<DSPlayer>(replay.DSPlayer.Where(x => x.REALPOS == j).ToList());
                        if (temp.Count == 0)
                        {
                            pl.REALPOS = (byte)j;
                        }
                    }
                    if (new List<DSPlayer>(replay.DSPlayer.Where(x => x.REALPOS == pl.POS).ToList()).Count == 0) pl.REALPOS = pl.POS;
                }

                if (new List<DSPlayer>(replay.DSPlayer.Where(x => x.REALPOS == pl.REALPOS).ToList()).Count > 1)
                {
                    Console.WriteLine("Found double playerid for " + pl.POS + "|" + pl.REALPOS);

                    for (int j = 1; j <= 6; j++)
                    {
                        if (replay.PLAYERCOUNT == 2 && (j == 2 || j == 3 || j == 5 || j == 6)) continue;
                        if (replay.PLAYERCOUNT == 4 && (j == 3 || j == 6)) continue;
                        if (new List<DSPlayer>(replay.DSPlayer.Where(x => x.REALPOS == j).ToList()).Count == 0)
                        {
                            pl.REALPOS = (byte)j;
                            break;
                        }
                    }
                }
            }
        }

        public static void FixWinner(DSReplay replay)
        {
            if (replay.WINNER < 0)
            {
                foreach (DSPlayer pl in replay.DSPlayer)
                {
                    if (pl.RESULT == 1)
                    {
                        replay.WINNER = (sbyte)pl.TEAM;
                        break;
                    }
                }
            }

            foreach (DSPlayer pl in replay.DSPlayer)
            {
                if (pl.TEAM == replay.WINNER) pl.RESULT = 1;
                else pl.RESULT = 2;
            }
        }

        public static string GenHash(DSReplay replay)
        {
            string md5 = "";
            string hashstring = "";
            foreach (DSPlayer pl in replay.DSPlayer.OrderBy(o => o.POS))
            {
                hashstring += pl.POS + pl.RACE;
            }
            hashstring += replay.MINARMY + replay.MINKILLSUM + replay.MININCOME + replay.MAXKILLSUM;
            using (MD5 md5Hash = MD5.Create())
            {
                md5 = GetMd5Hash(md5Hash, hashstring);
            }
            return md5;
        }

        static string GetMd5Hash(MD5 md5Hash, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = md5Hash.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            StringBuilder sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}
