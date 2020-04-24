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
using Microsoft.Extensions.Logging;
using sc2dsstats.lib.Db;

namespace sc2dsstats.decode.Service
{
    public class DecodeReplays
    {
        private BlockingCollection<string> _jobs_decode = new BlockingCollection<string>();
        private CancellationTokenSource source = new CancellationTokenSource();
        private CancellationToken token;
        private ManualResetEvent _empty = new ManualResetEvent(false);
        private int CORES = 4;
        public TimeSpan Elapsed { get; set; } = new TimeSpan(0);
        public event EventHandler ScanStateChanged;
        public ScanState arg = new ScanState();
        ILogger _logger;
        DBService _db;

        public DecodeReplays(ILogger<DecodeReplays> logger)
        {
            token = source.Token;
            _logger = logger;
        }

        protected virtual void OnScanStateChanged(ScanState e)
        {
            EventHandler handler = ScanStateChanged;
            handler?.Invoke(this, e);
        }

        public async Task<DSReplay> ScanRep(string file, bool GetDetails = false)
        {
            return await Task.Run(() =>
            {
                s2decode.LoadEngine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 1);
                return s2decode.DecodePython(file, false, GetDetails);
            });
        }

        public void Doit(List<string> fileList, DBService db, int cores = 2)
        {
            _db = db;
            arg = new ScanState();
            source = new CancellationTokenSource();
            token = source.Token;
            _empty = new ManualResetEvent(false);
            CORES = cores;

            _logger.LogInformation("Engine start.");
            arg.Start = DateTime.UtcNow;
            arg.Total = fileList.Count;
            s2decode.DEBUG = DSdata.Config.Debug;
            s2decode.LoadEngine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileList.Count());

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
            }

            for (int i = 0; i < CORES; i++)
            {
                Thread thread = new Thread(OnHandlerStart)
                { IsBackground = true };//Mark 'false' if you want to prevent program exit until jobs finish
                thread.Start();
            }

            Task tsscan = Task.Factory.StartNew(() =>
            {
                int i = 0;
                while (!_empty.WaitOne(1000))
                {
                    i++;
                    arg.Done = s2decode.DONE;
                    arg.Failed = s2decode.FAILED;
                    arg.Threads = s2decode.THREADS;
                    arg.FailedReplays.AddRange(s2decode.FailedDSReplays);

                    if (arg.Running == false && arg.DbDone >= arg.Total)
                    {
                        break;
                    } else if (arg.Threads == 0 && arg.Done >= arg.Total)
                    {
                        arg.Running = false;
                    }

                    if (arg.Done > 0)
                    {
                        double eta = (double)i / (double)arg.Done * (double)arg.Total;
                        arg.ETA = TimeSpan.FromSeconds(eta);
                    }
                    OnScanStateChanged(arg);
                }
                arg.End = DateTime.UtcNow;
                OnScanStateChanged(arg);
                _logger.LogInformation("Decoding finished");
            });

            PopulateDb();
        }

        public async Task PopulateDb()
        {
            _logger.LogInformation("Populating db");
            await Task.Run(() => {
                while (arg.DbDone < arg.Total)
                {
                    while (s2decode.DSReplays.Any())
                    {
                        DSReplay rep = null;
                        s2decode.DSReplays.TryTake(out rep);
                        if (rep != null)
                        {
                            try
                            {
                                _db.SaveReplay(rep, true);
                            }
                            catch (Exception e)
                            {
                                _logger.LogError(e.Message);
                            }
                            finally
                            {
                                Interlocked.Increment(ref arg.DbDone);
                            }
                        }
                        if (arg.DbDone % 10 == 0)
                        {
                            try
                            {
                                _db.SaveContext();
                            } catch (Exception e)
                            {
                                _logger.LogError(e.Message);
                            } finally
                            {
                                DSdata.DesktopStatus.DatabaseReplays = arg.DbDone;
                            }
                        }
                    }
                }
                try
                {
                    _db.SaveContext();
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                } finally
                {
                    DSdata.DesktopStatus.DatabaseReplays = arg.DbDone;
                }
                _logger.LogInformation("Populating db finished.");
            });
        }

        public void StopIt()
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

        private void OnHandlerStart(object obj)
        {
            if (token.IsCancellationRequested == true)
                return;

            try
            {
                foreach (var job in _jobs_decode.GetConsumingEnumerable(token))
                {
                    s2decode.DecodePython(job);
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

    public class ScanState : EventArgs
    {
        public int Threads = 0;
        public int Total = 0;
        public int Done = 0;
        public int DbDone = 0;
        public int Failed = 0;
        public List<string> FailedReplays = new List<string>();
        public bool Running = false;
        public DateTime Start = DateTime.Now;
        public DateTime End = DateTime.MinValue;
        public TimeSpan ETA = TimeSpan.Zero;
    }
}
