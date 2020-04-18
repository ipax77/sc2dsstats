using System.Text.Json;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using sc2dsstats.lib.Service;
using System.Net.NetworkInformation;
using System.Linq;
using System.Threading;

namespace sc2dsstats.data.Service
{
    

    public static class DataService
    {
        public static Dictionary<string, List<int>> DSHash = new Dictionary<string, List<int>>();
        public static TimeSpan MaxGametimeDiff = TimeSpan.Zero;
        public static TimeSpan MaxDurationDiff = TimeSpan.Zero;

        public static List<DSReplay> ReadJson(string file)
        {
            List<DSReplay> DSReplays = new List<DSReplay>();
            HashSet<string> ReplayHash = new HashSet<string>();

            string plhash = Path.GetFileNameWithoutExtension(file);
            
            Console.WriteLine("Working on " + plhash);

            if (File.Exists(file))
            {
                foreach (string line in File.ReadAllLines(file))
                {
                    dsreplay rep = JsonSerializer.Deserialize<dsreplay>(line);
                    if (ReplayHash.Contains(rep.REPLAY))
                        continue;
                    DSReplay dsrep = Map.Rep(rep, plhash);
                    ReplayHash.Add(rep.REPLAY);
                    DSReplays.Add(dsrep);
                }
            }
            return DSReplays;
        }

        public static List<DSReplay> FindDups (List<DSReplay> DSReplays)
        {
            Console.WriteLine("Dup find ..");
            List<DSReplay> RemoveReps = new List<DSReplay>();
            int i = 0;
            foreach (DSReplay rep in DSReplays.ToArray())
            {
                if (RemoveReps.Contains(rep))
                    continue;
                RemoveReps.AddRange(CheckDup(rep, DSReplays));
                foreach (DSReplay rrep in RemoveReps.ToArray())
                    if (DSReplays.Contains(rrep))
                        DSReplays.Remove(rep);
                if (i % 100 == 0)
                    Console.WriteLine($"{i}/{DSReplays.Count} {RemoveReps.Count}");
                i++;
            }
            Console.WriteLine("MaxGametimeDiff: " + MaxGametimeDiff.TotalSeconds);
            Console.WriteLine("MaxDurationDiff: " + MaxDurationDiff.TotalSeconds);

            Console.WriteLine("Removed: " + RemoveReps.Count);
            return DSReplays;
        }

        public static List<DSReplay> CheckDup (DSReplay rep, List<DSReplay> DSReplays)
        {
            List<DSReplay> RemoveReps = new List<DSReplay>();
            var compreps = DSReplays.Where(x => 
                   x.GAMETIME > rep.GAMETIME.AddDays(-1)
                && x.GAMETIME < rep.GAMETIME.AddDays(1)
                && x.DURATION > rep.DURATION - TimeSpan.FromMinutes(1).TotalSeconds
                && x.DURATION < rep.DURATION + TimeSpan.FromMinutes(1).TotalSeconds
            ).ToList();
            compreps.Remove(rep);

            List<string> Races = rep.DSPlayer.OrderBy(o => o.REALPOS).Select(s => s.RACE).ToList();
            foreach (DSReplay crep in compreps)
            {
                List<string> compRaces = crep.DSPlayer.OrderBy(o => o.REALPOS).Select(s => s.RACE).ToList();
                if (Races.SequenceEqual(compRaces))
                    if (IsDup(rep, crep))
                        RemoveReps.Add(MergeReps(rep, crep));
            }
            return RemoveReps;
        }

        public static DSReplay MergeReps(DSReplay rep, DSReplay crep)
        {
            if (new Version(rep.VERSION) > new Version(crep.VERSION))
            {
                foreach (var ent in crep.DSPlayer.Select(s => new { s.NAME, s.REALPOS }))
                {
                    try
                    {
                        if (ent.NAME.Length == 64 && rep.DSPlayer.Single(x => x.REALPOS == ent.REALPOS).NAME.Length < 64)
                            rep.DSPlayer.Single(x => x.REALPOS == ent.REALPOS).NAME = ent.NAME;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("???" + e.Message);
                    }
                }
                return crep;
            }
            else
            {
                foreach (var ent in rep.DSPlayer.Select(s => new { s.NAME, s.REALPOS }))
                {
                    try
                    {
                        if (ent.NAME.Length == 64 && crep.DSPlayer.Single(x => x.REALPOS == ent.REALPOS).NAME.Length < 64)
                            crep.DSPlayer.Single(x => x.REALPOS == ent.REALPOS).NAME = ent.NAME;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(":( " + e.Message);
                    }
                }
                return rep;
            }
        }

        public static bool IsDup(DSReplay rep, DSReplay crep)
        {
            int isDupPossible = 0;
            if (rep.VERSION == crep.VERSION)
            {
                if (rep.MAXKILLSUM == crep.MAXKILLSUM) isDupPossible++;
                if (rep.MAXLEAVER == crep.MAXLEAVER) isDupPossible++;
                if (rep.MININCOME == crep.MININCOME) isDupPossible++;
                if (rep.MINARMY == crep.MINARMY) isDupPossible++;
            }
            else
            {
                int kdiff = Math.Abs(rep.MAXKILLSUM - crep.MAXKILLSUM);
                //int adiff = Math.Abs(rep.MINARMY - crep.MINARMY);
                int mdiff = Math.Abs(rep.MINKILLSUM - crep.MINKILLSUM);
                int ddiff = Math.Abs(rep.DURATION - crep.DURATION);

                if (ddiff == 0 || (kdiff < 1000 && mdiff < 1000))
                    isDupPossible = 4;
            }

            if (isDupPossible > 2)
            {
                TimeSpan t = rep.GAMETIME - crep.GAMETIME;
                if (t > MaxGametimeDiff)
                    MaxGametimeDiff = t;
                TimeSpan d = TimeSpan.FromSeconds(Math.Abs(rep.DURATION - crep.DURATION));
                if (d > MaxDurationDiff)
                    MaxDurationDiff = d;
                return true;
            }
            else
                return false;
        }

        public static List<DSReplay> Insert(List<DSReplay> DSReplays)
        {



            return DSReplays;
        }

    }
}
