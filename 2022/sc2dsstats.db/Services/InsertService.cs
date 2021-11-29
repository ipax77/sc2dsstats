using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using sc2dsstats._2022.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace sc2dsstats.db.Services;

public interface IInsertService
{
    event EventHandler<EventArgs> ReplaysInserted;

    bool AddReplay(Dsreplay replay);
    void AddReplays(List<Dsreplay> replays);
    void Cancel();
    void Dispose();
    void Reset();
    void WriteFinished();
    void WriteStart();
}


