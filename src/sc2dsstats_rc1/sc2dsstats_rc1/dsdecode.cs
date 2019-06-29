using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Runtime;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats_rc1
{
    public class dsdecode
    {
        MainWindow MW { get; set; }
        private BlockingCollection<string> _jobs_decode = new BlockingCollection<string>();
        private BlockingCollection<scandetail> _jobs_parse = new BlockingCollection<scandetail>();
        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        ConcurrentDictionary<string, dsreplay> replaysng { get; set; } = new ConcurrentDictionary<string, dsreplay>();
        ConcurrentDictionary<string, int> REDO { get; set; } = new ConcurrentDictionary<string, int>();
        ConcurrentDictionary<string, int> SKIP { get; set; } = new ConcurrentDictionary<string, int>();
        public string OUTDIR { get; set; } = @"C:\temp\bab\analyzes";
        public string OUTFILE { get; set; } = @"C:\temp\bab\csharp_stats.csv";
        public string JSONFILE { get; set; } = @"C:\temp\bab\csharp_stats.json";
        static int THREADS = 0;
        static int THREADS_PARSE = 0;
        static int TOTAL = 0;
        static int TOTAL_DONE = 0;
        static int REPID = 0;
        static readonly object _locker = new object();
        private ManualResetEvent _empty = new ManualResetEvent(false);
        static string[] GETPOOL = new string[] { "details", "trackerevents" };
        private DateTime START { get; set; }
        private DateTime END { get; set; }
        private ScriptScope SCOPE { get; set; }
        private ScriptEngine ENGINE { get; set; }
        private REParea AREA { get; set; } = new REParea();
        private JsonSerializerSettings JsonSetting { get; set; }

        Regex rx_name = new Regex(@"m_name': '([^']+)',$", RegexOptions.Singleline);
        Regex rx_subname = new Regex(@"<sp\/>(.*)$", RegexOptions.Singleline);
        Regex rx_race = new Regex(@"m_race': '([\\\w]*)',$", RegexOptions.Singleline);
        Regex rx_result = new Regex(@"m_result': (\d+),$", RegexOptions.Singleline);
        Regex rx_team = new Regex(@"m_teamId': (\d+),$", RegexOptions.Singleline);
        Regex rx_workingset = new Regex(@"m_workingSetSlotId': (\d+)\}\]?,$", RegexOptions.Singleline);
        Regex rx_offset = new Regex(@"m_timeLocalOffset'\:\s([\-\d]+)L,$", RegexOptions.Singleline);
        Regex rx_time = new Regex(@"m_timeUTC': (\d+)L,$", RegexOptions.Singleline);

        Regex rx_race2 = new Regex(@"Worker(.*)", RegexOptions.Singleline);
        Regex rx_unit = new Regex(@"([^']+)Place([^']+)?", RegexOptions.Singleline);

        public static int MIN5 = 6720;
        public static int MIN10 = 13440;
        public static int MIN15 = 20160;
        public static int CORES = 1;
        private static int DEBUG = 0;
        private static string EXEDIR;

        public List<KeyValuePair<string, int>> BREAKPOINTS { get; set; } = new List<KeyValuePair<string, int>>();

        public dsdecode(int numThreads, MainWindow mw)
        {
            MW = mw;
            DEBUG = Properties.Settings.Default.DEBUG;

            if (numThreads > 0) CORES = numThreads;
            else
            {
                CORES = Environment.ProcessorCount;
            }

            BREAKPOINTS.Add(new KeyValuePair<string, int>("MIN5", MIN5));
            BREAKPOINTS.Add(new KeyValuePair<string, int>("MIN10", MIN10));
            BREAKPOINTS.Add(new KeyValuePair<string, int>("MIN15", MIN15));
            BREAKPOINTS.Add(new KeyValuePair<string, int>("ALL", 0));

            JsonSerializerSettings jsSettings = new JsonSerializerSettings();
            jsSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            JsonSetting = jsSettings;
        }

        private void Log(string msg)
        {
            if (DEBUG > 1)
            {
                Console.WriteLine(msg);
                _readWriteLock.EnterWriteLock();
                File.AppendAllText(MW.myScan_log, msg + Environment.NewLine);
                _readWriteLock.ExitWriteLock();
            }
        }

        private void Run(System.Windows.Controls.ProgressBar pb)
        {
            Log("Starting thread handler with " + CORES + " cores ..");
            LoadEngine();
            for (int i = 0; i < CORES; i++)
            {
                Thread thread = new Thread(OnHandlerStart)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }
            bool scanfailed = false;
            Task tsscan = Task.Factory.StartNew(() =>
            {
                int failsafe = 0;
                while (!_empty.WaitOne(1000))
                {
                    //Console.WriteLine("Waiting for queue to empty");
                    double wr = 0;
                    if (TOTAL > 0)
                    {
                        wr = (double)TOTAL_DONE * 100 / (double)TOTAL;
                    }
                    int val = Convert.ToInt32(wr);
                    MW.Dispatcher.Invoke(() =>
                    {
                        pb.Value = val;
                        MW.lb_sb_info2.Content = TOTAL_DONE + "/" + TOTAL + " done. (" + Math.Round(wr, 2).ToString() + "%)";
                    });

                    if (REDO.Count >= (TOTAL - CORES))
                    {
                        failsafe++;
                        if (_jobs_decode.Count == 0 && failsafe > 7 + CORES)
                        {
                            scanfailed = true;
                            Stop_decode();
                            break;
                        }
                    }
                }
                Done(scanfailed, pb);

            }, TaskCreationOptions.AttachedToParent);
        }

        private void Done(bool scanfailed, System.Windows.Controls.ProgressBar pb)
        {
            TimeSpan timeDiff = new TimeSpan(0);
            if (TOTAL_DONE == TOTAL)
            {
                DateTime end = DateTime.UtcNow;
                timeDiff = end - START;
                //Console.WriteLine(timeDiff.TotalSeconds);
            }
            Console.WriteLine("Scan complete.");
            dsskip.Finaly(MW.mySkip_csv, SKIP);
            MW.Dispatcher.Invoke(() =>
            {
                if (scanfailed == false)
                {
                    pb.Value = 100;
                    MW.lb_sb_info2.Content = TOTAL_DONE + "/" + TOTAL + " done. (100%) - Elapsed time: " + timeDiff.ToString("c");
                }
                else MW.lb_sb_info2.Content = "Scan failed :( Please try to scan again (maybe with one core only).";

                MW.replays.Clear();
                MW.replays = MW.LoadData(MW.myStats_json);
                MW.UpdateGraph(null);
                MW.scan_running = false;
            });
        }

        private ScriptEngine LoadEngine()
        {
            Log("Loading Engine ..");
            string exedir = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName;
            EXEDIR = exedir;
            string pylib1 = exedir + @"\pylib";
            string pylib2 = exedir + @"\pylib\site-packages";

            Dictionary<string, object> options = new Dictionary<string, object>();
            if (DEBUG > 1)
            {
                options["Debug"] = ScriptingRuntimeHelpers.True;
                options["ExceptionDetail"] = ScriptingRuntimeHelpers.True;
                options["ShowClrExceptions"] = ScriptingRuntimeHelpers.True;
            }
            //options["MTA"] = ScriptingRuntimeHelpers.True;
            ScriptEngine engine = IronPython.Hosting.Python.CreateEngine(options);

            var paths = engine.GetSearchPaths();
            paths.Add(pylib1);
            paths.Add(pylib2);
            engine.SetSearchPaths(paths);

            ScriptScope scope = engine.CreateScope();

            dynamic result = null;
            result = engine.ExecuteFile(exedir + @"\pylib\site-packages\mpyq.py", scope);
            if (result != null) Console.WriteLine(result);
            result = engine.Execute("import s2protocol", scope);
            if (result != null) Console.WriteLine(result);
            result = engine.Execute("from s2protocol import versions", scope);
            if (result != null) Console.WriteLine(result);
            //Thread.Sleep(1000);
            SCOPE = scope;
            ENGINE = engine;
            Log("Loading Engine comlete.");
            return engine;
        }
        public void Enqueue(string job)
        {
            if (!_jobs_decode.IsAddingCompleted)
            {
                _jobs_decode.Add(job);
            }
        }

        public void Stop_decode()
        {
            //This will cause '_jobs.GetConsumingEnumerable' to stop blocking and exit when it's empty
            _jobs_decode.CompleteAdding();
        }

        private void OnHandlerStart()
        {
            foreach (var job in _jobs_decode.GetConsumingEnumerable(CancellationToken.None))
            {
                //Console.WriteLine(job);
                //Thread.Sleep(1000);
                //Decode(job);
                DecodePython(job);
            }
            _empty.Set();
        }

        private void OnHandlerStart_parse()
        {
            foreach (var job in _jobs_parse.GetConsumingEnumerable(CancellationToken.None))
            {
                //Console.WriteLine(job);
                //Thread.Sleep(1000);
                //ParseDetail(job.REP, job.DETAILS);
            }
        }

        public void Scan()
        {
            MW.scan_running = true;

            List<string> todo_pre = dsscan.Scan(MW);
            SKIP = new ConcurrentDictionary<string, int>(dsskip.Get(MW.mySkip_csv));
            List<string> todo = new List<string>();
            foreach (string rep in todo_pre) {
                if (SKIP.ContainsKey(rep) && SKIP[rep] > 3)
                {
                    if (DEBUG > 0) Console.WriteLine("Skipping " + rep + " due to skiplist");
                }
                else todo.Add(rep);
            }

            TOTAL_DONE = 0;
            TOTAL = todo.Count();
            START = DateTime.UtcNow;
            REPID = ReadFromJsonFile();
            dsstatus status = new dsstatus(MW);
            if (CORES == 1)
            {
                Scan_sequ(todo, status.Set());
                return;
            }

            if (todo.Count > 0)
            {
                Run(status.Set());
                MW.scan_running = true;
            } else
            {
                MW.Dispatcher.Invoke(() =>
                {
                    MW.lb_sb_info2.Content = "No new replays found.";
                });
                MW.scan_running = false;
            }
            Log("Working on " + TOTAL + " replays.");
            foreach (string rep in todo)
            {
                _jobs_decode.Add(rep);
            }
            //_jobs_decode.CompleteAdding();
        }

        private void Scan_sequ(List<string> todo, System.Windows.Controls.ProgressBar pb)
        {
            LoadEngine();
            Task sequ = Task.Factory.StartNew(() =>
            {
                foreach (string rep in todo)
                {
                    DecodePython(rep);
                    double wr = 0;
                    if (TOTAL > 0)
                    {
                        wr = (double)TOTAL_DONE * 100 / (double)TOTAL;
                    }
                    int val = Convert.ToInt32(wr);
                    MW.Dispatcher.Invoke(() =>
                    {
                        pb.Value = val;
                        MW.lb_sb_info2.Content = TOTAL_DONE + "/" + TOTAL + " done. (" + Math.Round(wr, 2).ToString() + "%)";
                    });
                }
                TimeSpan timeDiff = new TimeSpan(0);
                if (TOTAL_DONE == TOTAL)
                {
                    DateTime end = DateTime.UtcNow;
                    timeDiff = end - START;
                    Console.WriteLine(timeDiff.TotalSeconds);
                }
                Console.WriteLine("Scan complete.");
                dsskip.Finaly(MW.mySkip_csv, SKIP);

                MW.Dispatcher.Invoke(() =>
                {
                    pb.Value = 100;
                    if (REDO.Count == 0)
                    {
                        MW.lb_sb_info2.Content = TOTAL_DONE + "/" + TOTAL + " done. (100%) - Elapsed time: " + timeDiff.ToString("c");
                        MW.scan_running = false;
                    }
                });
                
                if (MW.scan_running == true)
                {
                    while (REDO.Count > 0)
                    {
                        Thread.Sleep(1000);
                    }
                    MW.Dispatcher.Invoke(() =>
                    {
                        MW.lb_sb_info2.Content = TOTAL_DONE + "/" + TOTAL + " done. (100%) - Elapsed time: " + timeDiff.ToString("c");
                        MW.replays.Clear();
                        MW.replays = MW.LoadData(MW.myStats_json);
                        MW.UpdateGraph(null);
                        MW.scan_running = false;
                    });
                }
            }, TaskCreationOptions.AttachedToParent);
        }

        private void RedoScan()
        {
            //LoadEngine();

            if (REDO.Count == TOTAL)
            {
                LoadEngine();
            }

            if (REDO.Count > 0)
            {
                if (CORES > 1)
                {
                    foreach (string rep in REDO.Keys)
                    {
                        if (REDO[rep] <= 3)
                        {
                            REDO[rep]++;
                            Interlocked.Increment(ref TOTAL);
                            Enqueue(rep);
                        } else
                        {
                            int myval = 0;
                            REDO.TryRemove(rep, out myval);
                        }
                    }
                } else
                {
                    Task sequ = Task.Factory.StartNew(() =>
                    {
                        foreach (string rep in REDO.Keys)
                        {
                            if (REDO[rep] <= 3)
                            {
                                REDO[rep]++;
                                Interlocked.Increment(ref TOTAL);
                                DecodePython(rep);
                            } else
                            {
                                int myval = 0;
                                REDO.TryRemove(rep, out myval);

                                if (SKIP.ContainsKey(rep)) SKIP[rep]++;
                                else SKIP.TryAdd(rep, 1);

                            }
                        }

                    }, TaskCreationOptions.AttachedToParent);

                }
            }
            //_jobs_decode.CompleteAdding();
        }

        public void DecodePython(string rep)
        {
            Interlocked.Increment(ref THREADS);
            Log("Threads running: " + THREADS);
            

            string id = Path.GetFileNameWithoutExtension(rep);
            Log("Working on " + id);
            string reppath = Path.GetDirectoryName(rep);
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(reppath);
            MD5 md5 = new MD5CryptoServiceProvider();
            string reppath_md5 = System.BitConverter.ToString(md5.ComputeHash(plainTextBytes));
            string repid = reppath_md5 + "/" + id;
            REParea area = AREA;
            
            
            ScriptScope threadsave_scope = null;
            lock (SCOPE)
            {
                threadsave_scope = SCOPE;
            }
            

            /**
            ScriptEngine engine = LoadEngine();
            ScriptScope threadsave_scope = engine.CreateScope();

            dynamic result = engine.ExecuteFile(EXEDIR + @"\pylib\site-packages\mpyq.py", threadsave_scope);
            if (result != null) Console.WriteLine(result);
            result = engine.Execute("from s2protocol import versions", threadsave_scope);
            if (result != null) Console.WriteLine(result);
            **/

            Log("Loading s2protocol ..");
            //dynamic MPQArchive = SCOPE.GetVariable("MPQArchive");
            dynamic MPQArchive = threadsave_scope.GetVariable("MPQArchive");
            dynamic archive = null;
            dynamic files = null;
            dynamic contents = null;
            dynamic versions = null;
            try
            {
                archive = MPQArchive(rep);
                files = archive.extract();
                contents = archive.header["user_data_header"]["content"];

                //versions = SCOPE.GetVariable("versions");
                versions = threadsave_scope.GetVariable("versions");
            }
            catch
            {
                if (SKIP.ContainsKey(rep)) SKIP[rep]++;
                else SKIP.TryAdd(rep, 1);
                Interlocked.Increment(ref TOTAL_DONE);
                Interlocked.Decrement(ref THREADS);
                if (DEBUG > 0) Console.WriteLine("No MPQArchive for " + id);
                return;
            }
            dynamic header = null;
            try
            {
                lock (_locker)
                {
                    header = versions.latest().decode_replay_header(contents);
                }
            }
            catch (Exception e)
            {
                if (DEBUG > 0) Console.WriteLine(e.Message);
                
                if (DEBUG > 0) Console.WriteLine("No header for " + id);
                lock (ENGINE)
                {
                    lock (SCOPE)
                    {
                        ENGINE.Execute("from s2protocol import versions", SCOPE);

                    }
                    ENGINE.Execute("from s2protocol import versions", threadsave_scope);
                }
                if (REDO.ContainsKey(rep))
                {
                    //REDO[rep] = REDO[rep] + 1;
                }
                else
                {
                    REDO.TryAdd(rep, 1);
                }
                Interlocked.Increment(ref TOTAL_DONE);
                Interlocked.Decrement(ref THREADS);
            }
            finally
            {
                //header = versions.latest().decode_replay_header(contents);
            }
            if (header == null) return;

            if (header != null)
            {
                Log("Loading s2protocol header finished");
                var baseBuild = header["m_version"]["m_baseBuild"];
                dynamic protocol = null;
                try
                {
                    protocol = versions.build(baseBuild);
                }
                catch
                {
                    if (DEBUG > 0) Console.WriteLine("No protocol found for " + id);
                    Interlocked.Increment(ref TOTAL_DONE);
                    Interlocked.Decrement(ref THREADS);
                }
                if (protocol == null) return;
                Log("Loading s2protocol protocol finished");
                var details_enc = archive.read_file("replay.details");
                dynamic details_dec = null;
                try
                {
                    details_dec = protocol.decode_replay_details(details_enc);
                }
                catch
                {
                    Console.WriteLine("No Version for " + id);
                    lock (ENGINE)
                    {
                        lock (SCOPE)
                        {
                            ENGINE.Execute("from s2protocol import versions", SCOPE);

                        }
                        ENGINE.Execute("from s2protocol import versions", threadsave_scope);
                    }
                    if (REDO.ContainsKey(rep))
                    {
                        //REDO[rep] = REDO[rep] + 1;
                    }
                    else
                    {
                        REDO.TryAdd(rep, 1);
                    }
                    Interlocked.Increment(ref TOTAL_DONE);
                    Interlocked.Decrement(ref THREADS);
                }
                finally
                {
                    //details_dec = protocol.decode_replay_details(details_enc);

                }
                if (details_dec == null) return;
                Log("Loading s2protocol details finished");
                dsreplay replay = new dsreplay();
                replay.REPLAY = repid;
                Log("Replay id: " + repid);
                string names = id + ";";
                int failsafe_pos = 0;
                foreach (var player in details_dec["m_playerList"])
                {
                    failsafe_pos++;
                    string name = "";
                    IronPython.Runtime.Bytes bab = null;
                    try
                    {
                        bab = player["m_name"];
                    }
                    catch { }

                    if (bab != null) name = Encoding.UTF8.GetString(bab.ToByteArray());
                    else name = player["m_name"].ToString();

                    Match m2 = rx_subname.Match(name);
                    if (m2.Success) name = m2.Groups[1].Value;
                    Log("Replay playername: " + name);
                    dsplayer pl = new dsplayer();

                    pl.NAME = name;
                    pl.RACE = player["m_race"].ToString();
                    Log("Replay race: " + pl.RACE);
                    pl.RESULT = int.Parse(player["m_result"].ToString());
                    pl.TEAM = int.Parse(player["m_teamId"].ToString());
                    try
                    {
                        pl.POS = int.Parse(player["m_workingSetSlotId"].ToString()) + 1;
                    } catch
                    {
                        pl.POS = failsafe_pos;
                    }

                    names += pl.POS + ";";
                    names += pl.NAME + ";";
                    names += pl.RACE + ";";
                    names += pl.TEAM + ";";
                    names += pl.RESULT + ";";

                    replay.PLAYERS.Add(pl);
                }

                long offset = long.Parse(details_dec["m_timeLocalOffset"].ToString());
                long timeutc = long.Parse(details_dec["m_timeUTC"].ToString());

                long georgian = timeutc + offset;
                DateTime gametime = DateTime.FromFileTime(georgian);
                replay.GAMETIME = double.Parse(gametime.ToString("yyyyMMddhhmmss"));
                Log("Replay gametime: " + replay.GAMETIME);
                names += replay.GAMETIME + ";";

                var trackerevents_enc = archive.read_file("replay.tracker.events");
                dynamic trackerevents_dec = null;
                try
                {
                    trackerevents_dec = protocol.decode_replay_tracker_events(trackerevents_enc);
                    Log("Loading trackerevents success");
                }
                catch
                {
                    if (DEBUG > 0) Console.WriteLine("No tracker version for " + id);
                    Log("Loading trackerevents failed");
                    lock (ENGINE)
                    {
                        lock (SCOPE)
                        {
                            ENGINE.Execute("from s2protocol import versions", SCOPE);

                        }
                        ENGINE.Execute("from s2protocol import versions", threadsave_scope);
                    }
                    if (REDO.ContainsKey(rep))
                    {
                        //REDO[rep] = REDO[rep] + 1;
                    }
                    else
                    {
                        REDO.TryAdd(rep, 1);
                    }
                    Interlocked.Increment(ref TOTAL_DONE);
                    Interlocked.Decrement(ref THREADS);
                }
                finally
                {
                    //trackerevents_dec = protocol.decode_replay_tracker_events(trackerevents_enc);

                }
                if (trackerevents_dec == null) return;
                Log("Loading s2protocol trackerevents finished");
                REPtrackerevents track = new REPtrackerevents();
                foreach (dsplayer pl in replay.PLAYERS)
                {
                    //track.PLAYERS.Add(pl.POS + 1, pl);
                    track.PLAYERS.Add(pl.POS, pl);
                }

                int i = 0;
                bool fix = false;
                bool isBrawl_set = false;
                HashSet<string> Mutation = new HashSet<string>();

                Dictionary<int, REPvec> UNITPOS = new Dictionary<int, REPvec>();
                foreach (IronPython.Runtime.PythonDictionary pydic in trackerevents_dec)
                {

                    if (pydic.ContainsKey("m_unitTypeName"))
                    {
                        if (pydic.ContainsKey("m_controlPlayerId"))
                        {
                            int playerid = int.Parse(pydic["m_controlPlayerId"].ToString());
                            Match m = rx_race2.Match(pydic["m_unitTypeName"].ToString());
                            if (m.Success && m.Groups[1].Value.Length > 0)
                            {
                                string race = m.Groups[1].Value;
                                names += race + ";";

                                if (track.PLAYERS.ContainsKey(playerid))
                                {
                                    track.PLAYERS[playerid].RACE = race;

                                    if (fix == false)
                                    {
                                        if (race == "Stukov" || race == "Horner" || race == "Zagara" || race == "Kerrigan" || race == "Alarak" || race == "Nova")
                                        {
                                            if (replay.GAMETIME <= 20190121000000)
                                            {
                                                fix = true;
                                                if (race == "Nova") track.PLAYERS[playerid].ARMY += 250;
                                                else if (race == "Zagara") track.PLAYERS[playerid].ARMY += 275;
                                                else if (race == "Alarak") track.PLAYERS[playerid].ARMY += 300;
                                                else if (race == "Kerrigan") track.PLAYERS[playerid].ARMY += 400;
                                            }
                                        }
                                    }

                                }
                                else
                                {
                                    if (DEBUG > 0) Console.WriteLine("No player for " + playerid);
                                }

                            }
                            else if (pydic.ContainsKey("m_creatorAbilityName") && pydic["m_creatorAbilityName"] != null)
                            {
                                m = rx_unit.Match(pydic["m_creatorAbilityName"].ToString());
                                if (m.Success)
                                {
                                    int gameloop = int.Parse(pydic["_gameloop"].ToString());
                                    string unit = m.Groups[1].Value;
                                    if (m.Groups[2].Value.Length > 0) unit += m.Groups[2].Value;

                                    // failsafe double tychus
                                    //if (unit == "TychusTychus")
                                    if (pydic.ContainsKey("m_unitTypeName"))
                                    {
                                        if (pydic["m_unitTypeName"].ToString() == "UnitBirthBar")
                                        {
                                            //Console.WriteLine(unit);
                                            continue;
                                        }
                                    }
                                            
                                        
                                    foreach (var bp in BREAKPOINTS)
                                    {
                                        
                                        if (bp.Value > 0 && gameloop > bp.Value) continue;


                                        if (track.UNITS.ContainsKey(playerid))
                                        {
                                            if (track.UNITS[playerid].ContainsKey(bp.Key))
                                            {
                                                if (track.UNITS[playerid][bp.Key].ContainsKey(unit)) track.UNITS[playerid][bp.Key][unit] = track.UNITS[playerid][bp.Key][unit] + 1;
                                                else track.UNITS[playerid][bp.Key].Add(unit, 1);
                                            }
                                            else
                                            {
                                                track.UNITS[playerid].Add(bp.Key, new Dictionary<string, int>());
                                                track.UNITS[playerid][bp.Key].Add(unit, 1);
                                            }
                                        }
                                        else
                                        {
                                            track.UNITS.Add(playerid, new Dictionary<string, Dictionary<string, int>>());
                                            track.UNITS[playerid].Add(bp.Key, new Dictionary<string, int>());
                                            track.UNITS[playerid][bp.Key].Add(unit, 1);
                                        }
                                    }


                                    if (track.PLAYERS[playerid].REALPOS == 0)
                                    {
                                        int x = int.Parse(pydic["m_x"].ToString());
                                        int y = int.Parse(pydic["m_y"].ToString());

                                        int pos = area.GetPos(x, y);

                                        if (pos > 0)
                                        {
                                            foreach (dsplayer fpl in track.PLAYERS.Values)
                                            {
                                                if (fpl.REALPOS == pos)
                                                {
                                                    if (UNITPOS.ContainsKey(fpl.POS) && DEBUG > 0) Console.WriteLine(id + " Double pos: X: " + x + " Y: " + y + " POS:" + track.PLAYERS[playerid].POS + " REALPOS: " + pos + " (DX: " + UNITPOS[fpl.POS].x + " DY: " + UNITPOS[fpl.POS].y + " DPOS: " + fpl.POS + " DREALPOS: " + fpl.REALPOS + ")");
                                                }
                                            }
                                            if (!UNITPOS.ContainsKey(playerid)) UNITPOS.Add(playerid, new REPvec(x, y));
                                            track.PLAYERS[playerid].REALPOS = pos;
                                        }
                                    }

                                    if (fix == true)
                                    {
                                        if (unit == "StukovInfestedBunker") track.PLAYERS[playerid].ARMY += 375;
                                        else if (unit == "HornerAssaultGalleon") track.PLAYERS[playerid].ARMY += 475;

                                    }
                                }
                            }
                        }
                    }


                    else if (pydic.ContainsKey("m_stats"))
                    {
                        int playerid = int.Parse(pydic["m_playerId"].ToString());
                        int gameloop = int.Parse(pydic["_gameloop"].ToString());
                        int spawn = (gameloop - 480) % 1440;
                        int pos = 0;
                        if (isBrawl_set == false)
                        {
                            if (Mutation.Contains("MutationExpansion") && replay.GAMEMODE == "GameModeCommanders")
                                replay.GAMEMODE = "GameModeCommandersHeroic";
                            else if (Mutation.Contains("MutationCovenant"))
                                replay.GAMEMODE = "GameModeSwitch";
                            else if (Mutation.Contains("MutationEquipment"))
                                replay.GAMEMODE = "GameModeGear";
                            else if (Mutation.Contains("MutationExile")
                                    && Mutation.Contains("MutationRescue")
                                    && Mutation.Contains("MutationShroud")
                                    && Mutation.Contains("MutationSuperscan"))
                                replay.GAMEMODE = "GameModeSabotage";
                            else
                                if (replay.GAMEMODE == "unknown")
                                replay.GAMEMODE = "GameModeStandard";

                            replay.ISBRAWL = true;
                            if (replay.GAMEMODE == "GameModeCommanders" || replay.GAMEMODE == "GameModeCommandersHeroic" || replay.GAMEMODE == "GameModeStandard")
                                replay.ISBRAWL = false;

                            isBrawl_set = true;
                        }

                        if (track.PLAYERS.ContainsKey(playerid))
                        {
                            if (track.PLAYERS[playerid].REALPOS > 0) pos = track.PLAYERS[playerid].REALPOS;
                            else pos = track.PLAYERS[playerid].POS;

                            IronPython.Runtime.PythonDictionary pystats = pydic["m_stats"] as IronPython.Runtime.PythonDictionary;
                            track.PLAYERS[playerid].KILLSUM = int.Parse(pystats["m_scoreValueMineralsKilledArmy"].ToString());
                            track.PLAYERS[playerid].INCOME += int.Parse(pystats["m_scoreValueMineralsCollectionRate"].ToString()) / 9.15;
                            if (pos > 0)
                            {
                                bool playerspawn = false;
                                if (spawn == 0 && (pos == 1 || pos == 4)) playerspawn = true;
                                if (spawn == 480 && (pos == 2 || pos == 5)) playerspawn = true;
                                if (spawn == 960 && (pos == 3 || pos == 6)) playerspawn = true;
                                if (playerspawn == true) track.PLAYERS[playerid].ARMY += int.Parse(pystats["m_scoreValueMineralsUsedActiveForces"].ToString());
                            }

                            replay.DURATION = int.Parse(pydic["_gameloop"].ToString());
                            track.PLAYERS[playerid].PDURATION = replay.DURATION;
                            i++;
                        }
                        else
                        {
                            if (DEBUG > 0) Console.WriteLine("No player for " + playerid);
                        }

                    }
                    else if (isBrawl_set == false && pydic.ContainsKey("_gameloop") && (int)pydic["_gameloop"] == 0 && pydic.ContainsKey("m_upgradeTypeName"))
                    {
                        if (pydic["m_upgradeTypeName"].ToString() == "GameModeBrawl")
                            replay.GAMEMODE = "GameModeBrawl";
                        else if (pydic["m_upgradeTypeName"].ToString() == "GameModeCommanders")
                            replay.GAMEMODE = "GameModeCommanders";
                        else if (pydic["m_upgradeTypeName"].ToString().StartsWith("Mutation"))
                            Mutation.Add(pydic["m_upgradeTypeName"].ToString());
                    }

                    i++;

                }

                replay.PLAYERCOUNT = replay.PLAYERS.Count;

                // fail safe
                FixPos(replay);
                FixWinner(replay);

                foreach (dsplayer pl in replay.PLAYERS)
                {
                    pl.INCOME = Math.Round(pl.INCOME, 2);
                    if (track.UNITS.ContainsKey(pl.POS)) pl.UNITS = track.UNITS[pl.POS];
                }
                Interlocked.Increment(ref REPID);
                replay.ID = REPID;
                //if (!replaysng.ContainsKey(repid)) replaysng.TryAdd(repid, replay);
                Save(MW.myStats_json, replay);
            }

            if (REDO.ContainsKey(rep))
            {
                int myval = 0;
                REDO.TryRemove(rep, out myval);
            }


            Interlocked.Increment(ref TOTAL_DONE);
            double wr = 0;
            if (TOTAL > 0) wr = TOTAL_DONE * 100 / TOTAL;
            wr = Math.Round(wr, 2);
            if (DEBUG > 0) Console.WriteLine(TOTAL_DONE + "/" + TOTAL + " done. (" + wr.ToString() + "%)");

            if (TOTAL_DONE >= TOTAL)
            {
                DateTime end = DateTime.UtcNow;
                TimeSpan timeDiff = end - START;
                Console.WriteLine(timeDiff.TotalSeconds);

                if (REDO.Count > 0)
                {
                    if (DEBUG > 0) Console.WriteLine("REDO: " + REDO.Count);
                    RedoScan();
                } else
                {
                    Stop_decode();
                }
            }

            Log("Decoding " + id + " complete.");
            Interlocked.Decrement(ref THREADS);
        }

        public void FixWinner(dsreplay replay)
        {
            bool player = false;
            foreach (dsplayer pl in replay.PLAYERS)
            {
                if (MW.player_list.Contains(pl.NAME))
                {
                    player = true;
                    int oppteam;
                    if (pl.TEAM == 0) oppteam = 1;
                    else oppteam = 0;

                    if (pl.RESULT == 1) replay.WINNER = pl.TEAM;
                    else replay.WINNER = oppteam;
                    break;
                }
            }

            if (player == false)
            {
                foreach (dsplayer pl in replay.PLAYERS)
                {
                    if (pl.RESULT == 1)
                    {
                        int oppteam;
                        if (pl.TEAM == 0) oppteam = 1;
                        else oppteam = 0;

                        replay.WINNER = pl.TEAM;
                        break;
                    }
                }
            }

            foreach (dsplayer pl in replay.PLAYERS)
            {
                if (pl.TEAM == replay.WINNER) pl.RESULT = 1;
                else pl.RESULT = 2;
            }

        }

        public void FixPos(dsreplay replay)
        {
            foreach (dsplayer pl in replay.PLAYERS)
            {
                if (pl.REALPOS == 0)
                {
                    for (int j = 1; j <= 6; j++)
                    {
                        if (replay.PLAYERCOUNT == 2 && (j == 2 || j == 3 || j == 5 || j == 6)) continue;
                        if (replay.PLAYERCOUNT == 4 && (j == 3 || j == 6)) continue;

                        List<dsplayer> temp = new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == j).ToList());
                        if (temp.Count == 0)
                        {
                            pl.REALPOS = j;
                            if (DEBUG > 0) Console.WriteLine("Fixing missing playerid for " + pl.POS + "|" + pl.REALPOS + " => " + j);
                        }
                    }



                    if (new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == pl.POS).ToList()).Count == 0) pl.REALPOS = pl.POS;

                }

                if (new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == pl.REALPOS).ToList()).Count > 1)
                {
                    Console.WriteLine("Found double playerid for " + pl.POS + "|" + pl.REALPOS);

                    for (int j = 1; j <= 6; j++)
                    {
                        if (replay.PLAYERCOUNT == 2 && (j == 2 || j == 3 || j == 5 || j == 6)) continue;
                        if (replay.PLAYERCOUNT == 4 && (j == 3 || j == 6)) continue;
                        if (new List<dsplayer>(replay.PLAYERS.Where(x => x.REALPOS == j).ToList()).Count == 0)
                        {
                            pl.REALPOS = j;
                            if (DEBUG > 0) Console.WriteLine("Fixing double playerid for " + pl.POS + "|" + pl.REALPOS + " => " + j);
                            break;
                        }
                    }

                }

            }
        }

        public void Save(string out_file, dsreplay rep)
        {

            TextWriter writer = null;
            _readWriteLock.EnterWriteLock();
            try
            {
                var repjson = JsonConvert.SerializeObject(rep);

                writer = new StreamWriter(out_file, true, Encoding.UTF8);
                writer.Write(repjson + Environment.NewLine);

            }
            catch
            {
                if (DEBUG > 0) Console.WriteLine("Failed writing to json :(");
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
            _readWriteLock.ExitWriteLock();

        }

        public int ReadFromJsonFile()
        {
            string filePath = MW.myStats_json;
            TextReader reader = null;
            dsreplay rep = null;
            int maxid = 0;
            try
            {
                reader = new StreamReader(filePath, Encoding.UTF8);
                string fileContents;
                while ((fileContents = reader.ReadLine()) != null)
                {
                    rep = JsonConvert.DeserializeObject<dsreplay>(fileContents);
                    if (rep != null)
                    {
                        replaysng.TryAdd(rep.REPLAY, rep);
                        if (rep.ID > maxid) maxid = rep.ID;
                    }
                }
            }
            finally
            {
                if (reader != null)
                    reader.Close();
            }
            return maxid;
        }

        private void Decode(string rep)
        {
            Interlocked.Increment(ref THREADS);
            Console.WriteLine("Threads running: " + THREADS);

            Process doit = new Process();
            string id = Path.GetFileNameWithoutExtension(rep);
            string out_file = OUTDIR + @"\" + id;
            Dictionary<string, List<string>> detail = new Dictionary<string, List<string>>();
            Console.WriteLine("Working on " + id);
            foreach (string get in GETPOOL)
            {
                detail.Add(get, new List<string>());
                out_file = OUTDIR + @"\" + id + "_" + get + ".txt";
                Console.WriteLine("Outfile: " + out_file);
                if (File.Exists(out_file))
                {
                    Console.WriteLine("Reading in " + id);
                    detail[get].AddRange(File.ReadAllLines(out_file).ToList());
                }
                else
                {
                    Console.WriteLine("Decoding " + rep);
                    //string Arguments = "\"" + rep + "\"" + " --details > \"" + out_file + "\"";
                    string Arguments = "\"" + rep + "\"" + " --" + get;




                    if (File.Exists(MW.myS2cli_exe))
                    {
                        doit.StartInfo.FileName = MW.myS2cli_exe;
                        doit.StartInfo.Arguments = Arguments;

                        doit.StartInfo.UseShellExecute = false;
                        doit.StartInfo.RedirectStandardOutput = true;
                        doit.StartInfo.RedirectStandardError = false;

                        doit.Start();
                        while (!doit.StandardOutput.EndOfStream)
                        {
                            detail[get].Add(doit.StandardOutput.ReadLine());
                        }
                        doit.WaitForExit();
                    }
                }

                Console.WriteLine("Finished decoding " + id);
            }
            ParseDetail(id, detail);

            /**
            scandetail scdetail = new scandetail();
            scdetail.REP = id;
            scdetail.DETAILS = new List<string>(detail);
            _jobs_parse.Add(scdetail);
            **/

            Interlocked.Increment(ref TOTAL_DONE);
            Console.WriteLine(TOTAL_DONE + "/" + TOTAL + " done.");

            Interlocked.Decrement(ref THREADS);
        }

        private void ParseDetail(string rep, Dictionary<string, List<string>> detail)
        {
            Interlocked.Increment(ref THREADS_PARSE);
            Console.WriteLine("Parse Threads running: " + THREADS_PARSE);

            long offset = 0;

            string names = rep + ";";
            if (detail.ContainsKey("details") && detail.ContainsKey("trackerevents"))
            {
                foreach (string line in detail["details"])
                {
                    Match m = rx_name.Match(line);
                    if (m.Success)
                    {
                        Match m2 = rx_subname.Match(m.Groups[1].Value);
                        if (m2.Success) names += m2.Groups[1].Value;
                        else names += m.Groups[1].Value;
                        names += ";";
                    }
                    else
                    {
                        m = rx_race.Match(line);
                        if (m.Success) names += m.Groups[1].Value + ";";
                        else
                        {
                            m = rx_result.Match(line);
                            if (m.Success) names += m.Groups[1].Value + ";";
                            else
                            {
                                m = rx_team.Match(line);
                                if (m.Success) names += m.Groups[1].Value + ";";
                                else
                                {
                                    m = rx_workingset.Match(line);
                                    if (m.Success) names += m.Groups[1].Value + ";";
                                    else
                                    {
                                        m = rx_offset.Match(line);
                                        if (m.Success)
                                        {
                                            //names += m.Groups[1].Value + ";";
                                            offset = long.Parse(m.Groups[1].Value);
                                        }
                                        else
                                        {
                                            m = rx_time.Match(line);
                                            if (m.Success)
                                            {
                                                long georgian = long.Parse(m.Groups[1].Value);
                                                georgian = georgian + offset;

                                                DateTime gametime = DateTime.FromFileTime(georgian);

                                                names += gametime.ToString("yyyyMMddhhmmss") + ";";
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }
                names += Environment.NewLine;

                _readWriteLock.EnterWriteLock();
                try
                {
                    File.AppendAllText(OUTFILE, names);
                }
                finally
                {
                    _readWriteLock.ExitWriteLock();
                }
            }
            Interlocked.Decrement(ref THREADS_PARSE);
        }

        public Encoding detectTextEncoding(byte[] b, out String text, int taster = 1000)
        {
            //byte[] b = File.ReadAllBytes(filename);

            //////////////// First check the low hanging fruit by checking if a
            //////////////// BOM/signature exists (sourced from http://www.unicode.org/faq/utf_bom.html#bom4)
            if (b.Length >= 4 && b[0] == 0x00 && b[1] == 0x00 && b[2] == 0xFE && b[3] == 0xFF) { text = Encoding.GetEncoding("utf-32BE").GetString(b, 4, b.Length - 4); return Encoding.GetEncoding("utf-32BE"); }  // UTF-32, big-endian 
            else if (b.Length >= 4 && b[0] == 0xFF && b[1] == 0xFE && b[2] == 0x00 && b[3] == 0x00) { text = Encoding.UTF32.GetString(b, 4, b.Length - 4); return Encoding.UTF32; }    // UTF-32, little-endian
            else if (b.Length >= 2 && b[0] == 0xFE && b[1] == 0xFF) { text = Encoding.BigEndianUnicode.GetString(b, 2, b.Length - 2); return Encoding.BigEndianUnicode; }     // UTF-16, big-endian
            else if (b.Length >= 2 && b[0] == 0xFF && b[1] == 0xFE) { text = Encoding.Unicode.GetString(b, 2, b.Length - 2); return Encoding.Unicode; }              // UTF-16, little-endian
            else if (b.Length >= 3 && b[0] == 0xEF && b[1] == 0xBB && b[2] == 0xBF) { text = Encoding.UTF8.GetString(b, 3, b.Length - 3); return Encoding.UTF8; } // UTF-8
            else if (b.Length >= 3 && b[0] == 0x2b && b[1] == 0x2f && b[2] == 0x76) { text = Encoding.UTF7.GetString(b, 3, b.Length - 3); return Encoding.UTF7; } // UTF-7


            //////////// If the code reaches here, no BOM/signature was found, so now
            //////////// we need to 'taste' the file to see if can manually discover
            //////////// the encoding. A high taster value is desired for UTF-8
            if (taster == 0 || taster > b.Length) taster = b.Length;    // Taster size can't be bigger than the filesize obviously.


            // Some text files are encoded in UTF8, but have no BOM/signature. Hence
            // the below manually checks for a UTF8 pattern. This code is based off
            // the top answer at: https://stackoverflow.com/questions/6555015/check-for-invalid-utf8
            // For our purposes, an unnecessarily strict (and terser/slower)
            // implementation is shown at: https://stackoverflow.com/questions/1031645/how-to-detect-utf-8-in-plain-c
            // For the below, false positives should be exceedingly rare (and would
            // be either slightly malformed UTF-8 (which would suit our purposes
            // anyway) or 8-bit extended ASCII/UTF-16/32 at a vanishingly long shot).
            int i = 0;
            bool utf8 = false;
            while (i < taster - 4)
            {
                if (b[i] <= 0x7F) { i += 1; continue; }     // If all characters are below 0x80, then it is valid UTF8, but UTF8 is not 'required' (and therefore the text is more desirable to be treated as the default codepage of the computer). Hence, there's no "utf8 = true;" code unlike the next three checks.
                if (b[i] >= 0xC2 && b[i] <= 0xDF && b[i + 1] >= 0x80 && b[i + 1] < 0xC0) { i += 2; utf8 = true; continue; }
                if (b[i] >= 0xE0 && b[i] <= 0xF0 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0) { i += 3; utf8 = true; continue; }
                if (b[i] >= 0xF0 && b[i] <= 0xF4 && b[i + 1] >= 0x80 && b[i + 1] < 0xC0 && b[i + 2] >= 0x80 && b[i + 2] < 0xC0 && b[i + 3] >= 0x80 && b[i + 3] < 0xC0) { i += 4; utf8 = true; continue; }
                utf8 = false; break;
            }
            if (utf8 == true)
            {
                text = Encoding.UTF8.GetString(b);
                return Encoding.UTF8;
            }


            // The next check is a heuristic attempt to detect UTF-16 without a BOM.
            // We simply look for zeroes in odd or even byte places, and if a certain
            // threshold is reached, the code is 'probably' UF-16.          
            double threshold = 0.1; // proportion of chars step 2 which must be zeroed to be diagnosed as utf-16. 0.1 = 10%
            int count = 0;
            for (int n = 0; n < taster; n += 2) if (b[n] == 0) count++;
            if (((double)count) / taster > threshold) { text = Encoding.BigEndianUnicode.GetString(b); return Encoding.BigEndianUnicode; }
            count = 0;
            for (int n = 1; n < taster; n += 2) if (b[n] == 0) count++;
            if (((double)count) / taster > threshold) { text = Encoding.Unicode.GetString(b); return Encoding.Unicode; } // (little-endian)


            // Finally, a long shot - let's see if we can find "charset=xyz" or
            // "encoding=xyz" to identify the encoding:
            for (int n = 0; n < taster - 9; n++)
            {
                if (
                    ((b[n + 0] == 'c' || b[n + 0] == 'C') && (b[n + 1] == 'h' || b[n + 1] == 'H') && (b[n + 2] == 'a' || b[n + 2] == 'A') && (b[n + 3] == 'r' || b[n + 3] == 'R') && (b[n + 4] == 's' || b[n + 4] == 'S') && (b[n + 5] == 'e' || b[n + 5] == 'E') && (b[n + 6] == 't' || b[n + 6] == 'T') && (b[n + 7] == '=')) ||
                    ((b[n + 0] == 'e' || b[n + 0] == 'E') && (b[n + 1] == 'n' || b[n + 1] == 'N') && (b[n + 2] == 'c' || b[n + 2] == 'C') && (b[n + 3] == 'o' || b[n + 3] == 'O') && (b[n + 4] == 'd' || b[n + 4] == 'D') && (b[n + 5] == 'i' || b[n + 5] == 'I') && (b[n + 6] == 'n' || b[n + 6] == 'N') && (b[n + 7] == 'g' || b[n + 7] == 'G') && (b[n + 8] == '='))
                    )
                {
                    if (b[n + 0] == 'c' || b[n + 0] == 'C') n += 8; else n += 9;
                    if (b[n] == '"' || b[n] == '\'') n++;
                    int oldn = n;
                    while (n < taster && (b[n] == '_' || b[n] == '-' || (b[n] >= '0' && b[n] <= '9') || (b[n] >= 'a' && b[n] <= 'z') || (b[n] >= 'A' && b[n] <= 'Z')))
                    { n++; }
                    byte[] nb = new byte[n - oldn];
                    Array.Copy(b, oldn, nb, 0, n - oldn);
                    try
                    {
                        string internalEnc = Encoding.ASCII.GetString(nb);
                        text = Encoding.GetEncoding(internalEnc).GetString(b);
                        return Encoding.GetEncoding(internalEnc);
                    }
                    catch { break; }    // If C# doesn't recognize the name of the encoding, break.
                }
            }


            // If all else fails, the encoding is probably (though certainly not
            // definitely) the user's local codepage! One might present to the user a
            // list of alternative encodings as shown here: https://stackoverflow.com/questions/8509339/what-is-the-most-common-encoding-of-each-language
            // A full list can be found using Encoding.GetEncodings();
            text = Encoding.Default.GetString(b);
            return Encoding.Default;
        }



        private class scandetail
        {
            public string REP { get; set; }
            public List<string> DETAILS { get; set; }
        }

        private class REPdetail
        {
            List<dsplayer> PLAYERS { get; set; } = new List<dsplayer>();
            long OFFSET { get; set; } = 0;
            double GAMETIME { get; set; } = 0;
        }

        private class REPtrackerevents
        {
            public Dictionary<int, dsplayer> PLAYERS { get; set; } = new Dictionary<int, dsplayer>();
            public Dictionary<int, Dictionary<string, Dictionary<string, int>>> UNITS { get; set; } = new Dictionary<int, Dictionary<string, Dictionary<string, int>>>();


        }

        private class REPvec
        {
            public int x { get; set; }
            public int y { get; set; }

            public REPvec(int X, int Y)
            {
                x = X;
                y = Y;
            }
        }

        private class REParea
        {
            public Dictionary<int, Dictionary<string, REPvec>> POS { get; set; } = new Dictionary<int, Dictionary<string, REPvec>>();

            public REParea()
            {

                POS.Add(1, new Dictionary<string, REPvec>());
                POS[1].Add("A", new REPvec(115, 202));
                POS[1].Add("B", new REPvec(154, 177));
                POS[1].Add("C", new REPvec(184, 208));
                POS[1].Add("D", new REPvec(153, 239));

                POS.Add(2, new Dictionary<string, REPvec>());
                POS[2].Add("A", new REPvec(151, 178));
                POS[2].Add("B", new REPvec(179, 151));
                POS[2].Add("C", new REPvec(210, 181));
                POS[2].Add("D", new REPvec(183, 208));

                POS.Add(3, new Dictionary<string, REPvec>());
                POS[3].Add("A", new REPvec(179, 151));
                POS[3].Add("B", new REPvec(206, 108));
                POS[3].Add("C", new REPvec(243, 150));
                POS[3].Add("D", new REPvec(210, 181));

                POS.Add(4, new Dictionary<string, REPvec>());
                POS[4].Add("A", new REPvec(6, 90));
                POS[4].Add("B", new REPvec(35, 56));
                POS[4].Add("C", new REPvec(69, 89));
                POS[4].Add("D", new REPvec(36, 122));

                POS.Add(5, new Dictionary<string, REPvec>());
                POS[5].Add("A", new REPvec(35, 56));
                POS[5].Add("B", new REPvec(57, 32));
                POS[5].Add("C", new REPvec(93, 65));
                POS[5].Add("D", new REPvec(69, 89));

                POS.Add(6, new Dictionary<string, REPvec>());
                POS[6].Add("A", new REPvec(57, 32));
                POS[6].Add("B", new REPvec(91, 0));
                POS[6].Add("C", new REPvec(126, 33));
                POS[6].Add("D", new REPvec(93, 65));

            }

            public int GetPos(int x, int y)
            {
                int pos = 0;

                /**
                	$indahouse = &PointInTriangle($p{ 'x'}, $p{ 'y'}, $pos{$pl}
                { 'Ax'}, $pos{$pl}
                { 'Ay'}, $pos{$pl}
                { 'Bx'}, $pos{$pl}
                { 'By'}, $pos{$pl}
                { 'Cx'}, $pos{$pl}
                { 'Cy'});
		$indahouse = &PointInTriangle($p{ 'x'}, $p{ 'y'}, $pos{$pl}
                { 'Ax'}, $pos{$pl}
                { 'Ay'}, $pos{$pl}
                { 'Dx'}, $pos{$pl}
                { 'Dy'}, $pos{$pl}
                { 'Cx'}, $pos{$pl}
                { 'Cy'}) unless $indahouse;
    **/
                bool indahouse = false;
                foreach (int plpos in POS.Keys)
                {
                    indahouse = PointInTriangle(x, y, POS[plpos]["A"].x, POS[plpos]["A"].y, POS[plpos]["B"].x, POS[plpos]["B"].y, POS[plpos]["C"].x, POS[plpos]["C"].y);
                    if (indahouse == false) indahouse = PointInTriangle(x, y, POS[plpos]["A"].x, POS[plpos]["A"].y, POS[plpos]["D"].x, POS[plpos]["D"].y, POS[plpos]["C"].x, POS[plpos]["C"].y);

                    if (indahouse == true)
                    {
                        pos = plpos;
                        break;
                    }
                }
                return pos;
            }


            private bool PointInTriangle(int Px, int Py, int Ax, int Ay, int Bx, int By, int Cx, int Cy)
            {
                bool indahouse = false;
                int b1 = 0;
                int b2 = 0;
                int b3 = 0;

                if (sign(Px, Py, Ax, Ay, Bx, By) < 0) b1 = 1;
                if (sign(Px, Py, Bx, By, Cx, Cy) < 0) b2 = 1;
                if (sign(Px, Py, Cx, Cy, Ax, Ay) < 0) b3 = 1;

                if ((b1 == b2) && (b2 == b3)) indahouse = true;
                return indahouse;
            }

            private int sign(int Ax, int Ay, int Bx, int By, int Cx, int Cy)
            {
                int sig = (Ax - Cx) * (By - Cy) - (Bx - Cx) * (Ay - Cy);
                return sig;
            }

        }

    }


}
