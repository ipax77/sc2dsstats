using Microsoft.AspNetCore.SignalR;
using sc2dsstats._2022.Shared;
using System.Collections.Concurrent;

namespace sc2dsstats._2022.Server.Hubs
{
    public class PbHub : Hub
    {
        public static ConcurrentDictionary<Guid, PickbanStatus> Pbs = new ConcurrentDictionary<Guid, PickbanStatus>();
        private readonly ILogger<PbHub> logger;

        public PbHub(ILogger<PbHub> logger)
        {
            this.logger = logger;
        }


        public async Task CreateNewPage(Guid guid)
        {
            logger.LogInformation($"create new page {guid}");
            Context.Items.Clear();
            Context.Items.Add("guid", guid);

            PickbanStatus model = new PickbanStatus(guid);
            if (!Pbs.TryAdd(guid, model))
            {
                Pbs[guid].Reset();
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, guid.ToString());
            await Clients.Group(guid.ToString()).SendAsync("VisitorJoined", 1);
        }

        public async Task VisitPage(Guid guid)
        {
            logger.LogInformation($"visit page {guid}");
            Context.Items.Clear();
            Context.Items.Add("guid", guid);
            var status = Pbs.AddOrUpdate(guid, new PickbanStatus(guid), (key, oldvalue) => { oldvalue.AddVisitor(); return oldvalue; });
            await Groups.AddToGroupAsync(Context.ConnectionId, guid.ToString());
            await Clients.Group(guid.ToString()).SendAsync("VisitorJoined", status.Visitors);
            await Clients.Client(Context.ConnectionId).SendAsync("ConnectInfo", status.GetConnectInfo());
        }

        public async Task LeavePage(Guid guid)
        {
            logger.LogInformation($"leave page {guid}");
            var status = Pbs.AddOrUpdate(guid, new PickbanStatus(guid), (key, oldvalue) => { oldvalue.RemoveVisitor(); return oldvalue; });
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, guid.ToString());
            await Clients.Group(guid.ToString()).SendAsync("VisitorLeft", status.Visitors);
        }

        public virtual async Task Lock(PickbanSelectinfo info)
        {
            logger.LogInformation($"lock {info.Guid}");
            PickbanLockinfo lockInfo = null;
            var status = Pbs.AddOrUpdate(info.Guid, new PickbanStatus(info.Guid), (key, oldvalue) => { lockInfo = oldvalue.Lock(info); return oldvalue; });
            // await Clients.OthersInGroup(info.Guid.ToString()).SendAsync("CmdrLocked", lockInfo);
            await Clients.Group(info.Guid.ToString()).SendAsync("CmdrLocked", lockInfo);
            if (status.Turn >= 6)
            {
                status.Picks.ForEach(f => f.Public = true);
                await Clients.Group(info.Guid.ToString()).SendAsync("ConnectInfo", status.GetConnectInfo());
            }
        }

        public virtual async Task LockBan(PickbanSelectinfo info)
        {
            logger.LogInformation($"lock {info.Guid}");
            PickbanLockinfo lockInfo = null;
            var status = Pbs.AddOrUpdate(info.Guid, new PickbanStatus(info.Guid), (key, oldvalue) => { lockInfo = oldvalue.Lock(info); return oldvalue; });
            // await Clients.OthersInGroup(info.Guid.ToString()).SendAsync("CmdrLocked", lockInfo);
            await Clients.Group(info.Guid.ToString()).SendAsync("CmdrLocked", lockInfo);
            if (status.Turn >= 2)
            {
                status.Picks.ForEach(f => f.Public = true);
                await Clients.Group(info.Guid.ToString()).SendAsync("ConnectInfo", status.GetConnectInfo());
            }
        }

        public override async Task OnDisconnectedAsync(Exception e)
        {
            logger.LogInformation($"disconnect");
            if (Context.Items.ContainsKey("guid"))
            {
                Guid guid = (Guid)Context.Items["guid"];
                var status = Pbs.AddOrUpdate(guid, new PickbanStatus(guid), (key, oldvalue) => { oldvalue.RemoveVisitor(); return oldvalue; });
                await Clients.OthersInGroup(guid.ToString()).SendAsync("VisitorLeft", status.Visitors);
            }
            await base.OnDisconnectedAsync(e);
        }

        public override async Task OnConnectedAsync()
        {
            logger.LogInformation($"connect");
            //if (Context.Items.ContainsKey("guid"))
            //{
            //    Guid guid = (Guid)Context.Items["guid"];
            //    var status = Pbs.AddOrUpdate(guid, new PickbanStatus(guid), (key, oldvalue) => { oldvalue.AddVisitor(); return oldvalue; });
            //    await Clients.Group(guid.ToString()).SendAsync("VisitorJoined", status.Visitors);
            //}
            //await base.OnConnectedAsync();
        }
    }
}
