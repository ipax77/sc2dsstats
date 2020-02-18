using sc2dsstats.decode.Models;
using sc2dsstats.lib.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;


namespace sc2dsstats.data
{
    public static class Scan2
    {
        private static string NewJson = Path.GetDirectoryName(DSdata.ServerConfig.MonsterJson) + "/newdata.json";

        private static HashSet<string> Hashs = new HashSet<string>();
        private static int RepIDMax = 0;
        private static List<dsreplay> mReplays = new List<dsreplay>();
        private static List<dsreplay> sReplays = new List<dsreplay>();

        public static void FullScan()
        {
            Hashs = new HashSet<string>();
            mReplays = new List<dsreplay>();
            sReplays = new List<dsreplay>();
            DSdata.ServerConfig.LastRun = DateTime.Now;

            ReadSumJsons(DSdata.ServerConfig.SumDir1);
            ReadSumJsons(DSdata.ServerConfig.SumDir2);
            DupFind();
            WriteMonsterJson();
        }

        public static void Scan()
        {
            Hashs = new HashSet<string>();
            mReplays = new List<dsreplay>();
            sReplays = new List<dsreplay>();


            List<FileInfo> NewJsons = new List<FileInfo>();
            foreach (var dir in Directory.GetDirectories(DSdata.ServerConfig.SumDir).Where(x => Path.GetFileName(x).Length == 64))
                foreach (var file in Directory.GetFiles(dir).Where(d => new FileInfo(d).LastWriteTime > DSdata.ServerConfig.LastRun).Select(s => new FileInfo(s)))
                    NewJsons.Add(file);

            if (!NewJsons.Any())
            {
                Console.WriteLine("No new datafiles found.");
                return;
            }


            ReadMonsterJson();
            foreach (var file in NewJsons.OrderBy(o => o.LastWriteTime))
                ReadJson(file.FullName);

            DupFind();
            WriteMonsterJson();


            if (File.Exists(DSdata.ServerConfig.MonsterJson + "_bak"))
                File.Delete(DSdata.ServerConfig.MonsterJson + "_bak");

            if (File.Exists(DSdata.ServerConfig.MonsterJson))
                File.Move(DSdata.ServerConfig.MonsterJson, DSdata.ServerConfig.MonsterJson + "_bak");

            if (File.Exists(NewJson))
                File.Move(NewJson, DSdata.ServerConfig.MonsterJson);

            Program.SendUpdateRequest();

            DSdata.ServerConfig.LastRun = DateTime.Now;
            Program.SaveConfig();
        }

        public static void DupFind()
        {
            Console.WriteLine("Dup find");
            int d1 = 0;
            int d2 = 0;
            foreach (dsreplay replay in sReplays.OrderBy(o => o.GAMETIME).ToArray())
            {
                List<dsreplay> cReplays = new List<dsreplay>(mReplays.Where(x => x.GAMETIME > replay.GAMETIME - 9999999 && x.GAMETIME < replay.GAMETIME + 9999999).OrderBy(o => o.GAMETIME));
                bool isDup = false;
                bool isNDup = false;
                int isDupPossible = 0;
                dsreplay mrep = null;
                foreach (dsreplay crep in cReplays)
                {
                    isDupPossible = 0;
                    if (Enumerable.SequenceEqual(replay.RACES, crep.RACES))
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
                            double dur_diff = Math.Abs(replay.DURATION - crep.DURATION);
                            if (dur_diff <= 480)
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
                            crep.PLDupPos[ent.Key] = ent.Value;
                        break;
                    }

                }
                if (!isDup && !isNDup)
                    mReplays.Add(replay);
                else if (isNDup)
                {
                    if (mrep != null && Version.Parse(replay.VERSION) > Version.Parse(mrep.VERSION))
                    {
                        replay.PLDupPos = new Dictionary<string, int>(mrep.PLDupPos);
                        mReplays.Remove(mrep);
                        mReplays.Add(replay);
                    }
                    d2++;
                }
                else
                    d1++;

            }
            Console.WriteLine("Dups found: " + d1 + "/" + d2);
        }

        public static void WriteMonsterJson()
        {
            Console.WriteLine("Writing Monster Json");

            if (File.Exists(NewJson))
                File.Delete(NewJson);

            List<string> NewJsonReplays = new List<string>();
            foreach (dsreplay rep in mReplays.OrderBy(o => o.GAMETIME))
            {
                if (rep.ID == 0)
                    rep.ID = ++RepIDMax;
                NewJsonReplays.Add(JsonSerializer.Serialize(rep));
            }
            File.WriteAllLines(NewJson, NewJsonReplays);

            Console.WriteLine("New Monster Json ready to use. Count: " + NewJsonReplays.Count);
        }

        public static void ReadJson(string file)
        {
            Console.WriteLine("Working on " + Path.GetFileName(file));
            foreach (var line in File.ReadAllLines(file, Encoding.UTF8))
            {

                dsreplay replay = JsonSerializer.Deserialize<dsreplay>(line);
                if (replay != null)
                {
                    replay.Init();
                    replay.GenHash();
                    replay.ID = 0;

                    if (replay.PLAYERS.Where(x => x.NAME == "player").Count() == 1)
                    {
                        string plhash = Path.GetFileNameWithoutExtension(file);
                        int plpos = replay.PLAYERS.FindIndex(s => s.NAME == "player");
                        replay.PLDupPos[plhash] = plpos;
                    }

                    if (!Hashs.Contains(replay.HASH))
                    {
                        sReplays.Add(replay);
                        Hashs.Add(replay.HASH);
                    }
                    else
                    {
                        dsreplay crep = mReplays.SingleOrDefault(s => s.HASH == replay.HASH);
                        if (crep == null)
                            crep = sReplays.SingleOrDefault(s => s.HASH == replay.HASH);

                        foreach (var ent in replay.PLDupPos)
                            crep.PLDupPos[ent.Key] = ent.Value;
                    }
                }
            }
        }

        public static void ReadSumJsons(string folder)
        {
            Console.WriteLine("Reading in SumJsons ..");
            foreach (var file in Directory.GetFiles(folder).OrderByDescending(d => new FileInfo(d).LastWriteTime))
            {
                foreach (var line in File.ReadAllLines(file, Encoding.UTF8))
                {
                    dsreplay replay = JsonSerializer.Deserialize<dsreplay>(line);
                    if (replay != null)
                    {
                        replay.Init();
                        replay.GenHash();
                        replay.ID = 0;

                        if (replay.PLAYERS.Where(x => x.NAME == "player").Count() == 1)
                        {
                            string plhash = Path.GetFileNameWithoutExtension(file);
                            int plpos = replay.PLAYERS.FindIndex(s => s.NAME == "player");
                            replay.PLDupPos[plhash] = plpos;
                        }

                        if (!Hashs.Contains(replay.HASH))
                        {
                            sReplays.Add(replay);
                            Hashs.Add(replay.HASH);
                        }
                        else
                        {
                            dsreplay crep = mReplays.SingleOrDefault(s => s.HASH == replay.HASH);
                            if (crep == null)
                                crep = sReplays.SingleOrDefault(s => s.HASH == replay.HASH);

                            foreach (var ent in replay.PLDupPos)
                                crep.PLDupPos[ent.Key] = ent.Value;
                        }
                    }
                }
            }
            Console.WriteLine("New sum replays: " + sReplays.Count);
        }

        public static void ReadMonsterJson()
        {
            Console.WriteLine("Reading in MonsterJson ..");
            if (File.Exists(DSdata.ServerConfig.MonsterJson))
            {

                foreach (var line in File.ReadAllLines(DSdata.ServerConfig.MonsterJson, Encoding.UTF8))
                {
                    dsreplay replay = JsonSerializer.Deserialize<dsreplay>(line);
                    if (replay != null)
                    {
                        replay.Init();
                        replay.GenHash();
                        Hashs.Add(replay.HASH);
                        mReplays.Add(replay);
                        if (replay.ID > RepIDMax)
                            RepIDMax = replay.ID;
                    }
                }

                Console.WriteLine("Monster Json count: " + mReplays.Count);
            }

        }
    }
}
