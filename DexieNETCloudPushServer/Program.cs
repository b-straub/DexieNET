// See https://aka.ms/new-console-template for more information

using DexieNETCloudPushServer.Quartz;
using DexieNETCloudPushServer.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.AddSimpleConsole(o => o.TimestampFormat = "G");
#if !DEBUG
builder.Logging.AddFilter("DexieNETCloudPushServer", LogLevel.Information);
builder.Logging.AddFilter("Microsoft", LogLevel.Warning);
builder.Logging.AddFilter("System", LogLevel.Warning);
builder.Logging.AddFilter("Polly", LogLevel.Warning);
builder.Logging.AddFilter("Quartz", LogLevel.Warning);
#else
builder.Logging.AddFilter("DexieNETCloudPushServer", LogLevel.Debug);
builder.Logging.AddFilter("Microsoft", LogLevel.Information);
builder.Logging.AddFilter("System", LogLevel.Information);
builder.Logging.AddFilter("Polly", LogLevel.Information);
builder.Logging.AddFilter("Quartz", LogLevel.Information);
#endif

var dbFullName = Path.Combine(QuartzDBContext.DbPath, QuartzDBContext.DbName);
var sqlite = $"Data Source={dbFullName};";
builder.Services.AddDbContext<QuartzDBContext>(ServiceLifetime.Singleton, ServiceLifetime.Singleton);

#if DEBUG
builder.Services.AddBWSSecretsConfigurationService("DexieNETCloud");
#else
var secretsFullName = Path.Combine(QuartzDBContext.DbPath, "secrets.json");
builder.Services.AddFilesSecretsConfigurationService(builder.Configuration, secretsFullName);
#endif
builder.Services.AddHttpClient("PushClient");
    //.AddStandardResilienceHandler();

builder.Services.AddSingleton<PushService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<PushService>());
builder.Services.AddQuartz(q =>
{
    q.UsePersistentStore(o =>
    {
        o.PerformSchemaValidation = true;
        o.UseMicrosoftSQLite(sqlite);
        o.UseNewtonsoftJsonSerializer();
    });
});

builder.Services.AddQuartzHostedService(opt => { opt.WaitForJobsToComplete = true; });

using var host = builder.Build();
await host.RunAsync();