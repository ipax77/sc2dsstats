using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using sc2dsstats._2022.Client.Services;
using sc2dsstats._2022.Shared;

namespace sc2dsstats._2022.Client
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);
            builder.RootComponents.Add<App>("#app");

            builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

            builder.Services.AddScoped<IDataService, DataService>();

            await builder.Build().RunAsync();
        }
    }
}
