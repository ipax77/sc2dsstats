using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using sc2dsstats._2022.Client.Shared;
using sc2dsstats._2022.Server.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace sc2dsstats._2022.Server.Hubs
{
    public class GdslHub : Hub
    {
        public static ConcurrentDictionary<Guid, HubModel> Models = new ConcurrentDictionary<Guid, HubModel>();
        public static Random random = new Random();

        public async Task CreateNewPage(Guid guid)
        {
            Context.Items.Clear();
            Context.Items.Add("guid", guid);

            HubModel model = new HubModel(guid);
            if (!Models.TryAdd(guid, model))
            {
                Models[guid].Reset();
            }
            await Groups.AddToGroupAsync(Context.ConnectionId, guid.ToString());
            await Clients.Group(guid.ToString()).SendAsync("VisitorJoined", 1);
        }

        public async Task VisitPage(Guid guid)
        {
            Context.Items.Clear();
            Context.Items.Add("guid", guid);
            Models.AddOrUpdate(guid, new HubModel(guid), (key, oldvalue) => { oldvalue.AddVisitor(); return oldvalue; });
            await Groups.AddToGroupAsync(Context.ConnectionId, guid.ToString());
            await Clients.Group(guid.ToString()).SendAsync("VisitorJoined", Models[guid].Visitors);
            await Clients.Client(Context.ConnectionId).SendAsync("PickState", Models[guid].ViewModel(false));
        }

        public async Task LeavePage(Guid guid)
        {
            Models.AddOrUpdate(guid, new HubModel(guid), (key, oldvalue) => { oldvalue.RemoveVisitor(); return oldvalue; });
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, guid.ToString());
            await Clients.Group(guid.ToString()).SendAsync("VisitorLeft", Models[guid].Visitors);
        }

        public async Task CmdrPicked(PickBanLockModel cmdrLock)
        {
            Models.AddOrUpdate(
                cmdrLock.Guid,
                new HubModel(cmdrLock.Guid, cmdrLock.cmdr, cmdrLock.i),
                (key, oldvalue) => { oldvalue.SetCmdr(cmdrLock.cmdr, cmdrLock.i); return oldvalue; }
            );
            await Clients.OthersInGroup(cmdrLock.Guid.ToString()).SendAsync("CmdrLocked", Models[cmdrLock.Guid].ViewModel(false));
            if (Models[cmdrLock.Guid].Picks.Where(x => !String.IsNullOrEmpty(x)).Count() >= 2)
            {
                Models[cmdrLock.Guid].FillWithRandom(random, true);
                await Clients.Group(cmdrLock.Guid.ToString()).SendAsync("CmdrLocked", Models[cmdrLock.Guid].ViewModel(true));
            }
        }

        public override async Task OnDisconnectedAsync(Exception e)
        {
            if (Context.Items.ContainsKey("guid"))
            {
                Guid guid = (Guid)Context.Items["guid"];
                Models.AddOrUpdate(guid, new HubModel(guid), (key, oldvalue) => { oldvalue.RemoveVisitor(); return oldvalue; });
                await Clients.OthersInGroup(guid.ToString()).SendAsync("VisitorLeft", Models[guid].Visitors);
            }
            await base.OnDisconnectedAsync(e);
        }

        public override async Task OnConnectedAsync()
        {
            if (Context.Items.ContainsKey("guid"))
            {
                Guid guid = (Guid)Context.Items["guid"];
                Models.AddOrUpdate(guid, new HubModel(guid), (key, oldvalue) => { oldvalue.AddVisitor(); return oldvalue; });
                await Clients.Group(guid.ToString()).SendAsync("VisitorJoined", Models[guid].Visitors);
            }
            await base.OnConnectedAsync();
        }
    }
}
