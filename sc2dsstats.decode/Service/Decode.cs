using sc2dsstats.decode.Models;
using sc2dsstats.lib.Data;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using sc2dsstats.decode;

namespace sc2dsstats.decode.Service
{
    public static class Decode
    {
        public static s2decode s2dec = new s2decode();
        private static BlockingCollection<string> _jobs_decode = new BlockingCollection<string>();
        private static CancellationTokenSource source = new CancellationTokenSource();
        private static CancellationToken token = source.Token;
        private static ManualResetEvent _empty = new ManualResetEvent(false);
        private static int CORES = 4;
        public static TimeSpan Elapsed { get; set; } = new TimeSpan(0);
        public static ConcurrentBag<string> Failed { get; set; } = new ConcurrentBag<string>();

        public static async Task<DSReplay> ScanRep(string file, bool GetDetails = false)
        {
            return await Task.Run(() =>
            {
                s2dec.LoadEngine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 1);
                return s2dec.DecodePython(file, false, GetDetails);
            });
        }

        public static void Doit(List<string> fileList, int cores = 2)
        {
            source = new CancellationTokenSource();
            token = source.Token;
            _empty = new ManualResetEvent(false);
            CORES = cores;
            
            Failed = new ConcurrentBag<string>();
            Console.WriteLine("Engine start.");
            s2dec.DEBUG = DSdata.Config.Debug;
            s2dec.LoadEngine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileList.Count());
            
            int total = 0;

            _jobs_decode = new BlockingCollection<string>();
            foreach (var ent in fileList)
            {
                try
                {
                    _jobs_decode.Add(ent);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                total++;
            }

            for (int i = 0; i < CORES; i++)
            {
                Thread thread = new Thread(OnHandlerStart)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }
        }

        public static void StopIt()
        {
            Console.WriteLine("Stop requested");
            try
            {
                source.Cancel();
            }
            catch { }
            finally
            {
                source.Dispose();
            }
        }

        private static void OnHandlerStart(object obj)
        {
            if (token.IsCancellationRequested == true)
                return;

            try
            {
                foreach (var job in _jobs_decode.GetConsumingEnumerable(token))
                {
                    s2dec.DecodePython(job);
                }
            }
            catch (OperationCanceledException)
            {
                try
                {
                    //s2dec.END = DateTime.UtcNow;
                }
                catch { }
            }
            _empty.Set();
        }
    }


}
