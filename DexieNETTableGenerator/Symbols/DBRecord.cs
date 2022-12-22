/*
DBRecord.cs

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
using Microsoft.CodeAnalysis;
using System;
using System.Threading;
using static DNTGenerator.Verifier.DBRecordExtensions;

namespace DNTGenerator.Verifier
{
    internal class DBRecord
    {
        public enum RecordType
        {
            Interface,
            Class,
            Record,
            RecordStruct,
            Struct
        }

        public INamedTypeSymbol Symbol { get; }

        public IEnumerable<IndexDescriptor> Properties { get; }

        public SchemaDescriptor SchemaDescriptor { get; }

        public IEnumerable<(IEnumerable<(string Name, Location Location)> Keys, bool IsPrimary, Location PKLocation)> CompoundKeys { get; }

        public string Namespace => Symbol.ContainingNamespace.ToDisplayString();

        public string AccessToString => Symbol.DeclaredAccessibility is Accessibility.Public ? "public" : "internal";

        public RecordType Type { get; }

        public bool IsPartial { get; }

        public string DBName { get; private set; }

        public DBRecord(INamedTypeSymbol symbol, RecordType recordType, bool isPartial, Compilation compilation, CancellationToken cancellationToken)
        {
            Symbol = symbol;
            Type = recordType;
            IsPartial = isPartial;
            Properties = symbol.GetIndexProperties(compilation, cancellationToken);
            SchemaDescriptor = symbol.GetSchemaDescriptor(recordType is RecordType.Interface, compilation, cancellationToken);
            CompoundKeys = symbol.GetCompoundKeys(compilation, cancellationToken);
            DBName = symbol.Name.MakeDBOrTableName(true, recordType is RecordType.Interface);
        }

        public void SetDBName(string dbName)
        {
            DBName = dbName;
        }

        public string TypeName => Type switch
        {
            RecordType.Interface => throw new InvalidOperationException("Interface can not be used as name."),
            RecordType.Class => "class",
            RecordType.Record => "record",
            RecordType.RecordStruct => "struct record",
            RecordType.Struct => "struct",
            _ => throw new NotImplementedException()
        };
    }
}
