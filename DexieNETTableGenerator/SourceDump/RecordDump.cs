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
using System.Text;

namespace DNTGenerator.SourceDump
{
    internal static class RecordDump
    {
        public static string MakeTable(this DBRecord record, IEnumerable<DBRecord> records)
        {
            StringBuilder sb = new();

            var primaryIndexTypeName = record.HasGeneratedPrimaryKey() ? "ulong" : record.GetPrimaryIndexTypeName();
            var tablePropertyName = $"_{record.SchemaDescriptor.StoreName.LowerFirstChar()}Table";

            if (primaryIndexTypeName is null)
            {
                _ = sb.Append($@"
        public async ValueTask<Table<{record.Symbol.Name}, I>> {record.SchemaDescriptor.StoreName}<I>()
        {{  
            {record.MakeConverter()}
            var reference = await _jso.InvokeAsync<IJSObjectReference>(""table"", ""{record.GetStoreBaseName(records)}"");
            return new Table<{record.Symbol.Name}, I>(this, reference, ""{record.GetStoreBaseName(records)}"", converter, {record.Keys(false)}, {record.Keys(true)});
        }}");
            }
            else
            {
                _ = sb.Append($@"
        public async ValueTask<Table<{record.Symbol.Name}, {primaryIndexTypeName}>> {record.SchemaDescriptor.StoreName}()
        {{
            if ({tablePropertyName} is not null)
            {{
                return {tablePropertyName};
            }}

            {record.MakeConverter()}
            var reference = await _jso.InvokeAsync<IJSObjectReference>(""table"", ""{record.GetStoreBaseName(records)}"");
            var table = new Table<{record.Symbol.Name}, {primaryIndexTypeName}>(this, reference, ""{record.GetStoreBaseName(records)}"", converter, {record.Keys(false)}, {record.Keys(true)});

            {tablePropertyName} = table;
            return table;
        }}
");
            }

            return sb.ToString();
        }

        public static string MakeTransaction(this DBRecord record, IEnumerable<DBRecord> records)
        {
            StringBuilder sb = new();

            var primaryIndexTypeName = record.HasGeneratedPrimaryKey() ? "ulong" : record.GetPrimaryIndexTypeName();

            if (primaryIndexTypeName is null)
            {
                _ = sb.Append($@"
        public static async ValueTask<Table<{record.Symbol.Name}, I>> {record.SchemaDescriptor.StoreName}<I>(this TransactionBase transaction)
        {{
            {record.MakeConverter()}
            var reference = await transaction.InvokeAsync<IJSObjectReference>(""table"", ""{record.GetStoreBaseName(records)}"");
            return new Table<{record.Symbol.Name}, I>(transaction.DB, reference, ""{record.GetStoreBaseName(records)}"", converter, {record.Keys(false)}, {record.Keys(true)});
        }}");
            }
            else
            {
                _ = sb.Append($@"
        public static async ValueTask<Table<{record.Symbol.Name}, {primaryIndexTypeName}>> {record.SchemaDescriptor.StoreName}(this TransactionBase transaction)
        {{
            {record.MakeConverter()}
            var reference = await transaction.InvokeAsync<IJSObjectReference>(""table"", ""{record.GetStoreBaseName(records)}"");
            return new Table<{record.Symbol.Name}, {primaryIndexTypeName}>(transaction.DB, reference, ""{record.GetStoreBaseName(records)}"", converter, {record.Keys(false)}, {record.Keys(true)});
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

        public static string MakeDBBuilder(this IEnumerable<DBRecord> records, string dbName)
        {
            StringBuilder sb = new();

            _ = sb.Append($@"

    public class {dbName}: DBBase, IDBBase
    {{
        public static string Name => ""{dbName}"";
");
            foreach (DBRecord record in records)
            {
                var primaryIndexTypeName = record.HasGeneratedPrimaryKey() ? "ulong" : record.GetPrimaryIndexTypeName();
                var tablePropertyName = $"_{record.SchemaDescriptor.StoreName.LowerFirstChar()}Table";

                if (primaryIndexTypeName is not null)
                {
                    _ = sb.Append($@"
        private Table<{record.Symbol.Name}, {primaryIndexTypeName}>? {tablePropertyName};");
                }
            }
            _ = sb.Append($@"

        private readonly JSObject _jso;

        private {dbName}(IJSInProcessObjectReference module, IJSObjectReference reference) : base(module, reference)
        {{
            _jso = new(module, reference);
        }}
       
        public static IDBBase Create(IJSInProcessObjectReference module, IJSObjectReference reference)
        {{
            return new {dbName}(module, reference);
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
                    ""{record.Symbol.Name.ToLowerInvariant()}"", (""{StoreBaseName}"", ""{Schema}"")
                }},");
            }

            _ = sb.Append($@"
            }};

            return stores;
        }}

        public async override ValueTask<DexieNET.Version> Version(double versionNumber)
        {{
            var reference = await _jso.InvokeAsync<IJSObjectReference>(""version"", versionNumber);
            return new DexieNET.Version(this, GetStores(), GetUpdateStores(), reference);
        }}");

            return sb.ToString().TrimEnd();
        }
    }
}
