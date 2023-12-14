/*
DexieNETAttributes.cs

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

using System.Text.Json.Serialization;

namespace DexieNET
{
    public interface IIndexAttribute
    {
        public bool IsPrimary { get; }
        public bool IsAuto { get; }
        public bool IsUnique { get; }
        public bool IsMultiEntry { get; }
    }

    public interface IIndexConverter
    {
        public object Convert(object value);
        public bool CanConvert(object value);
    }

    public abstract class IndexConverter<T> : JsonConverter<T>, IIndexConverter
    {
        public abstract object Convert(object value);

        public bool CanConvert(object value)
        {
            return value.GetType() == typeof(T);
        }
    }

    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = false)]
    public class DBNameAttribute : Attribute
    {
        public string Name { get; private set; }
        /// <summary>
        /// Constructor to setup default values
        /// </summary>
        public DBNameAttribute(string name)
        {
            Name = name;
        }
    }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class IndexAttribute : Attribute, IIndexAttribute
    {
        public bool IsPrimary { get; set; }
        public bool IsAuto { get; set; }
        public bool IsUnique { get; set; }
        public bool IsMultiEntry { get; set; }

        /// <summary>
        /// Constructor to setup default values
        /// </summary>
        public IndexAttribute()
        {
            IsPrimary = false;
            IsAuto = false;
            IsUnique = false;
            IsMultiEntry = false;
        }
    }


    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public abstract class IndexConverterAttribute<T, C1, C2> : JsonConverterAttribute, IIndexAttribute 
        where C1 : IndexConverter<T>, new() where C2 : IndexConverter<IEnumerable<T>>, new()
    {
        public bool IsPrimary { get; set; }
        public bool IsAuto { get; set; }
        public bool IsUnique { get; set; }
        public bool IsMultiEntry { get; set; }

        private readonly C1 _converter;
        private readonly C2 _converterE;

        /// <summary>
        /// Constructor to setup default values
        /// </summary>
        public IndexConverterAttribute()
        {
            _converter = new C1();
            _converterE = new C2();
            IsPrimary = false;
            IsAuto = false;
            IsUnique = false;
            IsMultiEntry = false;
        }

        public override JsonConverter? CreateConverter(Type typeToConvert)
        {
            if (typeToConvert == typeof(T))
            {
                return _converter;
            }

            if (typeToConvert == typeof(IEnumerable<T>))
            {
                return _converterE;
            }

            throw new InvalidOperationException($"Invalid TypeConverter. No converter for type {typeToConvert.Name}.");
        }

        public static KeyValuePair<Type, IIndexConverter>[] TypeConverterPairs()
        {
            KeyValuePair<Type, IIndexConverter>[] kvps =
            [
                KeyValuePair.Create(typeof(T), (IIndexConverter)(new C1())),
                KeyValuePair.Create(typeof(IEnumerable<T>), (IIndexConverter)(new C2()))
            ];

            return kvps;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class CompoundIndexAttribute : Attribute
    {
        public string[] Keys { get; init; }
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Constructor to setup default values
        /// </summary>
        public CompoundIndexAttribute(string key1, string key2)
        {
            Keys = [key1, key2];
            IsPrimary = false;
        }

        public CompoundIndexAttribute(string key1, string key2, string key3)
        {
            Keys = [key1, key2, key3];
            IsPrimary = false;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class SchemaAttribute : Attribute
    {
        public string StoreName { get; set; }
        public Type? UpdateStore { get; set; }
        public string PrimaryKeyName { get; set; }
        public bool PrimaryKeyGuid { get; set; }
        public bool OutboundPrimaryKey { get; set; }

        /// <summary>
        /// Constructor to setup default values
        /// </summary>
        public SchemaAttribute()
        {
            StoreName = string.Empty;
            UpdateStore = null;
            PrimaryKeyName = string.Empty;
            OutboundPrimaryKey = false;
            PrimaryKeyGuid = true;
        }
    }
}
