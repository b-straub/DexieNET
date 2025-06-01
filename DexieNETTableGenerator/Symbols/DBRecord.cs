/*
DBRecord.cs

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
*/

using DNTGenerator.Helpers;
using Microsoft.CodeAnalysis;
using System;
using System.Threading;
using static DNTGenerator.Verifier.DBRecordExtensions;

namespace DNTGenerator.Verifier
{
    public class DBRecord(INamedTypeSymbol symbol, DBRecord.RecordType recordType, bool isPartial, Compilation compilation, CancellationToken cancellationToken)
    {
        public enum RecordType
        {
            Interface,
            Class,
            Record,
            RecordStruct,
            Struct
        }

        public INamedTypeSymbol Symbol { get; } = symbol;

        public IEnumerable<IndexDescriptor> Properties { get; } = symbol.GetIndexProperties(compilation, cancellationToken);

        public SchemaDescriptor SchemaDescriptor { get; } = symbol.GetSchemaDescriptor(recordType is RecordType.Interface, compilation, cancellationToken);

        public IEnumerable<(IEnumerable<(string Name, Location Location)> Keys, bool IsPrimary, Location PKLocation)> CompoundKeys { get; } = symbol.GetCompoundKeys(compilation, cancellationToken);

        public string Namespace => Symbol.ContainingNamespace.ToDisplayString();

        public string AccessToString => Symbol.DeclaredAccessibility.ToString().ToLowerInvariant();

        public RecordType Type { get; } = recordType;

        public bool IsPartial { get; } = isPartial;

        public string DBName { get; private set; } = symbol.Name.MakeDBOrTableName(true, recordType is RecordType.Interface);

        public void SetDBName(string dbName)
        {
            DBName = dbName;
        }

        public bool HasPushSupport { get; set; }
        
        public string TypeName => Type switch
        {
            RecordType.Interface => throw new InvalidOperationException("Interface can not be used as name."),
            RecordType.Class => "class",
            RecordType.Record => "record",
            RecordType.RecordStruct => "record struct",
            RecordType.Struct => "struct",
            _ => throw new NotImplementedException()
        };
    }
}
