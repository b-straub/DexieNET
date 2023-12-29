using DexieNET;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using static DexieNET.UIAlert;

namespace DexieNETCloudSample.Logic
{
    public class ToDoListData(string title)
    {
        [Required(AllowEmptyStrings = false)]
        public string Title { get; set; } = title;

        [Required]
        public DateTime DueDate { get; set; }
    }

    public class ToDoItemData(string text, DateTime date)
    {
        [Required(AllowEmptyStrings = false)]
        public string Text { get; set; } = text;

        [Required]
        public DateTime DueDate { get; set; } = date;
    }

    public class EmailData(string? placeholder)
    {
        [Required(AllowEmptyStrings = false)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; } = string.Empty;

        public string Placeholder { get; set; } = placeholder ?? string.Empty;
    }

    public class OTPData(IEnumerable<UIAlert> alerts)
    {
        [StringLength(8, MinimumLength = 8)]
        [RegularExpression(@"([a-zA-Z0-9]+)", ErrorMessage = "Only letters and numbers allowed")]
        public string OTP { get; set; } = string.Empty;

        public IEnumerable<UIAlert> Alerts { get; set; } = alerts;
    }

    public class CloudKeyData(string? placeholderClientId, string? placeholderClientSecret)
    {
        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public string ClientSecret { get; set; } = string.Empty;

        public string PlaceholderClientId { get; set; } = placeholderClientId ?? string.Empty;
        public string PlaceholderClientSecret { get; set; } = placeholderClientSecret ?? string.Empty;
    }

    public enum DBScopes
    {
        AccessDB,
        Impersonate,
        ManageDB,
        GlobalRead,
        GlobalWrite,
        DeleteDB
    }

    public class EnumSnakeCaseUpperConverter<TEnum> : JsonStringEnumConverter<TEnum> where TEnum : struct, Enum
    {
        public EnumSnakeCaseUpperConverter() : base(JsonNamingPolicy.SnakeCaseUpper) { }
    }

    public class EnumCamelCaseConverter<TEnum> : JsonStringEnumConverter<TEnum> where TEnum : struct, Enum
    {
        public EnumCamelCaseConverter() : base(JsonNamingPolicy.CamelCase) { }
    }

    public record RequestClaims
    (
        string Sub,
        string Email,
        string Name
    );


    public record AccesssTokenRequest
    (
        [property: JsonConverter(typeof(JsonArrayItemConverter<DBScopes, EnumSnakeCaseUpperConverter<DBScopes>>))] DBScopes[] Scopes,
        string ClientID,
        string ClientSecret,
        [property: JsonPropertyOrder(-5)] string GrantType = "client_credentials",
        string? PublicKey = null,
        RequestClaims? Claims = null
    );

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower)]
    [JsonSerializable(typeof(AccesssTokenRequest))]
    internal partial class AccesssTokenRequestContext : JsonSerializerContext
    {
    }

    public record ResponseClaims
    (
        string Sub,
        [property: JsonConverter(typeof(EnumCamelCaseConverter<LicenseStatus>))] LicenseStatus License,
        [property: JsonConverter(typeof(JsonArrayItemConverter<DBScopes, EnumSnakeCaseUpperConverter<DBScopes>>))] DBScopes[] Scopes,
        [property: JsonConverter(typeof(EnumCamelCaseConverter<LicenseType>))] LicenseType UserType
    )
    {
        [property: JsonExtensionData] public Dictionary<string, object>? ClaimName { get; set; }
    }

    public record ResponseAlerts
    (
        [property: JsonConverter(typeof(EnumCamelCaseConverter<AlertType>))] AlertType Type,
        string MessageCode,
        string Message,
        Dictionary<string, string> MessageParams
    );

    public record AccesssTokenResponse
    (
        string Type,
        ResponseClaims Claims,
        string AccessToken,
        long AccessTokenExpiration,
        string? RefreshToken,
        long? RefreshTokenExpiration,
        [property: JsonConverter(typeof(EnumCamelCaseConverter<LicenseType>))] LicenseType UserType,
        long? EvalDaysLeft,
        long? UserValidUntil,
        ResponseAlerts[]? Alerts
    );

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(AccesssTokenResponse))]
    internal partial class AccesssTokenResponseContext : JsonSerializerContext
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
    internal partial class UserRequestResponseContext : JsonSerializerContext
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
    internal partial class UsersResponseContext : JsonSerializerContext
    {
    }

    /// <summary>
    /// Json collection converter.
    /// </summary>
    /// <typeparam name="TDatatype">Type of item to convert.</typeparam>
    /// <typeparam name="TConverterType">Converter to use for individual items.</typeparam>
    public class JsonArrayItemConverter<TDatatype, TConverterType> : JsonConverter<TDatatype[]>
        where TConverterType : JsonConverter
    {
        /// <summary>
        /// Reads a json string and deserializes it into an object.
        /// </summary>
        /// <param name="reader">Json reader.</param>
        /// <param name="typeToConvert">Type to convert.</param>
        /// <param name="options">Serializer options.</param>
        /// <returns>Created object.</returns>
        public override TDatatype[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.Null)
            {
                return [];
            }

            JsonSerializerOptions jsonSerializerOptions = new(options);
            jsonSerializerOptions.Converters.Clear();
            jsonSerializerOptions.Converters.Add(Activator.CreateInstance<TConverterType>());

            List<TDatatype> returnValue = [];

            while (reader.TokenType != JsonTokenType.EndArray)
            {
                if (reader.TokenType != JsonTokenType.StartArray)
                {
                    var item = (TDatatype?)JsonSerializer.Deserialize(ref reader, typeof(TDatatype), jsonSerializerOptions);
                    ArgumentNullException.ThrowIfNull(item);
                    returnValue.Add(item);
                }

                reader.Read();
            }

            return [.. returnValue];
        }

        /// <summary>
        /// Writes a json string.
        /// </summary>
        /// <param name="writer">Json writer.</param>
        /// <param name="value">Value to write.</param>
        /// <param name="options">Serializer options.</param>
        public override void Write(Utf8JsonWriter writer, TDatatype[] value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNullValue();
                return;
            }

            JsonSerializerOptions jsonSerializerOptions = new(options);
            jsonSerializerOptions.Converters.Clear();
            jsonSerializerOptions.Converters.Add(Activator.CreateInstance<TConverterType>());

            writer.WriteStartArray();

            foreach (TDatatype data in value)
            {
                JsonSerializer.Serialize(writer, data, jsonSerializerOptions);
            }

            writer.WriteEndArray();
        }
    }
}
