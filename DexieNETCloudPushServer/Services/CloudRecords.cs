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

public record WebPushMessage(string Title, string Message, string Icon, bool RequireInteraction, bool? SetBadge = null, string? PayloadJson = null , string? Tag = null);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(WebPushMessage))]
public partial class WebPushMessageContext : JsonSerializerContext
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