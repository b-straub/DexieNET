namespace DexieNETCloudPushServer.Services;

public interface ISecretsConfigurationService
{
    public Dictionary<string, string> GetSecrets();
}