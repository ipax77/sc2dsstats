using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace sc2dsstats._2022.Shared
{
    public interface IDataService
    {
        Task<DsResponse> LoadData(DsRequest request);
        Task<bool> UploadData();
        Task<List<DsReplayResponse>> GetReplays(DsReplayRequest request, CancellationToken cancellationToken);
        Task<int> GetReplaysCount(DsReplayRequest request);
        Task<DsGameResponse> GetReplay(string hash);
        Task<DsBuildResponse> GetBuild(DsBuildRequest request);
        Task<List<DsRankingResponse>> GetRankings();
        Task<List<string>> GetPlayernames();
        Task<DsPlayerStats> GetPlayerStats(List<string> playerNames);
    }
}
