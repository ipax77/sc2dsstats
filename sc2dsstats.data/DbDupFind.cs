using Microsoft.EntityFrameworkCore;
using sc2dsstats.decode.Models;
using sc2dsstats.decode.Service;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace sc2dsstats.data
{
    public static class DbDupFind
    {
        private static List<dsreplay> sReplays = new List<dsreplay>();
        private static HashSet<string> Hashs = new HashSet<string>();

        public static int Scan()
        {
            List<FileInfo> NewJsons = new List<FileInfo>();
            foreach (var dir in Directory.GetDirectories(DSdata.ServerConfig.SumDir).Where(x => Path.GetFileName(x).Length == 64))
                foreach (var file in Directory.GetFiles(dir).Where(d => new FileInfo(d).LastWriteTime > DSdata.ServerConfig.LastRun).Select(s => new FileInfo(s)))
                    NewJsons.Add(file);

            if (!NewJsons.Any())
            {
                Console.WriteLine("No new datafiles found.");
                return 0;
            }

            foreach (var file in NewJsons.OrderBy(o => o.LastWriteTime))
                ReadJson(file.FullName);

            Console.WriteLine("New Replays found: " + sReplays.Count);

            List<DSReplay> NewDBReps = InsertReplays();

            Console.WriteLine("Replays added: " + NewDBReps.Count + " (" + sReplays.Count + ")");

            Dictionary<int, List<int>> CompareDups = FindDbDups(NewDBReps);

            Console.WriteLine("Dups found: " + CompareDups.Count);

            HashSet<int> DeleteMe = CheckDups(CompareDups);

            Console.WriteLine("Deleting: " + DeleteMe.Count);

            Delete(DeleteMe);
            return sReplays.Count;
        }

        public static List<DSReplay> InsertReplays()
        {
            List<DSReplay> NewDbReps = new List<DSReplay>();
            List<DSReplay> Dups = new List<DSReplay>();
            using (var context = new DSReplayContext(Program._opt))
            {
                foreach (dsreplay replay in sReplays.ToArray())
                {
                    DSReplay crep = context.DSReplays
                        .Include(p => p.DSPlayer)
                        .SingleOrDefault(s => s.HASH == replay.HASH);
                    if (crep != null)
                    {
                        if (new Version(replay.VERSION) > new Version(crep.VERSION))
                        {
                            foreach (var ent in crep.DSPlayer.Select(s => new { s.NAME, s.REALPOS }))
                            {
                                try
                                {
                                    if (ent.NAME.Length == 64 && replay.PLAYERS.Single(x => x.REALPOS == ent.REALPOS).NAME.Length < 64)
                                    {
                                        replay.PLAYERS.Single(x => x.REALPOS == ent.REALPOS).NAME = ent.NAME;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine("???");
                                }
                            }
                            DBService.DeleteRep(context, crep.ID);
                        }
                        else
                        {
                            int i = 0;
                            foreach (var ent in replay.PLAYERS.Select(s => new { s.NAME, s.REALPOS }))
                            {

                                try
                                {
                                    if (ent.NAME.Length == 64 && crep.DSPlayer.Single(x => x.REALPOS == ent.REALPOS).NAME.Length < 64)
                                    {
                                        i++;
                                        crep.DSPlayer.Single(x => x.REALPOS == ent.REALPOS).NAME = ent.NAME;
                                    }
                                }
                                catch
                                {
                                    Console.WriteLine(":(");
                                }
                            }
                            if (i > 0)
                                context.SaveChanges();
                            sReplays.Remove(replay);
                        }
                    }
                }

                foreach (dsreplay rep in sReplays)
                {
                    DSReplay newdbrep = Map.Rep(rep);
                    newdbrep.REPLAYPATH = "";
                    DBService.SaveReplay(context, newdbrep);
                    NewDbReps.Add(newdbrep);
                }
            }

            return NewDbReps;
        }

        public static void ReadJson(string file)
        {
            Console.WriteLine("Working on " + Path.GetFileName(file));
            string filename = Path.GetFileNameWithoutExtension(file);
            string plhash = filename.Substring(filename.Length - 64);
            using (var context = new DSReplayContext(Program._opt))
            {
                foreach (var line in File.ReadAllLines(file, Encoding.UTF8))
                {

                    dsreplay replay = System.Text.Json.JsonSerializer.Deserialize<dsreplay>(line);
                    if (replay != null)
                    {
                        replay.Init();
                        replay.GenHash();

                        dsplayer pl = replay.PLAYERS.FirstOrDefault(s => s.NAME == "player");
                        if (pl != null)
                        {
                            pl.NAME = plhash;
                            replay.PLDupPos[plhash] = pl.REALPOS;
                        }

                        dsreplay crep = sReplays.SingleOrDefault(s => s.HASH == replay.HASH);

                        if (crep == null)
                        {
                            sReplays.Add(replay);
                            Hashs.Add(replay.HASH);
                        }
                        else
                        {
                            if (new Version(replay.VERSION) >= new Version(crep.VERSION))
                            {
                                foreach (var ent in crep.PLAYERS.Select(s => new { s.NAME, s.REALPOS }))
                                {
                                    try
                                    {
                                        if (ent.NAME.Length == 64 && replay.PLAYERS.Single(x => x.REALPOS == ent.REALPOS).NAME.Length < 64)
                                            replay.PLAYERS.Single(x => x.REALPOS == ent.REALPOS).NAME = ent.NAME;
                                    }
                                    catch
                                    {
                                        Console.WriteLine("???");
                                    }
                                }
                                sReplays.Remove(crep);
                                sReplays.Add(replay);
                            }
                            else
                            {
                                foreach (var ent in replay.PLAYERS.Select(s => new { s.NAME, s.REALPOS }))
                                {
                                    try
                                    {
                                        if (ent.NAME.Length == 64 && crep.PLAYERS.Single(x => x.REALPOS == ent.REALPOS).NAME.Length < 64)
                                            crep.PLAYERS.Single(x => x.REALPOS == ent.REALPOS).NAME = ent.NAME;
                                    }
                                    catch
                                    {
                                        Console.WriteLine(":(");
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public static void Delete(HashSet<int> DeleteMe)
        {
            Console.WriteLine("Deleting: " + DeleteMe.Count);
            using (var context = new DSReplayContext(Program._opt))
            {
                foreach (int id in DeleteMe)
                    DBService.DeleteRep(context, id);
            }
        }
        public static HashSet<int> CheckDups(Dictionary<int, List<int>> CompareMe)
        {
            HashSet<int> DeleteMe = new HashSet<int>();
            int i = 0;
            int c = CompareMe.Count;
            using (var context = new DSReplayContext(Program._opt))
            {
                foreach (int id in CompareMe.Keys)
                {
                    i++;
                    if (i % 100 == 0)
                        Console.WriteLine($"{i}/{c}");

                    if (DeleteMe.Contains(id))
                        continue;



                    DSReplay replay = context.DSReplays.Single(s => s.ID == id);

                    foreach (int cid in CompareMe[id])
                    {
                        if (DeleteMe.Contains(cid))
                            continue;
                        DSReplay crep = context.DSReplays.Single(s => s.ID == cid);
                        int isDupPossible = 0;

                        if (replay.REPLAY == crep.REPLAY)
                        {
                            if (new Version(replay.VERSION) >= new Version(crep.VERSION))
                                DeleteMe.Add(crep.ID);
                            else
                                DeleteMe.Add(replay.ID);
                            continue;
                        }

                        if (replay.VERSION == crep.VERSION)
                        {
                            if (replay.MAXKILLSUM == crep.MAXKILLSUM) isDupPossible++;
                            if (replay.MAXLEAVER == crep.MAXLEAVER) isDupPossible++;
                            if (replay.MININCOME == crep.MININCOME) isDupPossible++;
                            if (replay.MINARMY == crep.MINARMY) isDupPossible++;
                        }
                        else
                        {
                            int kdiff = Math.Abs(replay.MAXKILLSUM - crep.MAXKILLSUM);
                            //int adiff = Math.Abs(rep.MINARMY - crep.MINARMY);
                            int mdiff = Math.Abs(replay.MINKILLSUM - crep.MINKILLSUM);

                            if (kdiff < 1000 && mdiff < 1000)
                                isDupPossible = 4;
                        }

                        if (isDupPossible > 2)
                        {
                            replay = context.DSReplays
                                .Include(p => p.DSPlayer)
                                .Single(s => s.ID == id);
                            crep = context.DSReplays
                                .Include(p => p.DSPlayer)
                                .Single(s => s.ID == cid);

                            if (new Version(replay.VERSION) >= new Version(crep.VERSION))
                            {
                                foreach (var ent in crep.DSPlayer.Select(s => new { s.NAME, s.REALPOS }))
                                {
                                    try
                                    {
                                        if (ent.NAME.Length == 64 && replay.DSPlayer.Single(x => x.REALPOS == ent.REALPOS).NAME.Length < 64)
                                            replay.DSPlayer.Single(x => x.REALPOS == ent.REALPOS).NAME = ent.NAME;
                                    }
                                    catch
                                    {
                                        Console.WriteLine("???");
                                    }
                                }
                                DeleteMe.Add(crep.ID);
                            }
                            else
                            {
                                foreach (var ent in replay.DSPlayer.Select(s => new { s.NAME, s.REALPOS }))
                                {
                                    try
                                    {
                                        if (ent.NAME.Length == 64 && crep.DSPlayer.Single(x => x.REALPOS == ent.REALPOS).NAME.Length < 64)
                                            crep.DSPlayer.Single(x => x.REALPOS == ent.REALPOS).NAME = ent.NAME;
                                    }
                                    catch
                                    {
                                        Console.WriteLine(":(");
                                    }
                                }
                                DeleteMe.Add(crep.ID);
                            }
                            context.SaveChanges();
                        }
                    }
                }
            }
            return DeleteMe;
        }
        public static Dictionary<int, List<int>> FindDbDups(List<DSReplay> newreplays)
        {
            HashSet<int> repsDeleted = new HashSet<int>();
            Dictionary<int, List<int>> CompareMe = new Dictionary<int, List<int>>();

            using (var context = new DSReplayContext(Program._opt))
            {
                //var replays = context.DSReplays.Include(p => p.DSPlayer);

                int i = 0;
                foreach (DSReplay rep in newreplays.ToArray())
                {
                    if (repsDeleted.Contains(rep.ID))
                        continue;

                    i++;
                    List<string> repRaces = new List<string>(rep.DSPlayer.OrderBy(o => o.REALPOS).Select(s => s.RACE));

                    var compreps = context.DSReplays.Where(x => x.GAMETIME > rep.GAMETIME.AddDays(-2)
                    && x.GAMETIME < rep.GAMETIME.AddDays(2)
                    && x.DURATION > rep.DURATION - TimeSpan.FromMinutes(2).TotalSeconds
                    && x.DURATION < rep.DURATION + TimeSpan.FromMinutes(2).TotalSeconds
                    && x.ID != rep.ID);

                    var rreps = from r in compreps.Include(p => p.DSPlayer)
                                from p in r.DSPlayer
                                select new
                                {
                                    r.ID,
                                    p.REALPOS,
                                    p.RACE
                                };

                    if (compreps.Any())
                    {
                        HashSet<int> ids = rreps.Select(s => s.ID).ToHashSet();
                        foreach (int id in ids)
                        {
                            List<string> compRaces = rreps.Where(x => x.ID == id).OrderBy(o => o.REALPOS).Select(s => s.RACE).ToList();
                            if (repRaces.SequenceEqual(compRaces))
                            {
                                if (!CompareMe.ContainsKey(rep.ID))
                                    CompareMe[rep.ID] = new List<int>();
                                CompareMe[rep.ID].Add(id);
                            }
                        }
                    }


                    if (i % 100 == 0)
                        Console.WriteLine($"{i}: comparereps => {CompareMe.Count}; searching: {compreps.Count()}");

                }
                return CompareMe;

            }
        }
        
    }
}
