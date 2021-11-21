using sc2dsstats._2022.Shared;
using System.Net.Http.Json;

namespace sc2dsstats._2022.Client.Services
{
    public class DataService : IDataService
    {
        private readonly HttpClient Http;
        private readonly ILogger<DataService> logger;

        public DataService(HttpClient Http, ILogger<DataService> logger)
        {
            this.Http = Http;
            this.logger = logger;
        }

        public async Task<DsResponse> LoadData(DsRequest request)
        {
            DsResponse response = null;
            try
            {
                var httpResponse = request.Mode switch
                {
                    "Winrate" => await Http.PostAsJsonAsync("api/stats/winrate", request),
                    "Timeline" => await Http.PostAsJsonAsync("api/stats/timeline", request),
                    "MVP" => await Http.PostAsJsonAsync("api/stats/mvp", request),
                    "DPS" => await Http.PostAsJsonAsync("api/stats/dps", request),
                    "Synergy" => await Http.PostAsJsonAsync("api/stats/synergy", request),
                    "AntiSynergy" => await Http.PostAsJsonAsync("api/stats/antisynergy", request),
                    "Duration" => await Http.PostAsJsonAsync("api/stats/duration", request),
                    "Standard" => await Http.PostAsJsonAsync("api/stats/teamstandard", request),
                    _ => await Http.PostAsJsonAsync("api/stats/winrate", request)
                };
                if (httpResponse.IsSuccessStatusCode)
                {
                    return request.Mode switch
                    {
                        "Winrate" => await httpResponse.Content.ReadFromJsonAsync<DsResponse>(),
                        "Timeline" => await httpResponse.Content.ReadFromJsonAsync<TimelineResponse>(),
                        "MVP" => await httpResponse.Content.ReadFromJsonAsync<DsResponse>(),
                        "Duration" => await httpResponse.Content.ReadFromJsonAsync<TimelineResponse>(),
                        "Standard" => await httpResponse.Content.ReadFromJsonAsync<DsResponse>(),
                        _ => await httpResponse.Content.ReadFromJsonAsync<DsResponse>()
                    };
                }
                else
                {
                    logger.LogError($"Failed loading data: {httpResponse.StatusCode}");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Failed loading data: {e.Message}");
            }
            return response;
        }

        public async Task<bool> UploadData()
        {
            return false;
        }

        public async Task<List<DsReplayResponse>> GetReplays(DsReplayRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    var response = await Http.PostAsJsonAsync<DsReplayRequest>("api/replays", request, cancellationToken);
                    if (response.IsSuccessStatusCode)
                    {
                        return await response.Content.ReadFromJsonAsync<List<DsReplayResponse>>();
                    }
                    else
                        return new List<DsReplayResponse>();
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                logger.LogError($"Failed getting replays: {e.Message}");
            }
            return new List<DsReplayResponse>();
        }

        public async Task<int> GetReplaysCount(DsReplayRequest request)
        {
            try
            {
                var response = await Http.PostAsJsonAsync<DsReplayRequest>("api/replays/count", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<int>();
                }
                else
                    return 0;
            }
            catch (Exception e)
            {
                logger.LogError($"Failed getting replays count: {e.Message}");
            }
            return 0;
        }

        public async Task<DsGameResponse> GetReplay(string hash)
        {
            try
            {
                return await Http.GetFromJsonAsync<DsGameResponse>($"api/replays/{hash}");
            }
            catch (Exception e)
            {
                logger.LogError($"Failed getting replay {hash}: {e.Message}");
            }
            return null;
        }

        public async Task<DsBuildResponse> GetBuild(DsBuildRequest request)
        {
            try
            {
                var response = await Http.PostAsJsonAsync<DsBuildRequest>("api/stats/build", request);
                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadFromJsonAsync<DsBuildResponse>();
                }
                else
                {
                    logger.LogError($"Failed getting build: {response.StatusCode}");
                }
            }
            catch (Exception e)
            {
                logger.LogError($"Failed getting build: {e.Message}");
            }
            return null;
        }

        public async Task<List<DsRankingResponse>> GetRankings()
        {
            try
            {
                return await Http.GetFromJsonAsync<List<DsRankingResponse>>("api/stats/ranking");
            }
            catch (Exception e)
            {
                logger.LogError($"Failed getting ranking: {e.Message}");
            }
            return new List<DsRankingResponse>();
        }

        public async Task<List<string>> GetPlayernames()
        {
            try
            {
                return await Http.GetFromJsonAsync<List<string>>("api/stats/names");
            }
            catch (Exception e)
            {
                logger.LogError($"Failed getting names: {e.Message}");
            }
            return new List<string>();
        }

        public async Task<DsPlayerStats> GetPlayerStats(List<string> playerNames)
        {
            try
            {
                var playerstats = await Http.GetFromJsonAsync<DsPlayerStats>($"api/stats/playerstats/{playerNames.First()}");
                return playerstats;
            }
            catch (Exception e)
            {
                logger.LogError($"Failed getting player stats: {e.Message}");
                return null;
            }
        }

        public async Task<List<PlayerNameResponse>> GetPlayerNameStats()
        {
            // app only
            throw new NotImplementedException();
        }

        public async Task<PlayerNameStatsResponse?> GetPlayerNameStatsResponse(string name, CancellationToken token)
        {
            // app only
            throw new NotImplementedException();
        }

        public void ClearPlayerStats()
        {
            // app only
            throw new NotImplementedException();
        }
    }
}
