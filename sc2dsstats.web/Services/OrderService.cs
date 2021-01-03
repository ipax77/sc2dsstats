using sc2dsstats.web.Pages;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace sc2dsstats.web.Services
{
    public class OrderService
    {
        private ConcurrentDictionary<Guid, List<string>> Orders = new ConcurrentDictionary<Guid, List<string>>();
        private ConcurrentDictionary<Guid, List<OrderPage>> OrderPages = new ConcurrentDictionary<Guid, List<OrderPage>>();

        public Guid CreateOrder()
        {
            Guid orderid = Guid.NewGuid();
            if (Orders.TryAdd(orderid, new List<string>() {
                "",
                "",
                "",
                "",
                "",
                ""
            }))
            {
                OrderPages.TryAdd(orderid, new List<OrderPage>());
                return orderid;
            }
            else
                return new Guid();
        }

        public void SetOrder(Guid orderid, int pos, string value)
        {
            Orders[orderid][pos] = value;
        }

        public List<string> GetOrders(Guid orderid)
        {
            return new List<string>(Orders[orderid]);
        }

        public int GetOrderCount(Guid orderid)
        {
            return Orders[orderid].Where(x => !String.IsNullOrEmpty(x)).Count();
        }

        public void LockOrder(Guid orderid, OrderPage page)
        {
            if (OrderPages.ContainsKey(orderid))
            {
                OrderPages[orderid].Add(page);
                if (OrderPages[orderid].Count == 2)
                {
                    foreach (var lpage in OrderPages[orderid])
                        lpage.ShowOrder();
                    OrderPages.TryRemove(orderid, out _);
                }
            }

        }

        public void Dispose(Guid orderid, OrderPage page)
        {
            if (OrderPages.ContainsKey(orderid))
                if (OrderPages[orderid].Contains(page))
                    OrderPages[orderid].Remove(page);
        }
    }
}
