using System.Text.Json;
using System.Text.Json.Serialization;

namespace DexieCloudNET
{
    internal class PermissionArrayConverter : JsonConverter<string[]>
    {
        public override string[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            return CloudJsonExtensions.ReadStringOrArray(ref reader, options);
        }

        public override void Write(Utf8JsonWriter writer, string[] values, JsonSerializerOptions options)
        {
            writer.WriteStringArray(values, options);
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string) || objectType == typeof(string[]);
        }
    }

    internal class PermissionArrayConverterAttribute : JsonConverterAttribute
    {
        public override JsonConverter CreateConverter(Type typeToConvert)
        {
            if (typeToConvert != typeof(string[]))
            {
                throw new NotSupportedException(typeToConvert.ToString());
            }

            return new PermissionArrayConverter();
        }
    }

    internal class PermissionDictionaryConverter : JsonConverter<Dictionary<string, string[]>>
    {
        public override Dictionary<string, string[]> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var dictionary = new Dictionary<string, string[]>();

            if (reader.TokenType is JsonTokenType.String)
            {
                dictionary.Add("*", new[] { "*" });
                return dictionary;
            }

            if (reader.TokenType is not JsonTokenType.StartObject)
            {
                throw new JsonException($"JsonTokenType was of type {reader.TokenType}, only objects are supported");
            }

            while (reader.Read())
            {
                if (reader.TokenType == JsonTokenType.EndObject)
                {
                    return dictionary;
                }

                if (reader.TokenType != JsonTokenType.PropertyName)
                {
                    throw new JsonException("JsonTokenType was not PropertyName");
                }

                var propertyName = reader.GetString();

                if (string.IsNullOrWhiteSpace(propertyName))
                {
                    throw new JsonException("Failed to get property name");
                }

                reader.Read();
                dictionary.Add(propertyName, CloudJsonExtensions.ReadStringOrArray(ref reader, options));
            }

            return dictionary;
        }

        public override void Write(Utf8JsonWriter writer, Dictionary<string, string[]> dictionary, JsonSerializerOptions options)
        {
            if (dictionary.Count == 1 && dictionary.First().Key == "*" && dictionary.First().Value.First() == "*")
            {
                writer.WriteStringArray(dictionary.First().Value, options);
                return;
            }

            writer.WriteStartObject();

            foreach (var (key, value) in dictionary.Select(d => (d.Key, d.Value)))
            {
                writer.WritePropertyName(key);
                writer.WriteStringArray(value, options);
            }

            writer.WriteEndObject();
        }

        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(string) ||
                objectType == typeof(Dictionary<string, string>) ||
                objectType == typeof(Dictionary<string, string[]>);
        }
    }

    internal class PermissionDictionaryConverterAttribute : JsonConverterAttribute
    {
        public override JsonConverter CreateConverter(Type typeToConvert)
        {
            if (typeToConvert != typeof(Dictionary<string, string[]>))
            {
                throw new NotSupportedException(typeToConvert.ToString());
            }

            return new PermissionDictionaryConverter();
        }
    }

    public class EnumSnakeCaseUpperConverter<TEnum> : JsonStringEnumConverter<TEnum> where TEnum : struct, Enum
    {
        public EnumSnakeCaseUpperConverter() : base(JsonNamingPolicy.SnakeCaseUpper) { }
    }

    public class EnumCamelCaseConverter<TEnum> : JsonStringEnumConverter<TEnum> where TEnum : struct, Enum
    {
        public EnumCamelCaseConverter() : base(JsonNamingPolicy.CamelCase) { }
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

    internal static class CloudJsonExtensions
    {
        internal static string[] ReadStringOrArray(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.StartArray)
            {
                var array = JsonSerializer.Deserialize<string[]>(ref reader, options);
                return array is null ? throw new JsonException("ReadStringArray: null value not supported here!") : array;
            }
            else if (reader.TokenType == JsonTokenType.String)
            {
                var value = JsonSerializer.Deserialize<string>(ref reader, options);
                return value is null ? throw new JsonException("ReadStringArray: null value not supported here!") : new[] { value };
            }

            throw new JsonException($"ReadStringOrArray: {reader.TokenType} not supported here!");
        }

        internal static void WriteStringArray(this Utf8JsonWriter writer, string[] values, JsonSerializerOptions options)
        {
            if (values.FirstOrDefault() == "*")
            {
                JsonSerializer.Serialize(writer, values.First(), options);
            }
            else
            {
                JsonSerializer.Serialize(writer, values, options);
            }
        }
    }
}

