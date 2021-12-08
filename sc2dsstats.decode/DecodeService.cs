using IronPython.Runtime;
using Microsoft.Extensions.Logging;
using Microsoft.Scripting.Hosting;
using sc2dsstats._2022.Shared;
using sc2dsstats.db;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using static sc2dsstats.decode.Program;

namespace sc2dsstats.decode
{
    public class DecodeService
    {
        private ScriptScope scriptScope;
        private SemaphoreSlim semaphoreSlim;
        public static Version Version = new Version(4, 1);

        private int activeThreads = 0;
        private int replaysDone = 0;
        private ILogger logger;
        private object lockObject = new object();
        private CancellationToken cancellationToken;
        public ConcurrentBag<Dsreplay> Replays;
        public List<string> FailedReplays;
        public bool isRunning { get; set; } = false;
        private bool feedbackWaiting = false;
        public event EventHandler<DecodeStateEvent> DecodeStateChanged;

        public virtual void OnDecodeStateChanged(DecodeStateEvent e)
        {
            EventHandler<DecodeStateEvent> handler = DecodeStateChanged;
            handler?.Invoke(this, e);
        }

        public async Task DecodeReplays(string appPath, List<string> replayPaths, int threads, CancellationToken cancellationToken)
        {
            if (isRunning)
                return;
            isRunning = true;
            feedbackWaiting = false;
            logger = ApplicationLogging.CreateLogger<DecodeService>();
            DateTime start = DateTime.UtcNow;
            this.cancellationToken = cancellationToken;

            if (threads <= 0)
                threads = 1;
            activeThreads = 0;
            replaysDone = 0;
            Replays = new ConcurrentBag<Dsreplay>();
            FailedReplays = new List<string>();

            OnDecodeStateChanged(new DecodeStateEvent()
            {
                Threads = activeThreads,
                Done = replaysDone,
                Failed = FailedReplays.Count,
                Running = true,
                StartTime = start
            });

            if (scriptScope == null)
                scriptScope = LoadEngine(appPath);

            SemaphoreSlim sem = new SemaphoreSlim(threads, threads);
            foreach (var replayPath in replayPaths)
            {
                await sem.WaitAsync();
                var task = Task.Factory.StartNew(() =>
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        DecodeFeedback(start);
                        DecodeReplay(replayPath, cancellationToken);
                    }
                }, cancellationToken);
                var ftask = task.ContinueWith((antecedent) =>
                {
                    sem.Release();
                    if (replaysDone == replayPaths.Count)
                    {
                        if (isRunning)
                        {
                            lock (lockObject)
                            {
                                isRunning = false;
                                OnDecodeStateChanged(new DecodeStateEvent()
                                {
                                    Threads = activeThreads,
                                    Done = replaysDone,
                                    Failed = FailedReplays.Count,
                                    Running = false,
                                    StartTime = start
                                });
                            }
                        }
                    }
                });
            }
            if (cancellationToken.IsCancellationRequested)
            {
                OnDecodeStateChanged(new DecodeStateEvent()
                {
                    Threads = activeThreads,
                    Done = replaysDone,
                    Failed = FailedReplays.Count,
                    Running = false,
                    StartTime = start
                });
                isRunning = false;
            }
        }

        private async Task DecodeFeedback(DateTime start)
        {
            if (feedbackWaiting)
                return;
            feedbackWaiting = true;
            await Task.Delay(1000);
            OnDecodeStateChanged(new DecodeStateEvent()
            {
                Threads = activeThreads,
                Done = replaysDone,
                Failed = FailedReplays.Count,
                Running = true,
                StartTime = start
            });
            feedbackWaiting = false;
        }

        private void DecodeReplay(string replayPath, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return;

            Interlocked.Increment(ref activeThreads);

            string id = Path.GetFileNameWithoutExtension(replayPath);
            logger.LogDebug($"Decoding replay {id} on {activeThreads} threads.");
            try
            {
                dynamic MPQArchive = scriptScope.GetVariable("MPQArchive");

                var archive = MPQArchive(replayPath);
                var contents = archive.header["user_data_header"]["content"];
                var versions = scriptScope.GetVariable("versions");

                dynamic header = null;
                lock (lockObject)
                {
                    header = versions.latest().decode_replay_header(contents);
                }
                var baseBuild = header["m_version"]["m_baseBuild"];
                var protocol = versions.build(baseBuild);

                // details
                var details_enc = archive.read_file("replay.details");

                lib.Models.DSReplay replay = null;
                var details_dec = protocol.decode_replay_details(details_enc);
                replay = DetailsService.GetDetails(details_dec);

                if (replay == null)
                    throw new Exception($"Decoding details for {id} failed.");
                replay.REPLAYPATH = replayPath;
                logger.LogDebug($"Got replay details with {replay.DSPlayer.Count} player.");

                // trackerevents
                var trackerevents_enc = archive.read_file("replay.tracker.events");
                var trackerevents_dec = protocol.decode_replay_tracker_events(trackerevents_enc);

                TrackerEventsService.GetTrackerEvents(replay, trackerevents_dec);

                Initialize.Replay(replay, false);

                var json = JsonSerializer.Serialize(replay);
                DsReplayDto newreplay = JsonSerializer.Deserialize<DsReplayDto>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });
                newreplay.VERSION = Version.ToString();
                Replays.Add(new Dsreplay(newreplay));

            }
            catch (Exception e)
            {
                FailedReplays.Add(replayPath);
                logger.LogError($"Failed decoding replay {id}: {e.Message}");
            }
            finally
            {
                Interlocked.Decrement(ref activeThreads);
                Interlocked.Increment(ref replaysDone);
            }
        }


        private ScriptScope LoadEngine(string appPath)
        {
            logger.LogInformation("Loading Python Engine");

            List<string> libs = new List<string>();
            string LibraryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            if (String.IsNullOrEmpty(LibraryPath))
            {
                LibraryPath = appPath;
                libs.Add(Path.Combine(appPath, "Lib"));
            }
            string pylib2 = Path.Combine(LibraryPath, @"pylib\site-packages");
            libs.Add(pylib2);

            logger.LogInformation($"lib: {pylib2}");

            //Dictionary<string, object> options = new Dictionary<string, object>();
            //options["Debug"] = ScriptingRuntimeHelpers.True;
            //options["ExceptionDetail"] = ScriptingRuntimeHelpers.True;
            //options["ShowClrExceptions"] = ScriptingRuntimeHelpers.True;
            //ScriptEngine engine = IronPython.Hosting.Python.CreateEngine(options);
            ScriptScope scope = null;
            try
            {
                ScriptEngine engine = IronPython.Hosting.Python.CreateEngine();

                var paths = engine.GetSearchPaths();
                foreach (var lib in libs)
                    paths.Add(lib);
                engine.SetSearchPaths(paths);

                scope = engine.CreateScope();

                dynamic result = null;
                result = engine.ExecuteFile(LibraryPath + "/pylib/site-packages/mpyq.py", scope);
                result = engine.Execute("import s2protocol", scope);
                result = engine.Execute("from s2protocol import versions", scope);
            }
            catch (Exception e)
            {
                logger.LogError(e.Message);
            }
            logger.LogInformation("Loading Python Engine done.");
            return scope;
        }

        public static string GetString(PythonDictionary pydic, string property)
        {
            object value;
            if (pydic.TryGetValue(property, out value))
            {
                if (value.GetType().Name == "Bytes")
                {
                    Bytes b = value as Bytes;
                    if (b != null)
                        return Encoding.UTF8.GetString(b.ToArray());
                    else
                        return "";
                }
                else
                    return value.ToString();
            }
            else return "";
        }

        public void Dispose()
        {
            scriptScope?.Engine.Runtime.Shutdown();
            scriptScope = null;
        }

    }
}
