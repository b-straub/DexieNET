/*
DexieNETTypeConverter.cs

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

using System.Diagnostics.CodeAnalysis;

namespace DexieNET
{
    public interface ITypeConverter
    {
        [return: NotNullIfNotNull(nameof(value))]
        public object? Convert(object? value);

        public IDictionary<string, object?> Convert(IDictionary<string, object?> keyObject);
    }

    public sealed class TypeConverter<T> : ITypeConverter where T : IDBStore
    {
        private readonly Dictionary<Type, IIndexConverter> _convertersForType;

        public TypeConverter(IEnumerable<KeyValuePair<Type, IIndexConverter>> converters)
        {
            foreach (var converter in converters)
            {
                if (converters.Where(c => c.Key == converter.Key).Count() > 1)
                {
                    throw new InvalidOperationException($"Invalid TypeConverters. {typeof(T).Name} has different Converters for same type {converter.Key.Name}.");
                }
            }

            _convertersForType = new(converters);
        }

        public IDictionary<string, object?> Convert(IDictionary<string, object?> keyObject)
        {
            var dictC = keyObject.ToDictionary(k => k.Key.ToCamelCase(), k => Convert(k.Value));
            return dictC;
        }

        public object[] Convert(object[] keys)
        {
            var keysC = keys.Select(k => Convert(k))
                .Where(k => k is not null)
                .Select(k => k!)
                .ToArray();
            return keysC;
        }

        [return: NotNullIfNotNull(nameof(value))]
        public object? Convert(object? value)
        {
            if (value is null)
            {
                return null;
            }

            if (_convertersForType.TryGetValue(value.GetType(), out IIndexConverter? converter))
            {
                if (converter != null && converter.CanConvert(value))
                {
                    return converter.Convert(value);
                }
            }

            return value;
        }
    }
}