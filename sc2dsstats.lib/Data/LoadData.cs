using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using sc2dsstats.lib.Db;
using sc2dsstats.lib.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats.lib.Data
{

    public class LoadData
    {
        public event EventHandler DataLoaded;
        private ReplaysLoadedEventArgs args = new ReplaysLoadedEventArgs();
        public UserConfig myConfig = new UserConfig();
        private readonly IServiceScopeFactory scopeFactory;


        public LoadData(IServiceScopeFactory scopeFactory)
        {
            this.scopeFactory = scopeFactory;
        }

        protected virtual void OnDataLoaded(ReplaysLoadedEventArgs e)
        {
            EventHandler handler = DataLoaded;
            handler?.Invoke(this, e);
        }

        public async Task Init()
        {
            lock (args)
                args.isBuildLoaded = false;

            await Task.Delay(6000);

            int i = 1;

            
            using (var scope = scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<DSReplayContext>();
                i = db.DSReplays.Count();

                if (db.Database.IsMySql())
                    DSdata.IsMySQL = true;
            }
            

            DSdata.Status.Count = i;
            lock (args)
            {
                args.Count = i;
                args.isBuildLoaded = true;
                args.isReplaysLoaded = true;
                args.isDuplicatesLoaded = true;
            }
            DSdata.Status = args;
            OnDataLoaded(args);

            Task.Factory.StartNew(() => { GetDatasetInfo(); }, new CancellationToken(), TaskCreationOptions.None, PriorityScheduler.Lowest);

        }

        public void Update()
        {
            OnDataLoaded(args);
        }

        async Task GetDatasetInfo()
        {
            DateTime t = DateTime.UtcNow;
            using (var scope = scopeFactory.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<DSReplayContext>();
                DSdata.Datasets = new List<DatasetInfo>();
                var names = context.DSPlayers.Select(s => s.NAME).Distinct().ToArray();

                foreach (var hash in names.Where(x => x.Length == 64))
                {
                    DatasetInfo setinfo = new DatasetInfo();
                    setinfo.Dataset = hash;

                    var reps = from r in context.DSReplays
                               from p in r.DSPlayer
                               where p.NAME == hash
                               select new
                               {
                                   r.ID,
                                   r.DSPlayer,
                                   p.TEAM,
                                   p.WIN
                               };


                    setinfo.Count = reps.Count();

                    var teams = reps.Where(x => x.DSPlayer.Where(y => y.NAME.Length == 64 && y.TEAM == x.TEAM).Count() > 1);
                    int teamgames = teams.Count();
                    setinfo.Teamgames = MathF.Round((float)teamgames * 100 / (float)setinfo.Count, 2);


                    int wins = reps.Where(x => x.WIN == true).Count();
                    setinfo.Winrate = MathF.Round((float)wins * 100 / (float)setinfo.Count, 2);

                    DSdata.Datasets.Add(setinfo);
                }
            }
            Console.WriteLine($"Dataset info created in {(DateTime.UtcNow - t).TotalSeconds}");
        }
    }

    public class ReplaysLoadedEventArgs : EventArgs
    {
        public int Count { get; set; } = 0;
        public bool isReplaysLoaded { get; set; } = false;
        public bool isDuplicatesLoaded { get; set; } = false;
        public bool isBuildLoaded { get; set; } = false;
    }

    public class PriorityScheduler : TaskScheduler
    {
        public static PriorityScheduler AboveNormal = new PriorityScheduler(ThreadPriority.AboveNormal);
        public static PriorityScheduler BelowNormal = new PriorityScheduler(ThreadPriority.BelowNormal);
        public static PriorityScheduler Lowest = new PriorityScheduler(ThreadPriority.Lowest);

        private BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
        private Thread[] _threads;
        private ThreadPriority _priority;
        private readonly int _maximumConcurrencyLevel = Math.Max(1, Environment.ProcessorCount);

        public PriorityScheduler(ThreadPriority priority)
        {
            _priority = priority;
        }

        public override int MaximumConcurrencyLevel
        {
            get { return _maximumConcurrencyLevel; }
        }

        protected override IEnumerable<Task> GetScheduledTasks()
        {
            return _tasks;
        }

        protected override void QueueTask(Task task)
        {
            _tasks.Add(task);

            if (_threads == null)
            {
                _threads = new Thread[_maximumConcurrencyLevel];
                for (int i = 0; i < _threads.Length; i++)
                {
                    int local = i;
                    _threads[i] = new Thread(() =>
                    {
                        foreach (Task t in _tasks.GetConsumingEnumerable())
                            base.TryExecuteTask(t);
                    });
                    _threads[i].Name = string.Format("PriorityScheduler: ", i);
                    _threads[i].Priority = _priority;
                    _threads[i].IsBackground = true;
                    _threads[i].Start();
                }
            }
        }

        protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
        {
            return false; // we might not want to execute task that should schedule as high or low priority inline
        }
    }
}
