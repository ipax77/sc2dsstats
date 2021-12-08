using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace sc2dsstats._2022.Shared
{
    public class PickbanStatus
    {
        public Guid Guid { get; init; }
        public List<PickbanSelect> Picks { get; set; }
        public int Turn { get; set; }
        public int Visitors = 1;
        public int CurrentSelect => Turn switch
        {
            0 => 0,
            1 => 3,
            2 => 1,
            3 => 4,
            4 => 2,
            5 => 5,
            _ => 99
        };
        public byte CurrentTeam => Turn switch
        {
            0 => 1,
            1 => 2,
            2 => 1,
            3 => 2,
            4 => 1,
            5 => 2,
            _ => 1
        };
        public PickbanSelect PreviouseSelect()
        {
            return Turn switch
            {
                1 => Picks.First(f => f.Pos == 0),
                2 => Picks.First(f => f.Pos == 3),
                3 => Picks.First(f => f.Pos == 1),
                4 => Picks.First(f => f.Pos == 4),
                5 => Picks.First(f => f.Pos == 2),
                6 => Picks.First(f => f.Pos == 5),
                _ => null,
            };
        }

        public PickbanStatus(Guid guid)
        {
            Guid = guid;
            Reset();
        }

        public List<string> GetOptions(byte Team)
        {
            return Picks.Where(x => x.Team == Team && x.Locked && x.Selected == "Protoss").Count() switch
            {
                1 => new List<string>() { "Terran", "Zerg" },
                2 => new List<string>() { "Terran", "Zerg" },
                _ => new List<string>() { "Protoss", "Terran", "Zerg" }
            };
        }

        // lock by clicking
        public PickbanSelectinfo Lock()
        {
            var pick = Picks.First(f => f.Pos == CurrentSelect);
            return new PickbanSelectinfo()
            {
                Guid = Guid,
                Pos = pick.Pos,
                Turn = Turn + 1,
                Selection = pick.Selected
            };
        }

        // server lock
        public PickbanLockinfo Lock(PickbanSelectinfo info)
        {
            var pick = Picks.First(f => f.Pos == info.Pos);
            var ppick = PreviouseSelect();

            pick.Selected = info.Selection;
            pick.Locked = true;
            Turn++;
            // pick.Public = Turn % 2 == 0;
            if (ppick != null && pick.Public)
            {
                ppick.Public = true;
            }
            return new PickbanLockinfo()
            {
                Pos = info.Pos,
                Selection = pick.Public ? pick.Selected : "",
                PreviouseSelection = ppick != null && ppick.Public ? ppick.Selected : ""
            };
        }

        // client lock
        public void Lock(PickbanLockinfo info)
        {
            var pick = Picks.First(f => f.Pos == info.Pos);
            if (String.IsNullOrEmpty(pick.Selected))
            {
                pick.Selected = info.Selection;
            }
            pick.Locked = true;
            if (!String.IsNullOrEmpty(info.PreviouseSelection))
            {
                PreviouseSelect().Selected = info.PreviouseSelection;
            }
            Turn++;
        }

        public void Reset()
        {
            Picks = new List<PickbanSelect>()
            {
                new PickbanSelect() { Team = 1, Pos = 0 },
                new PickbanSelect() { Team = 1, Pos = 1 },
                new PickbanSelect() { Team = 1, Pos = 2 },
                new PickbanSelect() { Team = 2, Pos = 3 },
                new PickbanSelect() { Team = 2, Pos = 4 },
                new PickbanSelect() { Team = 2, Pos = 5 }
            };
        }

        public List<PickbanLockinfo> GetConnectInfo()
        {
            return Picks.Where(x => x.Locked).Select(s => new PickbanLockinfo()
            {
                Pos = s.Pos,
                Selection = s.Public ? s.Selected : null
            }).ToList();
        }

        public void AddVisitor()
        {
            Interlocked.Increment(ref Visitors);
        }

        public void RemoveVisitor()
        {
            Interlocked.Decrement(ref Visitors);
        }
    }

    public class PickbanSelect
    {
        public byte Team { get; set; }
        public byte Pos { get; set; }
        public string Selected { get; set; }
        public bool Locked { get; set; } = false;
        public bool Public { get; set; } = false;
    }

    public class PickbanSelectinfo
    {
        public Guid Guid { get; init; }
        public int Pos { get; init; }
        public int Turn { get; init; }
        public string Selection { get; init; }
    }

    public class PickbanLockinfo
    {
        public int Pos { get; init; }
        public string PreviouseSelection { get; init; }
        public string Selection { get; init; }
    }
}
