using Microsoft.AspNetCore.SignalR;
using sc2dsstats._2022.Client.Shared;
using System.Collections.Concurrent;

namespace sc2dsstats._2022.Server.Hubs
{
    public class PickBanHub : Hub
    {
        private static ConcurrentDictionary<Guid, int> GroupCounts = new ConcurrentDictionary<Guid, int>();
        private static ConcurrentDictionary<Guid, PickBanModel> GroupStates = new ConcurrentDictionary<Guid, PickBanModel>();

        public async Task CreateNewPage(Guid guid)
        {
            Context.Items.Clear();
            Context.Items.Add("guid", guid);
            await Groups.AddToGroupAsync(Context.ConnectionId, guid.ToString());
            GroupCounts.AddOrUpdate(guid, 1, (key, value) => value = 1);
            GroupStates.AddOrUpdate(guid, new PickBanModel() { Guid = guid, Pos = -1 }, (key, value) => { value = new PickBanModel() { Guid = guid }; return value; });
            await Clients.Group(guid.ToString()).SendAsync("VisitorJoined", GroupCounts[guid]);
        }

        public async Task VisitPage(Guid guid)
        {
            Context.Items.Clear();
            Context.Items.Add("guid", guid);
            GroupCounts.AddOrUpdate(guid, 2, (key, value) => value = value + 1);
            await Groups.AddToGroupAsync(Context.ConnectionId, guid.ToString());
            await Clients.Group(guid.ToString()).SendAsync("VisitorJoined", GroupCounts[guid]);
            if (GroupStates.ContainsKey(guid))
            {
                await Clients.Client(Context.ConnectionId).SendAsync("PickState", GroupStates[guid]);
            }
        }

        public async Task LeavePage(Guid guid)
        {
            GroupCounts.AddOrUpdate(guid, 1, (key, value) => value = value - 1);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, guid.ToString());
            await Clients.Group(guid.ToString()).SendAsync("VisitorLeft", GroupCounts[guid]);
        }

        public async Task CmdrPicked(PickBanLockModel cmdrLock)
        {

            await Clients.OthersInGroup(cmdrLock.Guid.ToString()).SendAsync("CmdrPicked", cmdrLock);
            GroupStates.AddOrUpdate(cmdrLock.Guid, new PickBanModel() { Guid = cmdrLock.Guid }, (key, oldvalue) => { oldvalue.Picks[cmdrLock.i] = cmdrLock.cmdr; oldvalue.Pos = cmdrLock.i; return oldvalue; });
        }

        public override async Task OnDisconnectedAsync(Exception e)
        {
            if (Context.Items.ContainsKey("guid"))
            {
                Guid guid = (Guid)Context.Items["guid"];
                GroupCounts.AddOrUpdate(guid, 1, (key, value) => value = value - 1);
                await Clients.OthersInGroup(guid.ToString()).SendAsync("VisitorLeft", GroupCounts[guid]);
            }
            await base.OnDisconnectedAsync(e);
        }

        public override async Task OnConnectedAsync()
        {
            if (Context.Items.ContainsKey("guid"))
            {
                Guid guid = (Guid)Context.Items["guid"];
                GroupCounts.AddOrUpdate(guid, 1, (key, value) => value = value + 1);
                await Clients.Group(guid.ToString()).SendAsync("VisitorJoined", GroupCounts[guid]);
            }
            await base.OnConnectedAsync();
        }
    }
}
