using Microsoft.EntityFrameworkCore;
using sc2dsstats._2022.Shared;
using sc2dsstats.db;
using System.Security.Cryptography;
using System.Text;

namespace sc2dsstats.app.Services
{
    public static class UploadService
    {
        public static async Task<bool> Upload(HttpClient Http, sc2dsstatsContext context, UserConfig config, ILogger logger)
        {
            logger.LogInformation($"Uplading replay(s) {DateTime.UtcNow}");
            if (!context.Dsreplays.Any())
                return true;

            string id;
            using (SHA256 sha256Hash = SHA256.Create())
            {
                string names = String.Join(";", config.PlayersNames);
                id = GetHash(sha256Hash, names);
            }

            DsUploadRequest uploadRequest = new DsUploadRequest()
            {
                RealName = !String.IsNullOrEmpty(config.DisplayName) ? config.DisplayName : String.Empty,
                AppId = config.AppId,
                Hash = id,
                LastRep = (await context.Dsreplays.OrderByDescending(o => o.Gametime).FirstAsync()).Gametime.ToString("yyyyMMddHHmmss"),
                Total = await context.Dsreplays.CountAsync(),
                Version = Program.Version.ToString()
            };

            DateTime lastUploadReplayGametime = DateTime.MinValue;
            try
            {
                var iresponse = await Http.PostAsJsonAsync("secure/data/uploadrequest", uploadRequest);
                if (iresponse.IsSuccessStatusCode)
                {
                    lastUploadReplayGametime = await iresponse.Content.ReadFromJsonAsync<DateTime>();
                }
                else
                    return false;
            }
            catch (Exception e)
            {
                logger.LogError($"failed getting upload request: {e.Message}");
                return false;
            }

            var replays = await context.Dsreplays
                .Include(i => i.Middles)
                .Include(i => i.Dsplayers)
                    .ThenInclude(j => j.Breakpoints)
                .AsNoTracking()
                .AsSplitQuery()
                .Where(x => x.Gametime > lastUploadReplayGametime)
                .ToListAsync();

            if (!replays.Any())
                return true;
            replays.SelectMany(s => s.Dsplayers).ToList().ForEach(f => f.Name = config.PlayersNames.Contains(f.Name) ? "player" : $"player{f.Realpos}");

            replays.ForEach(f => f.Replaypath = String.Empty);

            var json = System.Text.Json.JsonSerializer.Serialize(replays.Select(s => s.GetDto()).ToList());
            json = DSData.Zip(json);


            var response = await Http.PostAsJsonAsync($"secure/data/replayupload/{config.AppId}", json);
            if (response.IsSuccessStatusCode)
            {
                var uploadResponse = await response.Content.ReadFromJsonAsync<DsUploadResponse>();
                config.DbId = uploadResponse.DbId;
                return true;
            }
            else
            {
                return false;
            }
        }

        private static string GetHash(HashAlgorithm hashAlgorithm, string input)
        {

            // Convert the input string to a byte array and compute the hash.
            byte[] data = hashAlgorithm.ComputeHash(Encoding.UTF8.GetBytes(input));

            // Create a new Stringbuilder to collect the bytes
            // and create a string.
            var sBuilder = new StringBuilder();

            // Loop through each byte of the hashed data 
            // and format each one as a hexadecimal string.
            for (int i = 0; i < data.Length; i++)
            {
                sBuilder.Append(data[i].ToString("x2"));
            }

            // Return the hexadecimal string.
            return sBuilder.ToString();
        }
    }
}
