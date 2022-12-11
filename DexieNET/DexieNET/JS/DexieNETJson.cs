/*
DexieNETJson.cs

Copyright(c) 2022 Bernhard Straub

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

using System.Text.Json;
using System.Text.RegularExpressions;

namespace DexieNET
{
    internal class JsonNamingPolicyLower : JsonNamingPolicy
    {
        public override string ConvertName(string name)
        {
            return name.ToLowerInvariant();
        }
    }

    public static class JsonExtensions
    {
        public static object? ToObject(this JsonElement element, Type type)
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return element.Deserialize(type, options);
            }
            catch
            {
            }

            return default;
        }

        public static T? ToObject<T>(this JsonElement element)
        {
            if (element.ValueKind is JsonValueKind.Undefined)
            {
                return default;
            }

            try
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                return (T?)element.Deserialize(typeof(T), options);
            }
            catch
            {
                throw new InvalidOperationException($"Can not convert {element} to {typeof(T).Name}.");
            }
        }

        internal static JsonElement? FromObject(this object? element)
        {
            if (element is null)
            {
                return null;
            }

            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = new JsonNamingPolicyLower(),
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never
            };

            var json = JsonSerializer.SerializeToElement(element, options);
            return json;
        }

        internal static string? FromObjectS(this object? element)
        {
            if (element is null)
            {
                return null;
            }

            var json = JsonSerializer.Serialize(element);
            string regexPattern = "\"([^\"]+)\":"; // the "propertyName": pattern

            json = Regex.Replace(json, regexPattern, m =>
            {
                return m.ToString().Replace("\"", string.Empty).ToLower();
            });

            return json;
        }
    }
}
