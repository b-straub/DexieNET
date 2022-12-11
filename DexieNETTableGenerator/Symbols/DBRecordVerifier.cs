/*
DBRecordVerifier.cs

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

using DNTGenerator.Diagnostics;
using DNTGenerator.Helpers;
using Microsoft.CodeAnalysis;
using System;

namespace DNTGenerator.Verifier
{
    internal static class DBRecordVerifier
    {
        public static IEnumerable<GeneratorDiagnostic> Verify(this DBRecord dBRecord, Compilation compilation)
        {
            List<GeneratorDiagnostic> diagnostics = new();

            var hasPrimary = dBRecord.Properties.Where(i => i.IsPrimary).Any() ||
                dBRecord.CompoundKeys.Where(c => c.IsPrimary).Any();

            if (!hasPrimary && !dBRecord.SchemaDescriptor.HasOutboundPrimaryKey && !dBRecord.IsPartial)
            {
                diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.NotPartial, dBRecord));
            }

            var duplicatePKIndexes = dBRecord.Properties
                .Where(i => !i.IsPrimary && i.Name.ToLowerInvariant() == dBRecord.SchemaDescriptor.PrimaryKeyName?.ToLowerInvariant() &&
                    i.Name.ToLowerInvariant() != "id");

            if (duplicatePKIndexes.Any())
            {
                foreach (var dupIndex in duplicatePKIndexes)
                {
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.DuplicatePrimaryKeySchemaArgument, dBRecord.SchemaDescriptor.PKNameLocation, dupIndex.Name));
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.DuplicatePrimaryKeyMember, dupIndex));
                }
            }

            if (dBRecord.SchemaDescriptor.PrimaryKeyName?.ToLowerInvariant() == "id")
            {
                diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.ReservedPrimaryKeyNameSchemaArgument, dBRecord.SchemaDescriptor.PKNameLocation,
                    new KeyValuePair<string, string?>("UniqueNonIDName", dBRecord.GetUniqueNonIDName())));
            }

            if (!hasPrimary && !dBRecord.SchemaDescriptor.HasOutboundPrimaryKey && dBRecord.SchemaDescriptor.PrimaryKeyName is null)
            {
                duplicatePKIndexes = dBRecord.Properties
                    .Where(i => i.Name.ToLowerInvariant() == "id");

                foreach (var dupIndex in duplicatePKIndexes)
                {
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.ReservedPrimaryKeyNameMember, dupIndex.Symbol.Locations.First(),
                        new KeyValuePair<string, string?>("UniqueNonIDName", dBRecord.GetUniqueNonIDName())));
                }
            }

            var primaryKeysIndex = dBRecord.Properties
               .Where(i => i.IsPrimary).Select(i => (i.Name, i.PKLocation));

            var primaryKeysCompound = dBRecord.CompoundKeys
               .Where(i => i.IsPrimary).Select(c => ("CompoundIndex", c.PKLocation));

            if (primaryKeysIndex.Count() + primaryKeysCompound.Count() > 1)
            {
                foreach ((string Name, Location Location) in primaryKeysIndex)
                {
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.MultiplePrimaryKeysArgument, Location, Name));
                }

                foreach ((string Name, Location Location) in primaryKeysCompound)
                {
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.MultiplePrimaryKeysArgument, Location, Name));
                }
            }

            if (dBRecord.SchemaDescriptor.PrimaryKeyName is not null && primaryKeysCompound.Any())
            {
                diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.MultiplePrimaryKeysSchemaArgument, dBRecord.SchemaDescriptor.PKNameLocation, "Schema"));
            }

            var propertyNames = dBRecord.Properties.Select(p => p.Name.ToLowerInvariant());

            foreach (var (Keys, IsPrimary, Location) in dBRecord.CompoundKeys)
            {
                foreach (var key in Keys)
                {
                    if (!propertyNames.Contains(key.Name))
                    {
                        diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.CompoundIndexNotFound, key.Location, dBRecord.Symbol.Name));
                    }
                }
            }

            foreach (var index in dBRecord.Properties)
            {
                if (index.IsAuto)
                {
                    if (!index.Symbol.IsNumeric())
                    {
                        diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.AutoIncrementNotNumeric, index));
                    }
                    else if (index.Symbol.NullableAnnotation is not NullableAnnotation.Annotated)
                    {
                        diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.AutoIncrementNotNullable, index));
                    }
                }

                var indexConverter = GetIndexConverterAttributeName(index.Symbol.Type, compilation);

                if (indexConverter is not null && (index.IndexConverter is null || !indexConverter.StartsWith(index.IndexConverter)))
                {
                    indexConverter = indexConverter.TrimEnd("Attribute");
                    var diagnostic = new GeneratorDiagnostic(GeneratorDiagnostic.MissingIndexConverter, index.AttributeLocation, index.Name, indexConverter);
                    diagnostic.AddProperties(new KeyValuePair<string, string?>("Attribute", indexConverter));

                    diagnostics.Add(diagnostic);
                }
            }

            return diagnostics;
        }

        public static string? GetIndexConverterAttributeName(ITypeSymbol type, Compilation compilation)
        {
            var missingIndexConverter = type switch
            {
                var _ when type.Equals(compilation.GetTypeByMetadataName("System.Boolean"), SymbolEqualityComparer.Default) => "BoolIndexAttribute",
                var _ when type.Equals(compilation.CreateArrayTypeSymbol(compilation.GetTypeByMetadataName("System.Byte")!), SymbolEqualityComparer.Default) => "ByteIndexAttribute",
                _ => null
            };

            return missingIndexConverter;
        }

        private static bool IsNumeric(this IPropertySymbol symbol)
        {
            var typeName = symbol.Type.ToString();

            return typeName.StartsWith("short") ||
                typeName.StartsWith("ushort") ||
                typeName.StartsWith("int") ||
                typeName.StartsWith("uint") ||
                typeName.StartsWith("long") ||
                typeName.StartsWith("ulong");
        }

        public static string GetUniqueNonIDName(this DBRecord record)
        {
            var idProperties = record.Properties
                .Where(p => p.Name.ToLowerInvariant().StartsWith("id"))
                .OrderByDescending(p => p.Name.Length)
                .ThenBy(p => p.Name);

            return idProperties.Any() ? idProperties.First().Name + "1" : "ID1";
        }
    }
}
