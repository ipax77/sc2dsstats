namespace sc2dsstats._2022.Server.Models;
using sc2dsstats._2022.Shared;

public class HubBanModel
{
    public Guid Guid { get; set; }
    public int Visitors;
    public string[] Bans { get; set; }

    public HubBanModel(Guid guid, int bans)
    {
        Guid = guid;
        Visitors = 1;
        Bans = new string[bans];
    }

    public HubBanModel(Guid guid, int bans, string cmdr, int pos) : this(guid, bans)
    {
        Bans[pos] = cmdr;
    }

    public void Reset()
    {
        Visitors = 1;
        Bans = new string[Bans.Length];
    }

    public void BanCmdr(string cmdr, int pos)
    {
        Bans[pos] = cmdr;
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
        for (int i = 0; i < Bans.Length; i++)
        {
            if (String.IsNullOrEmpty(Bans[i]))
            {
                if (std)
                {
                    // Picks[i] = DSData.stds[random.Next(DSData.stds.Length)];
                    Bans[i] = "random";
                }
                else
                {
                    Bans[i] = DSData.cmdrs[random.Next(DSData.cmdrs.Length)];
                }
            }
        }
    }

    public HubInfo ViewModel(bool withCmdrs)
    {
        int locks = Bans.Where(x => !String.IsNullOrEmpty(x)).Count();
        return new HubInfo()
        {
            Guid = Guid,
            Visitors = Visitors,
            Locked = locks,
            Commanders = withCmdrs || locks >= 2 ? Bans : new string[Bans.Length]
        };
    }
}
