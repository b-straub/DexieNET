using Humanizer.Configuration;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Configuration;
using System;

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
            builder.Services.AddScoped(sp => http);

            using var importfileR = await http.GetAsync("importfile.json");
            using var importfileS = await importfileR.Content.ReadAsStreamAsync();
            builder.Configuration.AddJsonStream(importfileS);

            using var dexieCloudR = await http.GetAsync("dexie-cloud.json");
            using var dexieCloudS = await dexieCloudR.Content.ReadAsStreamAsync();
            builder.Configuration.AddJsonStream(dexieCloudS);
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
    }
}
