using Microsoft.EntityFrameworkCore;
using sc2dsstats.decode.Models;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using sc2dsstats.lib.Service;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace sc2dsstats.data
{
    public static class DBScan
    {
        private static List<dsreplay> sReplays = new List<dsreplay>();
        private static HashSet<string> Hashs = new HashSet<string>();

        public static void Scan()
        {
            List<FileInfo> NewJsons = new List<FileInfo>();
            foreach (var dir in Directory.GetDirectories(Program.Config.SumDir).Where(x => Path.GetFileName(x).Length == 64))
                foreach (var file in Directory.GetFiles(dir).Where(d => new FileInfo(d).LastWriteTime > Program.Config.LastRun).Select(s => new FileInfo(s)))
                    NewJsons.Add(file);

            if (!NewJsons.Any())
            {
                Console.WriteLine("No new datafiles found.");
                return;
            }

            foreach (var file in NewJsons.OrderBy(o => o.LastWriteTime))
                ReadJson(file.FullName);

            Console.WriteLine("New Replays found: " + sReplays.Count); 
            DupFind();

        }

        public static void ReadJson(string file)
        {
            Console.WriteLine("Working on " + Path.GetFileName(file));

            using (var context = new DSReplayContext())
            {
                foreach (var line in File.ReadAllLines(file, Encoding.UTF8))
                {

                    dsreplay replay = JsonSerializer.Deserialize<dsreplay>(line);
                    if (replay != null)
                    {
                        replay.Init();
                        replay.GenHash();
                        replay.ID = 0;

                        string filename = Path.GetFileNameWithoutExtension(file);
                        string plhash = filename.Substring(filename.Length - 64);
                        int plpos = replay.PLAYERS.FindIndex(s => s.NAME == "player");
                        if (replay.PLAYERS.Where(x => x.NAME == "player").Count() == 1)
                        {
                            replay.PLDupPos[plhash] = plpos;
                        }

                        DSReplay crep = context.DSReplays
                            .Include(p => p.PLDuplicate)
                            .SingleOrDefault(x => x.HASH == replay.HASH);
                        dsreplay screp = null;
                        if (crep == null)
                            screp = sReplays.SingleOrDefault(s => s.HASH == replay.HASH);

                        if (crep == null && screp == null)
                        {
                            sReplays.Add(replay);
                            Hashs.Add(replay.HASH);
                        }
                        else
                        {
                            if (crep == null)
                                foreach (var ent in replay.PLDupPos)
                                    screp.PLDupPos[ent.Key] = ent.Value;
                            else {
                                PLDuplicate dup = crep.PLDuplicate.SingleOrDefault(x => x.Hash == plhash);
                                if (dup == null)
                                {
                                    PLDuplicate newdup = new PLDuplicate();
                                    newdup.Hash = plhash;
                                    newdup.Pos = plpos;
                                    newdup.DSReplay = crep;
                                    context.PLDuplicates.Add(newdup);
                                    context.SaveChanges();
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void DupFind()
        {
            Console.WriteLine("Dup find");
            int d1 = 0;
            int d2 = 0;
            int i = 0;
            int m = 0;
            int r = 0;
            using (var context = new DSReplayContext())
            {

                foreach (dsreplay replay in sReplays.OrderBy(o => o.GAMETIME).ToArray())
                {
                    string gametime = replay.GAMETIME.ToString();
                    int year = int.Parse(gametime.Substring(0, 4));
                    int month = int.Parse(gametime.Substring(4, 2));
                    int day = int.Parse(gametime.Substring(6, 2));
                    int hour = int.Parse(gametime.Substring(8, 2));
                    int min = int.Parse(gametime.Substring(10, 2));
                    int sec = int.Parse(gametime.Substring(12, 2));
                    DateTime gtime = new DateTime(year, month, day, hour, min, sec);

                    List<DSReplay> cReplays = new List<DSReplay>(context.DSReplays
                        .Include(p => p.DSPlayer)
                        .Include(p => p.PLDuplicate)
                        .Where(x => x.GAMETIME > gtime.AddDays(-2) && x.GAMETIME < gtime.AddDays(2))
                        .OrderBy(o => o.GAMETIME));
                    bool isDup = false;
                    bool isNDup = false;
                    int isDupPossible = 0;
                    DSReplay mrep = null;
                    foreach (DSReplay crep in cReplays)
                    {

                        List<string> races = new List<string>();
                        foreach (DSPlayer pl in crep.DSPlayer.OrderBy(o => o.POS))
                            races.Add(pl.RACE);

                        isDupPossible = 0;
                        if (Enumerable.SequenceEqual(replay.RACES, races))
                        {
                            if (replay.VERSION == crep.VERSION)
                            {
                                if (replay.MAXKILLSUM == crep.MAXKILLSUM) isDupPossible++;
                                if (replay.MAXLEAVER == crep.MAXLEAVER) isDupPossible++;
                                if (replay.MININCOME == crep.MININCOME) isDupPossible++;
                                if (replay.MINARMY == crep.MINARMY) isDupPossible++;
                                if (isDupPossible > 2)
                                    isDup = true;
                            }
                            else
                            {
                                double dur_diff = Math.Abs(TimeSpan.FromSeconds(replay.DURATION / 22.4).TotalSeconds - crep.DURATION.TotalSeconds);
                                if (dur_diff <= 480 / 22.4)
                                {
                                    int kdiff = Math.Abs(replay.MAXKILLSUM - crep.MAXKILLSUM);
                                    //int adiff = Math.Abs(rep.MINARMY - crep.MINARMY);
                                    int mdiff = Math.Abs(replay.MINKILLSUM - crep.MINKILLSUM);

                                    if (kdiff < 1000 && mdiff < 1000)
                                    {
                                        isNDup = true;
                                        mrep = crep;
                                    }
                                }
                            }
                        }
                        if (isDup || isNDup)
                        {
                            foreach (var ent in replay.PLDupPos)
                            {
                                PLDuplicate dup = crep.PLDuplicate.SingleOrDefault(x => x.Hash == ent.Key);
                                if (dup == null)
                                {
                                    PLDuplicate newdup = new PLDuplicate();
                                    newdup.Hash = ent.Key;
                                    newdup.Pos = ent.Value;
                                    newdup.DSReplay = crep;
                                    context.PLDuplicates.Add(newdup);
                                    m++;
                                }
                            }
                            break;
                        }
                    }
                    if (!isDup && !isNDup)
                    {
                        DBService.InsertdsReplay(replay);
                        i++;
                    }
                    else if (isNDup)
                    {
                        if (mrep != null && Version.Parse(replay.VERSION) > Version.Parse(mrep.VERSION))
                        {
                            replay.PLDupPos.Clear();
                            foreach (PLDuplicate dup in mrep.PLDuplicate)
                                replay.PLDupPos[dup.Hash] = dup.Pos;
                            context.DSReplays.Remove(mrep);
                            DBService.InsertdsReplay(replay);
                            m--;
                            r++;
                        }
                        d2++;
                    }
                    else
                        d1++;
                    
                    context.SaveChanges();
                }
            }
            Console.WriteLine("Dups found: " + d1 + "/" + d2);

            var info = new StringBuilder();
            info.AppendLine($"New replays inserted: {i}");
            info.AppendLine($"Replays modified    : {m}");
            info.AppendLine($"Replays replaced    : {r}");

            Console.WriteLine(info);
        }
    }
}
