using System.Text.Json.Serialization;
using DexieCloudNET;

namespace DexieNETCloudPushServer.Services;

public record PushEntity(
    string ID
);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(PushEntity[]))]
public partial class PushEntityContext : JsonSerializerContext
{
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(PushNotification[]))]
public partial class PushNotificationContext : JsonSerializerContext
{
}

public record PushMember(string? UserId);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(PushMember[]))]
public partial class PushMemberContext : JsonSerializerContext
{
}

// ReSharper disable once InconsistentNaming
public record WebPushNotification(string Title, string Body, string Navigate, string? Tag = null, long? App_badge = null, string? Icon =  null, bool? RequireInteraction = null);

// ReSharper disable once InconsistentNaming
public record DeclarativeWebPushNotification(int? Web_push, WebPushNotification Notification)
{
    public static int DeclarativeWebPushMagicNumber = 8030;
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DeclarativeWebPushNotification))]
public partial class DeclarativeWebPushNotificationContext : JsonSerializerContext
{
}
    
public record DatabaseConfig(string Url, string ClientId, string ClientSecret);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(DatabaseConfig[]))]
public partial class DatabaseConfigContext : JsonSerializerContext
{
}
public record VapidKeysConfig(string PublicKey, string PrivateKey);
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(VapidKeysConfig[]))]
public partial class VapidKeysConfigContext : JsonSerializerContext
{
}