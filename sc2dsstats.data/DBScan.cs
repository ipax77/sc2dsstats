using Microsoft.EntityFrameworkCore;
using sc2dsstats.decode.Models;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
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

        private static HashSet<string> plNames = new HashSet<string>()
        {
            "player1",
            "player2",
            "player3",
            "player4",
            "player5",
            "player6"
        };

        public static void Scan()
        {
            List<FileInfo> NewJsons = new List<FileInfo>();
            foreach (var dir in Directory.GetDirectories(DSdata.ServerConfig.SumDir).Where(x => Path.GetFileName(x).Length == 64))
                foreach (var file in Directory.GetFiles(dir).Where(d => new FileInfo(d).LastWriteTime > DSdata.ServerConfig.LastRun).Select(s => new FileInfo(s)))
                    NewJsons.Add(file);

            if (!NewJsons.Any())
            {
                Console.WriteLine("No new datafiles found.");
                return;
            }

            foreach (var file in NewJsons.OrderBy(o => o.LastWriteTime))
                ReadJson(file.FullName);

            Console.WriteLine("New Replays found: " + sReplays.Count);


        }

        public static void FixName()
        {
            using (var context = new DSReplayContext())
            {
                var replays = context.DSReplays
                                .Include(p => p.DSPlayer)
                                .Include(p => p.PLDuplicate);
                ;

                var localreps = replays.ToList();
                int i = 0;
                foreach (var rep in localreps)
                {
                    i++;
                    if (i % 10 == 0)
                    {
                        Console.WriteLine(i);
                        context.SaveChanges();
                    }

                    foreach (var dup in rep.PLDuplicate)
                        if (dup.Pos > 0)
                        {
                            DSPlayer pl = rep.DSPlayer.SingleOrDefault(s => s.REALPOS == dup.Pos);
                            if (pl != null)
                                pl.NAME = dup.Hash;
                        }

                }
            }
        }

        public static void InitDB(int count = 0)
        {
            using (var context = new DSReplayContext())
            {
                context.Database.EnsureCreated();
            }
            int i = 0;

            foreach (string line in File.ReadAllLines("/data/data.json"))
            {
                dsreplay rep = JsonSerializer.Deserialize<dsreplay>(line);
                rep.Init();
                rep.GenHash();
                //DBService.InsertdsReplay(rep);
                i++;
                if (i % 100 == 0)
                {
                    Console.WriteLine(i);
                    if (count > 0 && i > count)
                        break;
                }
            }

        }

        public static void ReadJson(string file)
        {
            Console.WriteLine("Working on " + Path.GetFileName(file));
            string filename = Path.GetFileNameWithoutExtension(file);
            string plhash = filename.Substring(filename.Length - 64);
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

                        dsplayer pl = replay.PLAYERS.FirstOrDefault(s => s.NAME == "player");
                        if (pl != null)
                        {
                            pl.NAME = plhash;
                            replay.PLDupPos[plhash] = pl.REALPOS;
                        }
                        DSReplay crep = null;

                        /*
                        crep = context.DSReplays
                            .Include(p => p.PLDuplicate)
                            .Include(p => p.DSPlayer)
                            .SingleOrDefault(x => x.HASH == replay.HASH);
                        */
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
                            {
                                foreach (var ent in replay.PLDupPos)
                                    screp.PLDupPos[ent.Key] = ent.Value;
                                if (pl != null)
                                {
                                    dsplayer dpl = screp.PLAYERS.SingleOrDefault(s => s.REALPOS == pl.REALPOS);
                                    if (dpl != null)
                                        dpl.NAME = plhash;
                                }
                            }
                            else
                            {
                                if (pl != null)
                                {
                                    DSPlayer dpl = crep.DSPlayer.SingleOrDefault(s => s.REALPOS == pl.REALPOS);
                                    if (dpl != null)
                                        dpl.NAME = plhash;
                                }

                                PLDuplicate dup = crep.PLDuplicate.SingleOrDefault(x => x.Hash == plhash);
                                if (dup == null)
                                {
                                    PLDuplicate newdup = new PLDuplicate();
                                    newdup.Hash = plhash;
                                    newdup.Pos = (byte)pl.REALPOS;
                                    newdup.DSReplay = crep;
                                    context.PLDuplicates.Add(newdup);

                                }
                                context.SaveChanges();
                            }
                        }
                    }
                }
            }
        }
    }
}
