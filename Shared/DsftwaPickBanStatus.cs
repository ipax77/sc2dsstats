using System;
using System.Collections.Generic;
using System.Linq;

namespace sc2dsstats._2022.Shared;

public class DsftwaPickbanStatus : PickbanStatus
{
    public DsftwaPickbanStatus(Guid guid) : base(guid)
    {
    }

    public new List<string> GetOptions(byte Team)
    {
        return Enum.GetValues(typeof(DSData.Commander)).Cast<int>().Where(x => x > 3).Select(s => Enum.GetName(typeof(DSData.Commander), s)).ToList();
    }
}