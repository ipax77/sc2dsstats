using Microsoft.Scripting.Hosting;
using sc2dsstats._2022.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace sc2dsstats.decode
{
#nullable enable
    public static class DsDecodeService
    {
        private static ScriptScope? ScriptScope;
        private static object lockObject = new object();
        public static Version Version = new Version(4, 1);

        public static DsReplayDto? DecodeReplay(string appPath, string replayPath, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            var scriptScope = LoadEngine(appPath);

            if (scriptScope == null)
            {
                return null;
            }

            string id = Path.GetFileNameWithoutExtension(replayPath);
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

                if (cancellationToken.IsCancellationRequested)
                    return null;
                // details
                var details_enc = archive.read_file("replay.details");

                lib.Models.DSReplay? replay = null;
                var details_dec = protocol.decode_replay_details(details_enc);
                replay = DetailsService.GetDetails(details_dec);

                if (replay == null)
                    throw new Exception($"Decoding details for {id} failed.");
                replay.REPLAYPATH = replayPath;

                if (cancellationToken.IsCancellationRequested)
                    return null;
                // trackerevents
                var trackerevents_enc = archive.read_file("replay.tracker.events");
                var trackerevents_dec = protocol.decode_replay_tracker_events(trackerevents_enc);

                TrackerEventsService.GetTrackerEvents(replay, trackerevents_dec);

                if (cancellationToken.IsCancellationRequested)
                    return null;
                Initialize.Replay(replay, false);

                var json = JsonSerializer.Serialize(replay);
                DsReplayDto? newreplay = JsonSerializer.Deserialize<DsReplayDto>(json, new JsonSerializerOptions() { PropertyNameCaseInsensitive = true });

                if (newreplay == null)
                {
                    throw new Exception("failed deserializing DsReplayDto");
                }
                newreplay.VERSION = Version.ToString();
                return newreplay;
            }
            catch (Exception e)
            {
                Console.WriteLine($"failed decoding replay {e.Message}");
            }
            return null;
        }


        private static ScriptScope LoadEngine(string appPath)
        {
            if (ScriptScope != null)
            {
                return ScriptScope;
            }
            lock (lockObject)
            {
                if (ScriptScope != null)
                {
                    return ScriptScope;
                }
                List<string> libs = new List<string>();
                string? LibraryPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (String.IsNullOrEmpty(LibraryPath))
                {
                    LibraryPath = appPath;
                    libs.Add(Path.Combine(appPath, "Lib"));
                }
                string pylib2 = Path.Combine(LibraryPath, @"pylib\site-packages");
                libs.Add(pylib2);

                ScriptScope? scope = null;
                try
                {
                    ScriptEngine engine = IronPython.Hosting.Python.CreateEngine();

                    var paths = engine.GetSearchPaths();
                    foreach (var lib in libs)
                        paths.Add(lib);
                    engine.SetSearchPaths(paths);

                    scope = engine.CreateScope();

                    dynamic? result = null;
                    result = engine.ExecuteFile(LibraryPath + "/pylib/site-packages/mpyq.py", scope);
                    result = engine.Execute("import s2protocol", scope);
                    result = engine.Execute("from s2protocol import versions", scope);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"failed loading python engine: {e.Message}");
                }
                ScriptScope = scope;
                if (scope == null)
                {
                    throw new Exception("failed loading python engine");
                }
                return scope;
            }
        }
    }
}
