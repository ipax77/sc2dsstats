
using sc2dsstats._2022.Shared;

namespace sc2dsstats._2022.Server.Models;
public class HubModel
{
    public Guid Guid { get; set; }
    public int Visitors;
    public string[] Picks { get; set; } = new string[6] { null, null, null, null, null, null };

    public HubModel(Guid guid)
    {
        Guid = guid;
        Visitors = 1;
    }

    public HubModel(Guid guid, string cmdr, int pos) : this(guid)
    {
        Picks[pos] = cmdr;
    }

    public void Reset()
    {
        Visitors = 1;
        Picks = new string[6] { null, null, null, null, null, null };
    }

    public void SetCmdr(string cmdr, int pos)
    {
        Picks[pos] = cmdr;
    }

    public void AddVisitor()
    {
        Interlocked.Increment(ref Visitors);
    }

    public void RemoveVisitor()
    {
        Interlocked.Decrement(ref Visitors);
    }

    public void FillWithRandom(Random random, bool std = false)
    {
        for (int i = 0; i < Picks.Length; i++)
        {
            if (String.IsNullOrEmpty(Picks[i]))
            {
                if (std)
                {
                    // Picks[i] = DSData.stds[random.Next(DSData.stds.Length)];
                    Picks[i] = "random";
                }
                else
                {
                    Picks[i] = DSData.cmdrs[random.Next(DSData.cmdrs.Length)];
                }
            }
        }
    }

    public HubInfo ViewModel(bool withCmdrs)
    {
        int locks = Picks.Where(x => !String.IsNullOrEmpty(x)).Count();
        return new HubInfo()
        {
            Guid = Guid,
            Visitors = Visitors,
            Locked = locks,
            Commanders = withCmdrs || locks >= 2 ? Picks : new string[6] { null, null, null, null, null, null }
        };
    }
}
