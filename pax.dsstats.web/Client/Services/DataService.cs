using pax.dsstats.shared;
using System.Collections.Generic;
using System.Net.Http.Json;

namespace pax.dsstats.web.Client.Services;

public class DataService : IDataService
{
    private readonly HttpClient httpClient;
    private readonly ILogger<DataService> logger;
    private readonly string statsController = "api/StatsController/";

    public DataService(HttpClient httpClient, ILogger<DataService> logger)
    {
        this.httpClient = httpClient;
        this.logger = logger;
    }

    public async Task<ReplayDto?> GetReplay(string replayHash, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetReplay", replayHash, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ReplayDto>();
            }
            else
            {
                logger.LogError($"failed getting replay: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting replay: {e.Message}");
        }
        return null;
    }

    public async Task<int> GetReplaysCount(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetReplaysCount", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<int>();
            }
            else
            {
                logger.LogError($"failed getting replay count: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting replay count: {e.Message}");
        }
        return 0;
    }

    public async Task<ICollection<ReplayListDto>> GetReplays(ReplaysRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetReplays", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ICollection<ReplayListDto>>() ?? new List<ReplayListDto>();
            }
            else
            {
                logger.LogError($"failed getting replays: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting replays: {e.Message}");
        }
        return new List<ReplayListDto>();
    }


    public async Task<StatsResponse> GetStats(StatsRequest request, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetStats", request, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<StatsResponse>() ?? new();
            }
            else
            {
                logger.LogError($"failed getting stats: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting stats: {e.Message}");
        }
        return new();
    }

    public async Task<BuildResponse> GetBuild(BuildRequest request)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetBuild", request);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<BuildResponse>() ?? new();
            }
            else
            {
                logger.LogError($"failed getting build: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting build: {e.Message}");
        }
        return new();
    }

    public async Task<int> GetRatingsCount(CancellationToken token = default)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<int>($"{statsController}GetRatingsCount");
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting ratings count: {e.Message}");
        }
        return 0;
    }

    public async Task<List<PlayerRatingDto>> GetRatings(int skip, int take, Order order, CancellationToken token = default)
    {
        try
        {
            var response = await httpClient.PostAsJsonAsync($"{statsController}GetRatings/{skip}/{take}", order, token);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<List<PlayerRatingDto>>() ?? new();
            }
            else
            {
                logger.LogError($"failed getting ratings: {response.StatusCode}");
            }
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting ratings: {e.Message}");
        }
        return new();
    }

    public async Task<string?> GetPlayerRatings(int toonId)
    {
        try
        {
            return await httpClient.GetFromJsonAsync<string?>($"{statsController}GetPlayerRatings/{toonId}");
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting player ratings: {e.Message}");
        }
        return null;
    }

    public async Task<List<MmrDevDto>> GetRatingsDeviation()
    {
        try
        {
            return await httpClient.GetFromJsonAsync<List<MmrDevDto>>("{statsController}GetRatingsDeviation") ?? new();
        }
        catch (Exception e)
        {
            logger.LogError($"failed getting rating deviation: {e.Message}");
        }
        return new();
    }

    public async Task<ICollection<string>> GetReplayPaths()
    {
        return await Task.FromResult(new List<string>());
    }

    public async Task<List<string>> GetTournaments()
    {
        return await Task.FromResult(new List<string>());
    }
}
