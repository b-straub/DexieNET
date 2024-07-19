using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace DexieNETCloudPushServer.Services;

using Microsoft.Extensions.Configuration;

public class FilesSecretsConfigurationService(IServiceProvider serviceProvider) : ISecretsConfigurationService
{
    private readonly IConfiguration _configuration = serviceProvider.GetRequiredService<IConfiguration>();

    private record Secrets(string Key, string Value);
    public Dictionary<string, string> GetSecrets()
    {
        Dictionary<string, string> secretsDict = [];

        var databases = _configuration.GetSection("Databases").Get<DatabaseConfig[]>();
        ArgumentNullException.ThrowIfNull(databases);
        var databasesJson = JsonSerializer.Serialize(databases,
            DatabaseConfigContext.Default.DatabaseConfigArray);
        secretsDict.Add("Databases", databasesJson);

        var vapidKeys = _configuration.GetSection("VapidKeys").Get<VapidKeysConfig>();
        ArgumentNullException.ThrowIfNull(vapidKeys);
        var vapidKeysJson = JsonSerializer.Serialize(vapidKeys,
            VapidKeysConfigContext.Default.VapidKeysConfig);
        secretsDict.Add("VapidKeys", vapidKeysJson);
        
        return secretsDict;
    }
}

public static class FilesSecretsConfigurationServiceExtensions
{
    public static void AddFilesSecretsConfigurationService(this IServiceCollection services, ConfigurationManager configurationManager, string secretFileName)
    {
        configurationManager.AddJsonFile(secretFileName);
        services.AddSingleton<ISecretsConfigurationService, FilesSecretsConfigurationService>(sp => new FilesSecretsConfigurationService(sp));
    }
} 