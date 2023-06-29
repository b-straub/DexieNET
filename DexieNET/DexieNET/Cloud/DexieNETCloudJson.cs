using System.Text.Json;
using System.Text.Json.Serialization;

namespace DexieNET
{
    internal class PermissionArrayConverter : JsonConverter<string[]>
    {
        public override string[]? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
        public override JsonConverter? CreateConverter(Type typeToConvert)
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
        public override Dictionary<string, string[]>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
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
                dictionary.Add(propertyName!, CloudJsonExtensions.ReadStringOrArray(ref reader, options));
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
        public override JsonConverter? CreateConverter(Type typeToConvert)
        {
            if (typeToConvert != typeof(Dictionary<string, string[]>))
            {
                throw new NotSupportedException(typeToConvert.ToString());
            }

            return new PermissionDictionaryConverter();
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

