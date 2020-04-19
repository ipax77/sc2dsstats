using paxgamelib.Data;
using paxgamelib.Models;
using paxgamelib.Service;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Numerics;
using System.Diagnostics.CodeAnalysis;

namespace paxgamelib.Service
{
    public static class BBService
    {
        private static BlockingCollection<BBuildJob> _jobs_build;
        private static BlockingCollection<BBuildJob> _jobs_position;
        private static BlockingCollection<int> _jobs_random;
        private static CancellationTokenSource source = new CancellationTokenSource();
        private static CancellationToken token = source.Token;
        private static ManualResetEvent _empty = new ManualResetEvent(false);
        private static int CORES = 8;
        private static RefreshBB _refreshBB;
        
        private static int MaxValue = 0;
        private static object locker = new Object();
        public const string mlgamesFile = "/data/ml/mlgames.txt";

        public static TimeSpan Elapsed { get; set; } = new TimeSpan(0);

        public static DateTime START { get; set; }
        public static DateTime END { get; set; }

        public static int THREADS = 0;

        public static int BUILDS = 0;
        public static int POSITIONS = 0;

        public static bool Running = false;

        public static Vector2 center = new Vector2(128, 119);

        internal static ILibData _store = LibData.Instance;

        internal static Player _player;
        internal static Player _opp;

        public static async Task GetBestBuild([NotNull] Player player, [NotNull] Player opp, [NotNull] RefreshBB refreshBB, int builds = 100, int positions = 200, int cores = 8)
        {
            if (Running == true)
                return;
            _refreshBB = refreshBB;
            _refreshBB.Init();
            Running = true;
            opp.SoftReset();
            opp.Units = UnitPool.Units.Where(x => x.Race == opp.Race && x.Cost > 0).ToList();
            _refreshBB.Bopp = opp.GetString();
            _refreshBB.Bplayer = player.GetString();
            _refreshBB.BestBuild = _refreshBB.Bplayer;
            _refreshBB.WorstBuild = _refreshBB.Bplayer;

            _player = player;
            _opp = opp;

            source = new CancellationTokenSource();
            token = source.Token;
            _empty = new ManualResetEvent(false);
            CORES = cores;
            MaxValue = 0;

            BUILDS = builds;
            POSITIONS = positions;
            _refreshBB.TOTAL_DONE = 0;
            _refreshBB.TOTAL = builds * positions + builds;

            START = DateTime.UtcNow;
            END = DateTime.MinValue;

            _jobs_build = new BlockingCollection<BBuildJob>();
            _jobs_position = new BlockingCollection<BBuildJob>();

            foreach (Unit unit in player.Units.Where(y => y.Status != UnitStatuses.Available))
                MaxValue += unit.Cost;

            GameHistory game = new GameHistory();
            game.ID = paxgame.GetGameID();
            game.battlefield = new Battlefield();
            Player myplayer = player.SoftCopy();
            Player myopp = opp.SoftCopy();
            game.Players.Add(myplayer);
            myplayer.Game = game;
            game.Players.Add(myopp);
            myopp.Game = game;
            myplayer.SetString(_refreshBB.Bplayer);
            _refreshBB.MineralsCurrent = myplayer.MineralsCurrent;
            myopp.SetString(_refreshBB.Bopp);
            GameService.GenFight(game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];
            _refreshBB.BestStats = result;
            _refreshBB.BestStatsOpp = oppresult;

            for (int i = 0; i < BUILDS; i++)
            {
                BBuildJob job = new BBuildJob();
                job.PlayerBuild = _refreshBB.Bplayer;
                job.OppBuild = _refreshBB.Bopp;
                job.Minerals = _refreshBB.MineralsCurrent;
                _jobs_build.Add(job);
            }

            for (int i = 0; i < 1; i++)
            {
                Thread thread = new Thread(OnHandlerStartBuild)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }

            for (int i = 0; i < 8; i++)
            {
                Thread thread = new Thread(OnHandlerStartPosition)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }

            while (!_empty.WaitOne(1000))
            {
                Console.WriteLine(_jobs_position.Count + _jobs_build.Count);
                _refreshBB.Update = !_refreshBB.Update;
                if (!_jobs_position.Any())
                    break;
            }
            END = DateTime.UtcNow;
            Running = false;
            _refreshBB.Update = !_refreshBB.Update;

        }

        public static async Task GetBestPosition([NotNull] Player player, [NotNull] Player opp, [NotNull] RefreshBB refreshBB, int builds = 100, int positions = 200, int cores = 8)
        {
            if (Running == true)
                return;

            Running = true;
            _refreshBB = refreshBB;
            _refreshBB.Init();
            _refreshBB.Bplayer = player.GetString();
            _refreshBB.Bopp = opp.GetString();
            _refreshBB.BestBuild = _refreshBB.Bopp;
            _refreshBB.WorstBuild = _refreshBB.Bopp;

            _player = player;
            _opp = opp;

            source = new CancellationTokenSource();
            token = source.Token;
            _empty = new ManualResetEvent(false);
            CORES = cores;
            MaxValue = 0;

            BUILDS = builds;
            POSITIONS = positions;
            _refreshBB.TOTAL_DONE = 0;
            _refreshBB.TOTAL = positions;

            START = DateTime.UtcNow;
            END = DateTime.MinValue;

            _jobs_build = new BlockingCollection<BBuildJob>();
            _jobs_position = new BlockingCollection<BBuildJob>();

            foreach (Unit unit in player.Units.Where(y => y.Status != UnitStatuses.Available))
                MaxValue += unit.Cost;

            GameHistory game = new GameHistory();
            game.ID = paxgame.GetGameID();
            game.battlefield = new Battlefield();
            Player myplayer = player.SoftCopy();
            Player myopp = opp.SoftCopy();
            game.Players.Add(myplayer);
            myplayer.Game = game;
            game.Players.Add(myopp);
            myopp.Game = game;
            myplayer.SetString(_refreshBB.Bplayer);
            myopp.SetString(_refreshBB.Bopp);
            GameService.GenFight(game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];
            _refreshBB.BestStats = result;
            _refreshBB.BestStatsOpp = oppresult;

            for (int i = 0; i < POSITIONS; i++)
            {
                BBuildJob job = new BBuildJob();
                job.PlayerBuild = myplayer.GetString();
                job.OppBuild = myopp.GetString();
                _jobs_position.Add(job);
            }

            for (int i = 0; i < 8; i++)
            {
                Thread thread = new Thread(OnHandlerStartPosition)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }

            while (!_empty.WaitOne(1000))
            {
                Console.WriteLine(_jobs_position.Count() + _jobs_build.Count());
                _refreshBB.Update = !_refreshBB.Update;
                if (!_jobs_position.Any())
                    break;
            }
            END = DateTime.UtcNow;
            Running = false;
            _refreshBB.Update = !_refreshBB.Update;
        }

        public static async Task GetRandomFights([NotNull] Player player, [NotNull] Player opp, [NotNull] RefreshBB refresh, int builds = 100, int positions = 200, int cores = 8)
        {
            if (Running == true)
                return;

            Running = true;
            _refreshBB = refresh;
            _refreshBB.Bplayer = player.GetString();
            _refreshBB.Bopp = opp.GetString();

            source = new CancellationTokenSource();
            token = source.Token;
            _empty = new ManualResetEvent(false);
            CORES = cores;
            MaxValue = 0;

            BUILDS = builds;
            POSITIONS = positions;
            _refreshBB.TOTAL_DONE = 0;
            _refreshBB.TOTAL = positions;

            START = DateTime.UtcNow;
            END = DateTime.MinValue;

            _jobs_random = new BlockingCollection<int>();

            for (int i = 0; i < 40000; i++)
                _jobs_random.Add(i);

            for (int i = 0; i < 8; i++)
            {
                Thread thread = new Thread(OnHandlerStartRandom)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }

            while (!_empty.WaitOne(1000))
            {
                Console.WriteLine(_jobs_random.Count());
                _refreshBB.Update = !_refreshBB.Update;
                if (!_jobs_random.Any())
                    break;
            }
            END = DateTime.UtcNow;
            Running = false;
            _refreshBB.Update = !_refreshBB.Update;
        }

        public static RandomGame RandomFight(int minerals = 2000, bool save = false)
        {
            Player _player = new Player();
            _player.Name = "Player#1";
            _player.Pos = 1;
            _player.ID = paxgame.GetPlayerID();
            _player.Race = UnitRace.Terran;
            _player.inGame = true;
            _player.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _player.Race));

            Player _opp = new Player();
            _opp.Name = "Player#2";
            _opp.Pos = 4;
            _opp.ID = paxgame.GetPlayerID();
            _opp.Race = UnitRace.Terran;
            _opp.inGame = true;
            _opp.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _opp.Race));

            GameHistory game = new GameHistory();
            _player.Game = game;
            _player.Game.ID = paxgame.GetGameID();
            _player.Game.Players.Add(_player);
            _player.Game.Players.Add(_opp);

            _opp.Game = _player.Game;

            _player.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _player.Race && x.Cost > 0));
            _opp.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _opp.Race && x.Cost > 0));

            _player.MineralsCurrent = minerals;
            _opp.MineralsCurrent = minerals;

            OppService.BPRandom(_player).GetAwaiter().GetResult();
            OppService.BPRandom(_opp).GetAwaiter().GetResult();

            BBuild bplayer = new BBuild(_player);
            BBuild bopp = new BBuild(_opp);
            bplayer.SetBuild(_player);
            bopp.SetBuild(_opp);

            GameService.GenFight(_player.Game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];

            RandomResult result1 = new RandomResult();
            result1.DamageDone = oppresult.DamageDone;
            result1.MineralValueKilled = oppresult.MineralValueKilled;
            RandomResult result2 = new RandomResult();
            result2.DamageDone = result.DamageDone;
            result2.MineralValueKilled = result.MineralValueKilled;

            RandomGame rgame = new RandomGame();
            rgame.player1 = bplayer;
            rgame.player2 = bopp;
            rgame.result1 = result1;
            rgame.result2 = result2;
            rgame.Result = game.Stats.Last().winner;

            if (save == true)
                SaveGame(rgame, _player, _opp);

            return rgame;
        }

        public static void SaveGame(RandomGame game, Player p1, Player p2)
        {
            float reward = 0;
            double mod1 = game.result1.MineralValueKilled - game.result2.MineralValueKilled;
            mod1 = mod1 / 1000;

            double mod2 = game.result1.DamageDone - game.result2.DamageDone;
            mod2 = mod2 / 10000;

            float rewardp1 = (float)mod1;
            rewardp1 += (float)mod2;

            float rewardp2 = (float)mod1 * -1;
            rewardp2 -= (float)mod2;

            if (game.Result == 0)
            {
                rewardp1 += 1;
                rewardp2 += 1;
            }
            else if (game.Result == 1)
            {
                rewardp1 += 2;
                rewardp2 += 0;
            }
            else if (game.Result == 2)
            {
                rewardp1 += 0;
                rewardp2 += 2;
            }

            List<string> presult = new List<string>();
            //presult.Add(rewardp1 + "," + BBuild.PrintMatrix(game.player1.GetMatrix(p1)));
            //presult.Add(rewardp2 + "," + BBuild.PrintMatrix(game.player2.GetMatrix(p2)));

            lock(locker)
            {
                File.AppendAllLines(mlgamesFile, presult);
            }
        }

        public static void RESTFight(GameHistory _game, bool random = true)
        {
            if (random)
            {
                OppService.BPRandom(_game.Players.First()).GetAwaiter();
                OppService.BPRandom(_game.Players.Last()).GetAwaiter();
            }
            GameService.GenFight(_game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(_game).GetAwaiter().GetResult();
        }

        public static (RESTResult, string) RESTFight(string p1, string p2)
        {
            Player _player = new Player();
            _player.Name = "Player#1";
            _player.Pos = 1;
            _player.ID = paxgame.GetPlayerID();
            _player.Race = UnitRace.Terran;
            _player.inGame = true;
            _player.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _player.Race));

            Player _opp = new Player();
            _opp.Name = "Player#2";
            _opp.Pos = 4;
            _opp.ID = paxgame.GetPlayerID();
            _opp.Race = UnitRace.Terran;
            _opp.inGame = true;
            _opp.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _opp.Race));

            GameHistory game = new GameHistory();
            _player.Game = game;
            _player.Game.ID = paxgame.GetGameID();
            _player.Game.Players.Add(_player);
            _player.Game.Players.Add(_opp);

            _opp.Game = _player.Game;


            //OppService.BPRandom(_player).GetAwaiter().GetResult();
            //OppService.BPRandom(_opp).GetAwaiter().GetResult();

            BBuild bplayer = new BBuild(_player);
            BBuild bopp = new BBuild(_opp);
            bplayer.SetString(p1, _player);
            bopp.SetString(p2, _opp);

            GameService.GenFight(_player.Game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();
            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];

            RESTResult rgame = new RESTResult();
            rgame.Result = game.Stats.Last().winner;
            rgame.DamageP1 = oppresult.DamageDone;
            rgame.MinValueP1 = oppresult.MineralValueKilled;
            rgame.DamageP2 = result.DamageDone;
            rgame.MinValueP2 = result.MineralValueKilled;

            return (rgame, bopp.GetString(_opp));
        }

        public static (RESTResult, string, string) RESTFight(string p1, int minerals)
        {
            Player _opp = new Player();
            _opp.Name = "Player#2";
            _opp.Pos = 4;
            _opp.ID = paxgame.GetPlayerID();
            _opp.Race = UnitRace.Terran;
            _opp.inGame = true;
            _opp.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _opp.Race));
            GameHistory game = new GameHistory();
            _opp.Game = game;
            _opp.Game.ID = paxgame.GetGameID();
            _opp.Game.Players.Add(_opp);
            
            _opp.MineralsCurrent = minerals;
            OppService.BPRandom(_opp).GetAwaiter().GetResult();
            string actions = String.Join('X', _opp.GetAIMoves());
            (RESTResult result, string oppstring) = RESTFight(p1, _opp.GetString());
            return (result, actions, oppstring);

            //return RESTFight(p1, bopp.GetString(_opp));
        }

        public static (RESTResult, string, string) RESTFight(string p1, ulong GameID, int minerals)
        {
            Player _opp = _store.GetPlayer(GameID);
            if (_opp == null)
            {
                _opp = new Player();
                _opp.Name = "RESTPlayer " + GameID;
                _opp.Pos = 4;
                _opp.ID = GameID;
                _opp.Race = UnitRace.Terran;
                _opp.inGame = true;
                _opp.Units = new List<Unit>(UnitPool.Units.Where(x => x.Race == _opp.Race));
                GameHistory game = new GameHistory();
                _opp.Game = game;
                _opp.Game.ID = paxgame.GetGameID();
                _opp.Game.Players.Add(_opp);

                _opp.MineralsCurrent = paxgame.Income;
                _store.SetPlayer(_opp);
            } else
            {
                foreach (Unit unit in _opp.Units.Where(x => x.Status == UnitStatuses.Placed))
                    unit.Status = UnitStatuses.Spawned;
                _opp.Game.Spawn++;
                //_opp.MineralsCurrent = minerals - (_opp.Game.Spawn * paxgame.Income);
                _opp.MineralsCurrent += paxgame.Income;
            }
            OppService.BPRandom(_opp).GetAwaiter().GetResult();
            string actions = String.Join('X', _opp.GetAIMoves());
            (RESTResult result, string oppstring) = RESTFight(p1, _opp.GetString());
            return (result, actions, oppstring);
        }

        public static void BuildJob(object obj)
        {
            BBuildJob job = obj as BBuildJob;
            GameHistory game = new GameHistory();
            game.ID = paxgame.GetGameID();
            game.battlefield = new Battlefield();
            Player myplayer = _player.SoftCopy();
            myplayer.Game = game;
            Player myopp = _opp.SoftCopy();
            myopp.Game = game;
            game.Players.Add(myplayer);
            myplayer.Game = game;
            game.Players.Add(myopp);
            myopp.Game = game;
            myplayer.SetString(job.PlayerBuild);
            myopp.SetString(job.OppBuild);
            myopp.MineralsCurrent = job.Minerals;

            OppService.BPRandom(myopp, true).GetAwaiter().GetResult();
            GameService.GenFight(game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();

            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];
            lock (_refreshBB)
            {
                int check = CheckResult(result, oppresult);
                if (check == 1 || check == 2)
                    _refreshBB.BestBuild = myopp.GetString();
                else if (check == 3)
                    _refreshBB.WorstBuild = myopp.GetString();
            }

            for (int i = 0; i < POSITIONS; i++)
            {
                BBuildJob pjob = new BBuildJob();
                pjob.PlayerBuild = job.PlayerBuild;
                pjob.OppBuild = myopp.GetString();
                _jobs_position.Add(pjob);
            }

            Interlocked.Increment(ref _refreshBB.TOTAL_DONE);
        }

        public static void PositionJob(object obj)
        {
            BBuildJob job = obj as BBuildJob;
            GameHistory game = new GameHistory();
            game.ID = paxgame.GetGameID();
            game.battlefield = new Battlefield();
            Player myplayer = _player.SoftCopy();
            myplayer.Game = game;
            Player myopp = _opp.SoftCopy();
            myopp.Game = game;
            game.Players.Add(myplayer);
            myplayer.Game = game;
            game.Players.Add(myopp);
            myopp.Game = game;
            myplayer.SetString(job.PlayerBuild);
            myopp.SetString(job.OppBuild);

            OppService.PositionRandomDistmod(myopp.Units.Where(x => x.Status != UnitStatuses.Available).ToList(), myopp).GetAwaiter();
            GameService.GenFight(game).GetAwaiter().GetResult();
            StatsService.GenRoundStats(game, false).GetAwaiter().GetResult();

            Stats result = new Stats();
            Stats oppresult = new Stats();
            result.DamageDone = game.Stats.Last().Damage[1];
            result.MineralValueKilled = game.Stats.Last().Killed[1];
            oppresult.DamageDone = game.Stats.Last().Damage[0];
            oppresult.MineralValueKilled = game.Stats.Last().Killed[0];
            lock (_refreshBB)
            {
                int check = CheckResult(result, oppresult);
                if (check == 1 || check == 2)
                    _refreshBB.BestBuild = myopp.GetString();
                else if (check == 3)
                    _refreshBB.WorstBuild = myopp.GetString();
            }
            Interlocked.Increment(ref _refreshBB.TOTAL_DONE);
        }

        public static int CheckResult(Stats result, Stats oppresult)
        {
            int check = 0;
            if (result.MineralValueKilled >= _refreshBB.BestStats.MineralValueKilled)
            {
                if (result.MineralValueKilled > _refreshBB.BestStats.MineralValueKilled)
                {
                    Console.WriteLine("setting Bestbuild");

                    _refreshBB.BestStats.MineralValueKilled = result.MineralValueKilled;
                    _refreshBB.BestStats.DamageDone = result.DamageDone;

                    check = 1;
                }
                else
                {
                    if (result.MineralValueKilled == _refreshBB.BestStats.MineralValueKilled)
                    {
                        if (_refreshBB.BestStatsOpp.MineralValueKilled == 0 || (oppresult.MineralValueKilled < _refreshBB.BestStatsOpp.MineralValueKilled))
                        {
                            _refreshBB.BestStatsOpp.MineralValueKilled = oppresult.MineralValueKilled;
                            _refreshBB.BestStatsOpp.DamageDone = oppresult.DamageDone;

                            Console.WriteLine("setting very Bestbuild");

                            _refreshBB.BestStats.MineralValueKilled = result.MineralValueKilled;
                            _refreshBB.BestStats.DamageDone = result.DamageDone;

                            check = 2;
                        }
                    }
                }
                /*
                if (_refreshBB.BestStats.MineralValueKilled == MaxValue)
                {
                    Running = false;
                    _refreshBB.Update = !_refreshBB.Update;
                    StopIt();
                    return;
                }
                */
            }

            if (_refreshBB.WorstStats.MineralValueKilled == 0 || result.MineralValueKilled < _refreshBB.WorstStats.MineralValueKilled)
            {
                _refreshBB.WorstStats.MineralValueKilled = result.MineralValueKilled;
                if (_refreshBB.WorstStats.MineralValueKilled == 0)
                    _refreshBB.WorstStats.MineralValueKilled = 1;
                _refreshBB.WorstStats.DamageDone = result.DamageDone;
                check = 3;
            }
            
            return check;
        }


        private static void OnHandlerStartBuild(object obj)
        {
            if (token.IsCancellationRequested == true)
                return;

            try
            {
                foreach (var job in _jobs_build.GetConsumingEnumerable(token))
                {
                    BuildJob(job);
                }
            }
            catch (OperationCanceledException)
            {
                END = DateTime.UtcNow;
            }
            _empty.Set();
        }

        private static void OnHandlerStartPosition(object obj)
        {
            if (token.IsCancellationRequested == true)
                return;
            try
            {
                foreach (var job in _jobs_position.GetConsumingEnumerable(token))
                {
                    PositionJob(job);
                }
            }
            catch (OperationCanceledException)
            {
                END = DateTime.UtcNow;
            }
        }

        private static void OnHandlerStartRandom(object obj)
        {
            if (token.IsCancellationRequested == true)
                return;
            try
            {
                foreach (var job in _jobs_random.GetConsumingEnumerable(token))
                {
                    RandomFight(2000, true);
                }
            }
            catch (OperationCanceledException)
            {
                END = DateTime.UtcNow;
            }
        }

        public static void StopIt()
        {
            try
            {
                source.Cancel();
            }
            catch { }
            finally
            {
                //source.Dispose();
            }
        }
    }
}
