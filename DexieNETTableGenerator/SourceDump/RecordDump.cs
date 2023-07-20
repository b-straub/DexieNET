/*
RecordDump.cs

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
*/

using DNTGenerator.Helpers;
using DNTGenerator.Verifier;
using Microsoft.CodeAnalysis;
using System;
using System.Reflection;
using System.Text;

namespace DNTGenerator.SourceDump
{
    internal static class RecordDump
    {
        public static string MakeTable(this DBRecord record, IEnumerable<DBRecord> records)
        {
            StringBuilder sb = new();

            var primaryIndexTypeName = record.PrimaryIndexTypeName();
            var tablePropertyName = $"_{record.SchemaDescriptor.StoreName.LowerFirstChar()}Table";

            if (primaryIndexTypeName is null)
            {
                _ = sb.Append($@"
        public Table<{record.Symbol.Name}, I> {record.SchemaDescriptor.StoreName}<I>()
        {{  
            {record.MakeConverter()}
            var reference = _jso.Invoke<IJSInProcessObjectReference>(""table"", ""{record.GetStoreBaseName(records)}"");
            return new Table<{record.Symbol.Name}, I>(this, reference, ""{record.GetStoreBaseName(records)}"", converter, {record.Keys(false)}, {record.Keys(true)}, {(record.HasNonCloudGuidPrimaryKey() ? "true" : "false")}, {(record.SchemaDescriptor.HasCloudSync ? "true" : "false")});
        }}");
            }
            else
            {
                _ = sb.Append($@"
        public Table<{record.Symbol.Name}, {primaryIndexTypeName}> {record.SchemaDescriptor.StoreName}()
        {{
            if ({tablePropertyName} is not null)
            {{
                return {tablePropertyName};
            }}

            {record.MakeConverter()}
            var reference = _jso.Invoke<IJSInProcessObjectReference>(""table"", ""{record.GetStoreBaseName(records)}"");
            var table = new Table<{record.Symbol.Name}, {primaryIndexTypeName}>(this, reference, ""{record.GetStoreBaseName(records)}"", converter, {record.Keys(false)}, {record.Keys(true)}, {(record.HasNonCloudGuidPrimaryKey() ? "true" : "false")}, {(record.SchemaDescriptor.HasCloudSync ? "true" : "false")});

            {tablePropertyName} = table;
            return table;
        }}
");
            }

            return sb.ToString();
        }

        public static string MakeCloudTables(this IEnumerable<DBRecord> records)
        {
            if (!records.HasCloudSync())
            {
                return string.Empty;
            }

            StringBuilder sb = new();

            _ = sb.Append($@"
        public Table<Realm, string> Realms()
        {{
            if (_realms is not null)
            {{
                return _realms;
            }}

            {MakeEmptyConverter("Realm")};
            var reference = _jso.Invoke<IJSInProcessObjectReference>(""table"", ""realms"");
            var table = new Table<Realm, string>(this, reference, ""realms"", converter, new string[] {{""realmId""}}, Enumerable.Empty<string>().ToArray(), false, true);

            _realms = table;
            return table;
        }}

        public Table<Member, string> Members()
        {{
            if (_members is not null)
            {{
                return _members;
            }}

            {MakeEmptyConverter("Member")};
            var reference = _jso.Invoke<IJSInProcessObjectReference>(""table"", ""members"");
            var table = new Table<Member, string>(this, reference, ""members"", converter, new string[] {{""id"", ""[userId+realmId]"", ""[email+realmId]"", ""realmId""}}, Enumerable.Empty<string>().ToArray(), false, true);

            _members = table;
            return table;
        }}

        public Table<Role, (string, string)> Roles()
        {{
            if (_roles is not null)
            {{
                return _roles;
            }}

            {MakeEmptyConverter("Role")};
            var reference = _jso.Invoke<IJSInProcessObjectReference>(""table"", ""roles"");
            var table = new Table<Role, (string, string)>(this, reference, ""roles"", converter, new string[] {{""[realmId+name]""}}, Enumerable.Empty<string>().ToArray(), false, true);

            _roles = table;
            return table;
        }}
");

            return sb.ToString();
        }


        public static string MakeTransaction(this DBRecord record, IEnumerable<DBRecord> records)
        {
            StringBuilder sb = new();

            var primaryIndexTypeName = record.PrimaryIndexTypeName();

            if (primaryIndexTypeName is null)
            {
                _ = sb.Append($@"
        public static Table<{record.Symbol.Name}, I> {record.SchemaDescriptor.StoreName}<I>(this Transaction transaction)
        {{
            {record.MakeConverter()}
            var reference = transaction.Invoke<IJSInProcessObjectReference>(""table"", ""{record.GetStoreBaseName(records)}"");
            return new Table<{record.Symbol.Name}, I>(transaction.DB, reference, ""{record.GetStoreBaseName(records)}"", converter, {record.Keys(false)}, {record.Keys(true)}, {(record.HasNonCloudGuidPrimaryKey() ? "true" : "false")}, {(record.SchemaDescriptor.HasCloudSync ? "true" : "false")});
        }}");
            }
            else
            {
                _ = sb.Append($@"
        public static Table<{record.Symbol.Name}, {primaryIndexTypeName}> {record.SchemaDescriptor.StoreName}(this Transaction transaction)
        {{
            {record.MakeConverter()}
            var reference = transaction.Invoke<IJSInProcessObjectReference>(""table"", ""{record.GetStoreBaseName(records)}"");
            return new Table<{record.Symbol.Name}, {primaryIndexTypeName}>(transaction.DB, reference, ""{record.GetStoreBaseName(records)}"", converter, {record.Keys(false)}, {record.Keys(true)}, {(record.HasNonCloudGuidPrimaryKey() ? "true" : "false")}, {(record.SchemaDescriptor.HasCloudSync ? "true" : "false")});
        }}
");
            }

            return sb.ToString();
        }

        private static string MakeConverter(this DBRecord record)
        {
            StringBuilder sb = new();

            var converters = record.GetIndexConverters();

            if (converters.Any())
            {
                sb.Append($@"
            var typeConverterPairs = new List<KeyValuePair<Type, IIndexConverter>>();");

                foreach (var converter in converters)
                {
                    sb.Append($@"
            typeConverterPairs.AddRange({converter}.TypeConverterPairs());");
                }
            }
            else
            {
                sb.Append($@"
            var typeConverterPairs = Enumerable.Empty<KeyValuePair<Type, IIndexConverter>>();");
            }

            sb.Append($@"
            var converter = new TypeConverter<{record.Symbol.Name}>(typeConverterPairs);
");
            return sb.ToString().TrimStart();
        }


        private static string MakeEmptyConverter(string name)
        {
            StringBuilder sb = new();

            sb.Append($@"
            var typeConverterPairs = Enumerable.Empty<KeyValuePair<Type, IIndexConverter>>();");

            sb.Append($@"
            var converter = new TypeConverter<{name}>(typeConverterPairs);
");
            return sb.ToString().TrimStart();
        }

        public static string MakeDBBuilder(this IEnumerable<DBRecord> records, string dbName)
        {
            StringBuilder sb = new();

            _ = sb.Append($@"

    public class {dbName}: DBBase, IDBBase
    {{
        public static string Name => ""{dbName}"";
        {records.DumpUnsyncedTables()}
");
            foreach (DBRecord record in records)
            {
                var primaryIndexTypeName = record.PrimaryIndexTypeName();
                var tablePropertyName = $"_{record.SchemaDescriptor.StoreName.LowerFirstChar()}Table";

                if (primaryIndexTypeName is not null)
                {
                    _ = sb.Append($@"
        private Table<{record.Symbol.Name}, {primaryIndexTypeName}>? {tablePropertyName};");
                }
            }

            if (records.HasCloudSync())
            {
                _ = sb.Append($@"
        private Table<Realm, string>? _realms;
        private Table<Member, string>? _members;
        private Table<Role, (string, string)>? _roles;");
            }

            _ = sb.Append($@"

        private readonly DexieJSObject _jso;

        private {dbName}(IJSInProcessObjectReference module, IJSInProcessObjectReference reference, bool cloudSync) : base(module, reference, cloudSync)
        {{
            _jso = new(module, reference);
        }}
       
        public static IDBBase Create(IJSInProcessObjectReference module, IJSInProcessObjectReference reference, bool cloudSync)
        {{
            return new {dbName}(module, reference, cloudSync);
        }}

        {records.DumpTables()}
        {records.DumpStores()}
    }}
");

            return sb.ToString();
        }

        private static string DumpTables(this IEnumerable<DBRecord> records)
        {
            StringBuilder sb = new();

            foreach (DBRecord record in records)
            {
                _ = sb.Append(record.MakeTable(records));
            }

            _ = sb.Append(records.MakeCloudTables());

            return sb.ToString().TrimEnd();
        }

        public static string DumpProperties(this DBRecord record)
        {
            StringBuilder sb = new();

            var name = Assembly.GetExecutingAssembly().GetName().Name;
            var version = Assembly.GetExecutingAssembly().GetName().Version;
            var attribute = @$"[GeneratedCode(""{name}"", ""{version}"")]";

            var primaryIndexName = record.GetPrimaryIndexName(false);
            var primaryIndexTypeName = record.PrimaryIndexTypeName();
            var getSet =
            (record.Type is DBRecord.RecordType.Record || record.Type is DBRecord.RecordType.RecordStruct) ?
            "{ get; init; }" : "{ get; set; }";


            if (record.HasGeneratedPrimaryKey() || (record.HasGeneratedPrimaryKey() && record.HasNonCloudGuidPrimaryKey()))
            {
                _ = sb.Append($@"
        {attribute}
        public {primaryIndexTypeName}? {primaryIndexName} {getSet} = null;
");
            }

            if (record.SchemaDescriptor.HasCloudSync)
            {
                _ = sb.Append($@"
        {attribute}
        public string? Owner {getSet} = null;

        {attribute}
        public string? RealmId {getSet} = null;
");
            }

            return sb.ToString().TrimEnd();
        }

        public static string DumpMethods(this DBRecord record)
        {
            StringBuilder sb = new();
            var primaryIndexName = record.GetPrimaryIndexName(false);

            bool unmutable = (record.Type is DBRecord.RecordType.Record || record.Type is DBRecord.RecordType.RecordStruct);

            if (record.HasNonCloudGuidPrimaryKey())
            {
                if (unmutable)
                {
                    _ = sb.Append($@"

        public {record.Symbol.Name} AssignPrimaryKey()
        {{
            if ({primaryIndexName} is null)
            {{
                return this with {{ {primaryIndexName} = Guid.NewGuid().ToString() }};
            }}
            return this;
        }}");
                }
                else
                {
                    _ = sb.Append($@"

        public {record.Symbol.Name} AssignPrimaryKey()
        {{
            if ({primaryIndexName} is null)
            {{
                {primaryIndexName} = Guid.NewGuid().ToString();
            }}
            return this;
        }}");
                }
            }

            return sb.ToString().TrimEnd();
        }

        private static string DumpUnsyncedTables(this IEnumerable<DBRecord> records)
        {
            StringBuilder sb = new();
            StringBuilder sbInner = new();
            var unsyncedTables = records.Where(r => !r.SchemaDescriptor.HasCloudSync)
                .Select(r => r.GetStoreBaseName(records))
                .Distinct();

            _ = sb.Append($@"
        public override string[] UnsyncedTables {{ get; }} = new string[] 
        {{");

            foreach (string table in unsyncedTables)
            {
                _ = sbInner.Append($@"
            ""{table}"",");
            }

            sb.Append(sbInner.ToString().TrimEnd(", "));



            _ = sb.Append($@"
        }};");

            return sb.ToString().TrimEnd();
        }

        private static string DumpStores(this IEnumerable<DBRecord> records)
        {
            StringBuilder sb = new();

            _ = sb.Append($@"
        private static Dictionary<string, string> GetStores()
        {{
            var stores = new Dictionary<string, string>()
            {{");
            foreach (var record in records)
            {
                var (StoreBaseName, Schema, Update) = record.GetSchema(records);

                if (Update)
                {
                    continue;
                }

                _ = sb.Append($@"
                {{ ""{StoreBaseName}"", ""{Schema}"" }},");
            }

            /*if (records.HasCloudSync())
            {
                _ = sb.Append($@"
                {{ ""realms"", ""@realmId"" }},
                {{ ""members"", ""@id,[realmId+email],[userId+realmId],realmId"" }},
                {{ ""roles"", ""[realmId+name]"" }}");
            }*/

            _ = sb.Append($@"
            }};

            return stores;
        }}

        private static Dictionary<string, (string, string)> GetUpdateStores()
        {{
            var stores = new Dictionary<string, (string, string)>()
            {{");
            foreach (var record in records)
            {
                var (StoreBaseName, Schema, Update) = record.GetSchema(records);

                if (!Update)
                {
                    continue;
                }

                _ = sb.Append($@"
                {{  
                    ""{record.Symbol.Name.ToCamelCase()}"", (""{StoreBaseName}"", ""{Schema}"")
                }},");
            }

            _ = sb.Append($@"
            }};

            return stores;
        }}

        public override DexieNET.Version Version(double versionNumber)
        {{
            var reference = _jso.Invoke<IJSInProcessObjectReference>(""version"", versionNumber);
            return new DexieNET.Version(this, GetStores(), GetUpdateStores(), reference);
        }}");

            return sb.ToString().TrimEnd();
        }
    }
}
