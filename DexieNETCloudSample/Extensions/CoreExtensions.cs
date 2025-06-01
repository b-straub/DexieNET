using DexieCloudNET;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace DexieNETCloudSample.Extensions
{
    internal static class CoreExtensions
    {
        public static bool True(this bool? value)
        {
            return value.GetValueOrDefault(false);
        }

        public static async Task LoadConfigurationAsync(this WebAssemblyHostBuilder builder)
        {
            var http = new HttpClient()
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress),
            };

            /*http.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue
            {
                NoCache = true
            };*/

            builder.Services.AddSingleton(sp => http);

            try
            {
                using var importfileR = await http.GetAsync("importfile.json");
                await using var importfileS = await importfileR.Content.ReadAsStreamAsync();
                builder.Configuration.AddJsonStream(importfileS);

                using var dexieCloudR = await http.GetAsync("dexie-cloud.json");
                await using var dexieCloudS = await dexieCloudR.Content.ReadAsStreamAsync();
                builder.Configuration.AddJsonStream(dexieCloudS);
            }
            catch
            {
                throw new InvalidOperationException("Can not load cloud configuration. Run 'configure-app.ps1' first!");
            }
        }

        public static string[] GetUsers(this IConfiguration? configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            return configuration.GetSection("demoUsers").GetChildren().Select(v => v.Key).ToArray();
        }

        public static string GetDBUrl(this IConfiguration? configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var dbUrl = configuration.GetSection("dbUrl").Value;
            ArgumentNullException.ThrowIfNull(dbUrl);
            return dbUrl;
        }
        
        public static string? GetApplicationServerKey(this IConfiguration? configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var applicationServerKey = configuration.GetSection("applicationServerKey").Value;
            return applicationServerKey;
        }
        
        public static string GetRouteFolder(this IConfiguration? configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);
            var rootFolder = configuration.GetSection("rootFolder").Value;
            ArgumentNullException.ThrowIfNull(rootFolder);
            return rootFolder;
        }
        
        public static bool ValidPhase(this SyncState? syncState)
        {
            return (syncState?.Phase is SyncState.SyncStatePhase.IN_SYNC or SyncState.SyncStatePhase.PULLING
                or SyncState.SyncStatePhase.PUSHING);
        }
    }
}
