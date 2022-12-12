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
        /// Contructor to setup default values
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
        /// Contructor to setup default values
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
    public abstract class IndexConverterAttribute<T, C> : JsonConverterAttribute, IIndexAttribute where C : IndexConverter<T>, new()
    {
        public bool IsPrimary { get; set; }
        public bool IsAuto { get; set; }
        public bool IsUnique { get; set; }

        private readonly C _converter;

        /// <summary>
        /// Contructor to setup default values
        /// </summary>
        public IndexConverterAttribute()
        {
            _converter = new C();
            IsPrimary = false;
            IsAuto = false;
            IsUnique = false;
        }

        public override JsonConverter? CreateConverter(Type typeToConvert)
        {
            if (typeToConvert != typeof(T))
            {
                throw new InvalidOperationException($"Invalid TypeConverter. {typeof(C).Name} is not a converter for type {typeToConvert.Name}.");
            }

            return _converter;
        }

        public static KeyValuePair<Type, IIndexConverter> TypeConverterPair()
        {
            return KeyValuePair.Create(typeof(T), (IIndexConverter)(new C()));
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
    public class CompoundIndexAttribute : Attribute
    {
        public string[] Keys { get; init; }
        public bool IsPrimary { get; set; }

        /// <summary>
        /// Contructor to setup default values
        /// </summary>
        public CompoundIndexAttribute(string key1, string key2)
        {
            Keys = new[] { key1, key2 };
            IsPrimary = false;
        }

        public CompoundIndexAttribute(string key1, string key2, string key3)
        {
            Keys = new[] { key1, key2, key3 };
            IsPrimary = false;
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class SchemaAttribute : Attribute
    {
        public string StoreName { get; set; }
        public Type? UpdateStore { get; set; }
        public string PrimaryKeyName { get; set; }
        public bool OutboundPrimaryKey { get; set; }

        /// <summary>
        /// Contructor to setup default values
        /// </summary>
        public SchemaAttribute()
        {
            StoreName = string.Empty;
            UpdateStore = null;
            PrimaryKeyName = string.Empty;
            OutboundPrimaryKey = false;
        }
    }
}
