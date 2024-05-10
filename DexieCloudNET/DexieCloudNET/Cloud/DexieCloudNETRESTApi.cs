/*
DexieNETCloudRESTApi.cs

Copyright(c) 2024 Bernhard Straub

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

'DexieNET' used with permission of David Fahlander 
*/

using System.Text.Json.Serialization;

namespace DexieCloudNET
{
    public enum DBScopes
    {
        AccessDB,
        Impersonate,
        ManageDB,
        GlobalRead,
        GlobalWrite,
        DeleteDB
    }

    public record TokenRequestClaims
    (
        string Sub,
        string Email,
        string Name
    );


    public record ClientCredentialsTokenRequest
    (
        [property: JsonConverter(typeof(JsonArrayItemConverter<DBScopes, EnumSnakeCaseUpperConverter<DBScopes>>))] DBScopes[] Scopes,
        string ClientID,
        string ClientSecret,
        string? PublicKey = null,
        TokenRequestClaims? Claims = null,
        [property: JsonPropertyOrder(-5)] string GrantType = "client_credentials"
    );

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    [JsonSerializable(typeof(ClientCredentialsTokenRequest))]
    public partial class ClientCredentialsTokenRequestContext : JsonSerializerContext
    {
    }

    public record TokenHint(string? UserdID, string? EMail);
    public record TokenParams(string Public_key, TokenHint? Hints);
    public record TokenResponseClaims
    (
        string Sub,
        [property: JsonConverter(typeof(EnumCamelCaseConverter<LicenseStatus>))] LicenseStatus License,
        [property: JsonConverter(typeof(JsonArrayItemConverter<DBScopes, EnumSnakeCaseUpperConverter<DBScopes>>))] DBScopes[] Scopes,
        [property: JsonConverter(typeof(EnumCamelCaseConverter<LicenseType>))] LicenseType UserType
    )
    {
        [property: JsonExtensionData] public Dictionary<string, object>? ClaimName { get; set; }
    }

    public record TokenFinalResponse
    (
        string Type,
        TokenResponseClaims Claims,
        string AccessToken,
        long AccessTokenExpiration,
        string? RefreshToken,
        long? RefreshTokenExpiration,
        [property: JsonConverter(typeof(EnumCamelCaseConverter<LicenseType>))] LicenseType UserType,
        long? EvalDaysLeft,
        long? UserValidUntil,
        UIAlert[]? Alerts
    );

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(TokenFinalResponse))]
    public partial class TokenFinalResponseContext : JsonSerializerContext
    {
    }

    public record UserData
    (
        string? DisplayName = null,
        string? Email = null
    )
    {
        [property: JsonExtensionData] public Dictionary<string, object>? Key { get; set; }
    }

    public record UserRequest
    (
        string UserId,
        UserData? Data = null,
        [property: JsonConverter(typeof(EnumCamelCaseConverter<LicenseType>))] LicenseType? Type = null,
        DateTime? ValidUntil = null,
        long? EvalDaysLeft = null,
        DateTime? Deactivated = null
    );

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(UserRequest))]
    internal partial class UserRequestContext : JsonSerializerContext
    {
    }

    public record UserResponse
    (
        string UserId,
        DateTime Created,
        DateTime Updated,
        DateTime? LastLogin,
        [property: JsonConverter(typeof(EnumCamelCaseConverter<LicenseType>))] LicenseType Type,
        DateTime? ValidUntil,
        long? EvalDaysLeft,
        long MaxAllowedEvalDaysLeft,
        DateTime? Deactivated,
        UserData Data
    );

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonSerializable(typeof(UserResponse))]
    public partial class UserRequestResponseContext : JsonSerializerContext
    {
    }

    public record UsersResponse
    (
        UserResponse[] Data,
        bool HasMore,
        string? PagingKey
    );

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(UsersResponse))]
    public partial class UsersResponseContext : JsonSerializerContext
    {
    }
}
