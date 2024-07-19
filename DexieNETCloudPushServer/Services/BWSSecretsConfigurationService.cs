#if DEBUG
using Microsoft.Extensions.DependencyInjection;

namespace DexieNETCloudPushServer.Services;

using Bitwarden.Sdk;
using Microsoft.Extensions.Configuration;

public class BWSSecretsConfigurationService(IServiceProvider serviceProvider, string projectName) : ISecretsConfigurationService
{
    private readonly IConfiguration _configuration = serviceProvider.GetRequiredService<IConfiguration>();

    public Dictionary<string, string> GetSecrets()
    {
        var accessToken = _configuration.GetSection("BWS_ACCESS_TOKEN").Value;
        ArgumentNullException.ThrowIfNull(accessToken);
        var organizationId = _configuration.GetSection("BWS_ORGANIZATION_ID").Value;
        ArgumentNullException.ThrowIfNull(organizationId);
        
        using var client = new BitwardenClient();
        client.AccessTokenLogin(accessToken);

        var organizationGuid = Guid.Parse(organizationId);
        
        var projects = client.Projects.List(organizationGuid);
        var projectId = projects.Data.Where(p => p.Name == projectName).Select(p => p.Id).First();
        
        var secrets = client.Secrets.List(organizationGuid);
        var secretValues = secrets.Data.Select(x => client.Secrets.Get(x.Id)).Where(x => x.ProjectId == projectId);

        Dictionary<string, string> secretsDict = [];
        
        foreach (var secretValue in secretValues)
        {
            secretsDict.Add(secretValue.Key, secretValue.Value);
        }

        return secretsDict;
    }
}

public static class BWSSecretsConfigurationServiceExtensions
{
    public static void AddBWSSecretsConfigurationService(this IServiceCollection services, string projectName)
    {
        services.AddSingleton<ISecretsConfigurationService, BWSSecretsConfigurationService>(sp => new BWSSecretsConfigurationService(sp, projectName));
    }
}
#endif