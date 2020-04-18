using IronPython.Runtime;
using Microsoft.Scripting.Hosting;
using Microsoft.Scripting.Runtime;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Text.Json;
using sc2dsstats.decode.Models;
using sc2dsstats.decode.Service;
using sc2dsstats.lib.Models;

namespace sc2dsstats.decode
{
    public class s2decode
    {
        private ScriptScope SCOPE { get; set; }
        private ScriptEngine ENGINE { get; set; }

        static readonly object _locker = new object();

        public List<string> Log { get; set; } = new List<string>();
        public int DEBUG { get; set; } = 0;

        private static ReaderWriterLockSlim _readWriteLock = new ReaderWriterLockSlim();
        ConcurrentDictionary<string, int> SKIP { get; set; } = new ConcurrentDictionary<string, int>();

        public Dictionary<string, string> ReplayFolder { get; set; } = new Dictionary<string, string>();

        public event EventHandler ScanStateChanged;
        private static ScanState args = new ScanState();
        public ConcurrentBag<dsreplay> Replays = new ConcurrentBag<dsreplay>();
        public ConcurrentBag<DSReplay> DSReplays = new ConcurrentBag<DSReplay>();

        public s2decode()
        {

        }

        protected virtual void OnScanStateChanged(ScanState e)
        {
            EventHandler handler = ScanStateChanged;
            handler?.Invoke(this, e);
        }



        /// <summary>
        /// Loading PythonEngine
        ///<param name="LibraryPath">Path to this library including the python libraries. In this path should be a Lib and pylib subdirectory.</param>
        /// </summary>
        public ScriptEngine LoadEngine(string LibraryPath, int count)
        {
            args = new ScanState();
            args.Total = count;
            args.Running = true;
            if (ENGINE != null) return ENGINE;
            AddLog("Loading Engine ..");

            string pylib2 = LibraryPath + "/pylib/site-packages";

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
            paths.Add(pylib2);
            engine.SetSearchPaths(paths);

            ScriptScope scope = engine.CreateScope();

            dynamic result = null;
            result = engine.ExecuteFile(LibraryPath + "/pylib/site-packages/mpyq.py", scope);
            if (result != null) AddLog(result.ToString());
            result = engine.Execute("import s2protocol", scope);
            if (result != null) AddLog(result);
            result = engine.Execute("from s2protocol import versions", scope);
            if (result != null) AddLog(result);
            //Thread.Sleep(1000);
            SCOPE = scope;
            ENGINE = engine;
            AddLog("Loading Engine comlete.");
            return engine;
        }

        public DSReplay DecodePython(Object stateInfo, bool toJson = true, bool GetDetails = false)
        {
            Interlocked.Increment(ref args.Threads);
            DSReplay dsreplay = null;
            string rep = (string)stateInfo;
            //Console.WriteLine("Working on rep ..");
            string id = Path.GetFileNameWithoutExtension(rep);
            AddLog("Working on rep .. " + id);
            AddLog("Loading s2protocol ..");
            dynamic MPQArchive = SCOPE.GetVariable("MPQArchive");
            dynamic archive = null;
            //dynamic files = null;
            dynamic contents = null;
            dynamic versions = null;
            try
            {
                archive = MPQArchive(rep);
                //files = archive.extract();
                contents = archive.header["user_data_header"]["content"];

                //versions = SCOPE.GetVariable("versions");
                versions = SCOPE.GetVariable("versions");
            }
            catch
            {
                AddLog("No MPQArchive for " + id);
                FailCleanup(rep, GetDetails);
                return null;
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
                AddLog("No header for " + id + ": " + e.Message + " " + versions.latest().ToString());
                FailCleanup(rep, GetDetails);
                return null;
            }

            if (header != null)
            {
                AddLog("Loading s2protocol header finished");
                var baseBuild = header["m_version"]["m_baseBuild"];
                dynamic protocol = null;
                try
                {
                    protocol = versions.build(baseBuild);
                }
                catch
                {
                    AddLog("No protocol found for " + id + " " + baseBuild.ToString());
                    FailCleanup(rep, GetDetails);
                    return null;
                }
                AddLog("Loading s2protocol protocol finished");


                // init
                /**
                var init_enc = archive.read_file("replay.initData");
                dynamic init_dec = null;
                try
                {
                    init_dec = protocol.decode_replay_initdata(init_enc);
                }
                catch
                {
                    AddLog("No Init version for " + id);
                    FailCleanup(rep, GetDetails);
                    return null;
                }
                AddLog("Loading s2protocol init finished");

                s2parse.GetInit(rep, init_dec);
                **/

                // details
                var details_enc = archive.read_file("replay.details");

                dynamic details_dec = null;
                try
                {
                    details_dec = protocol.decode_replay_details(details_enc);
                }
                catch
                {
                    AddLog("No Version for " + id);
                    FailCleanup(rep, GetDetails);
                    return null;
                }
                AddLog("Loading s2protocol details finished");

                //replay = DSparseNG.GetDetails(rep, details_dec);
                dsreplay = Details.Get(rep, details_dec);

                // trackerevents
                var trackerevents_enc = archive.read_file("replay.tracker.events");

                dynamic trackerevents_dec = null;
                try
                {
                    trackerevents_dec = protocol.decode_replay_tracker_events(trackerevents_enc);
                    AddLog("Loading trackerevents success");
                }
                catch
                {
                    AddLog("No tracker version for " + id);
                    FailCleanup(rep, GetDetails);
                    return null;
                }
                AddLog("Loading s2protocol trackerevents finished");

                try
                {
                    //replay = DSparseNG.GetTrackerevents(trackerevents_dec, replay, GetDetails);
                    dsreplay = Trackerevents.Get(trackerevents_dec, dsreplay);
                }
                catch
                {
                    AddLog("Trackerevents failed for " + id);
                    FailCleanup(rep, GetDetails);
                    return null;
                }

                AddLog("trackerevents analyzed.");

                Initialize.Replay(dsreplay, GetDetails);

                if (toJson == true)
                    DSReplays.Add(dsreplay);

            }

            Interlocked.Increment(ref args.Done);
            Interlocked.Decrement(ref args.Threads);
            if (GetDetails == false)
                OnScanStateChanged(args);

            return null;
        }


        public void SaveDS(string out_file, dsreplay rep)
        {
            if (out_file == null) return;

            TextWriter writer = null;
            _readWriteLock.EnterWriteLock();
            try
            {
                var repjson = JsonSerializer.Serialize(rep);
                writer = new StreamWriter(out_file, true, Encoding.UTF8);
                writer.Write(repjson + Environment.NewLine);
            }
            catch (Exception e)
            {
                AddLog("Failed writing to json :( " + e.Message);
            }
            finally
            {
                if (writer != null)
                    writer.Close();
            }
            _readWriteLock.ExitWriteLock();

        }

        private void FailCleanup(string replay_file, bool GetDetails)
        {
            //if (SKIP.ContainsKey(rep)) SKIP[rep]++;
            //else SKIP.TryAdd(rep, 1);

            Decode.Failed.Add(replay_file);

            Interlocked.Increment(ref args.Done);
            Interlocked.Increment(ref args.Failed);
            Interlocked.Decrement(ref args.Threads);
            if (GetDetails == false)
                OnScanStateChanged(args);
        }

        void AddLog(string msg)
        {
            if (DEBUG > 0)
            {
                Console.WriteLine(msg);
                Log.Add(msg);
            }
        }

    }

    public class ScanState : EventArgs
    {
        public int Threads = 0;
        public int Total = 0;
        public int Done = 0;
        public int Failed = 0;
        public bool Running = false;
        public DateTime Start = DateTime.Now;
        public DateTime End = DateTime.MinValue;
    }

}
