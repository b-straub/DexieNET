/*
SymbolQuery.cs

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
using DNTGenerator.Matcher;
using DNTGenerator.Verifier;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Threading;

namespace DNTGenerator.Query
{
    internal static class SymbolQuery
    {
        public static string DexieNETNamespace = "DexieNET";
        public static string DexieNETDBStore = "IDBStore";
        public static string DexieNETDBStoreFullName = DexieNETNamespace + "." + "IDBStore";

        public static DBRecord? CreateDBRecord(this TypeDeclarationSyntax td, Compilation compilation, CancellationToken cancellationToken)
        {
            SemanticModel? semanticModel = compilation.GetSemanticModel(td.SyntaxTree);
            INamedTypeSymbol? symbol = semanticModel.GetDeclaredSymbol(td, cancellationToken);
            var isPartial = td.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword));

            var recordType = td.Kind() switch
            {
                SyntaxKind.InterfaceDeclaration => DBRecord.RecordType.Interface,
                SyntaxKind.ClassDeclaration => DBRecord.RecordType.Class,
                SyntaxKind.RecordDeclaration => DBRecord.RecordType.Record,
                SyntaxKind.RecordStructDeclaration => DBRecord.RecordType.RecordStruct,
                SyntaxKind.StructDeclaration => DBRecord.RecordType.Struct,
                _ => throw new ArgumentOutOfRangeException(td.Kind().ToString())
            };

            var record = symbol is not null && MatchDBStoreInterface(symbol, compilation)
                ? new DBRecord(symbol, recordType, isPartial, compilation, cancellationToken)
                : null;

            if (record is not null && record.Type == DBRecord.RecordType.Interface)
            {
                var dbName = record.Symbol.Name.MakeDBOrTableName(true, true);

                string? dbAttributeName = null;
                var attr = record.Symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.MatchDBNameAttribute(compilation));

                if (attr is not null)
                {
                    dbAttributeName = (string?)attr.ConstructorArguments.FirstOrDefault().Value;
                }

                record.SetDBName(dbAttributeName ?? dbName);
                
                attr = record.Symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass.MatchDBAddPushSupportAttribute(compilation));
                if (attr is not null)
                {
                    record.HasPushSupport = true;
                }
            }

            return record;
        }

        public static void SetDerivedProperties(this IList<DBRecord> records, DBRecord nameRecord, CancellationToken cancellationToken)
        {
            foreach (DBRecord record in records)
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (record.Type is DBRecord.RecordType.Interface) continue;
                
                if (record.Symbol.MatchDerivedDBItemInterface(nameRecord.Symbol))
                {
                    record.SetDBName(nameRecord.DBName);
                    record.HasPushSupport = nameRecord.HasPushSupport;
                }
            }
        }

        public static IEnumerable<DBRecord> DBRecords(this IEnumerable<TypeDeclarationSyntax> tds, Compilation compilation, CancellationToken cancellationToken)
        {
            return tds
               .Select(td => td.CreateDBRecord(compilation, cancellationToken))
               .WhereNotNull();
        }

        private static bool MatchDBStoreInterface(this INamedTypeSymbol? ocs, Compilation compilation)
        {
            INamedTypeSymbol? interfaceSymbol = compilation.GetTypeByMetadataName(DexieNETDBStoreFullName);

            bool implementInterface = (ocs?.AllInterfaces.Where(i => i.ConstructedFrom.Equals(interfaceSymbol, SymbolEqualityComparer.Default)).Any()).True();

            return implementInterface;
        }

        public static bool MatchDerivedDBItemInterface(this INamedTypeSymbol? ocs, INamedTypeSymbol interfaceSymbol)
        {
            bool implementInterface = (ocs?.AllInterfaces.Where(i => i.ConstructedFrom.Equals(interfaceSymbol, SymbolEqualityComparer.Default)).Any()).True();

            return implementInterface;
        }

        public static string? GetGenericType(this IPropertySymbol ocs)
        {
            if (ocs.Type is not INamedTypeSymbol type || !type.IsGenericType || type.TypeArguments.Length != 1)
            {
                return null;
            }

            var argFirst = type.TypeArguments.FirstOrDefault();

            return argFirst?.ToString();
        }

        public static bool IsGuidType(this IPropertySymbol ocs)
        {
            if (ocs.Type is not INamedTypeSymbol type)
            {
                return false;
            }

            if (!type.IsGenericType || type.TypeArguments.Length != 1)
            {
                return type.Name.EndsWith("String");
            }

            var argFirst = type.TypeArguments.FirstOrDefault();

            if (argFirst is not INamedTypeSymbol genericType)
            {
                return false;
            }

            return genericType.Name.EndsWith("String");
        }

        public static string? GetBasicOrArrayType(this IPropertySymbol ocs)
        {
            if (ocs.Type is IArrayTypeSymbol type)
            {
                return type.ElementType.ToString();
            }

            return ocs.Type.ToString();
        }
    }
}
