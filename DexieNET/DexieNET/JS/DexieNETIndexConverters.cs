/*
DexieNETIndexConverters.cs

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

namespace DexieNET
{
    public class BoolIC : IndexConverter<bool>
    {
        public override object Convert(object value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            return (bool)value ? 1 : 0;
        }

        public override bool Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var b = reader.GetInt32();

            return b != 0;
        }

        public override void Write(Utf8JsonWriter writer, bool value, JsonSerializerOptions options) =>
                writer.WriteNumberValue((int)Convert(value));
    }

    public class BoolIndexAttribute : IndexConverterAttribute<bool, BoolIC>
    {
    }

    public class ByteIC : IndexConverter<byte[]>
    {
        public override object Convert(object value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var blob = value as byte[];
            ArgumentNullException.ThrowIfNull(blob);

            var base64 = System.Convert.ToBase64String(blob);
            return base64;
        }

        public override byte[] Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var s = reader.GetString();

            if (s is null)
            {
                throw new JsonException("Can not read value for converting Blob from Json.");
            }

            var arrayValue = System.Convert.FromBase64String(s);
            return arrayValue;
        }

        public override void Write(Utf8JsonWriter writer, byte[] value, JsonSerializerOptions options) =>
                writer.WriteStringValue((string)Convert(value));
    }

    public class ByteIndexAttribute : IndexConverterAttribute<byte[], ByteIC>
    {
    }
}
