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
using DNTGenerator.Query;
using Microsoft.CodeAnalysis;
using System;

namespace DNTGenerator.Verifier
{
    internal static class DBRecordVerifier
    {
        public static IEnumerable<GeneratorDiagnostic> Verify(this DBRecord dBRecord, Compilation compilation)
        {
            List<GeneratorDiagnostic> diagnostics = new();
            string[] reservedStoreNames = { "realms", "members", "roles" };

            bool missingPrimary = false;

            foreach (var index in dBRecord.Properties)
            {
                if (index.IsAuto && !index.IsPrimary)
                {
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.AutoWithoutPrimaryKeyArgument, index.PKLocation, index.Name));
                    missingPrimary = true;
                }
            }

            var hasPrimary = dBRecord.Properties.Where(i => i.IsPrimary).Any() ||
            dBRecord.CompoundKeys.Where(c => c.IsPrimary).Any();

            if (!missingPrimary && !hasPrimary && !dBRecord.SchemaDescriptor.HasOutboundPrimaryKey && !dBRecord.IsPartial)
            {
                diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.NotPartial, dBRecord));
            }
            else if (!dBRecord.IsPartial && dBRecord.Properties.Where(p => p.IsAutoGuidPrimary).Any())
            {
                diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.NotPartialAutoGuid, dBRecord));
            }
            else if (!dBRecord.IsPartial && dBRecord.SchemaDescriptor.HasCloudSync)
            {
                diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.NotPartialCloud, dBRecord));
            }
            else if (dBRecord.SchemaDescriptor.HasCloudSync && reservedStoreNames.Any(s => s == dBRecord.SchemaDescriptor.StoreName.ToLowerInvariant()))
            {
                if (dBRecord.SchemaDescriptor.StoreNameLocation is not null)
                {
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.ReservedStoreName, dBRecord.SchemaDescriptor.StoreNameLocation, dBRecord.SchemaDescriptor.StoreName));
                }
                else
                {
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.ReservedStoreName, dBRecord));
                }
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

            if (!missingPrimary && !hasPrimary && !dBRecord.SchemaDescriptor.HasOutboundPrimaryKey && dBRecord.SchemaDescriptor.PrimaryKeyName is null)
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

            if (primaryKeysIndex.Count() + primaryKeysCompound.Count() > 0)
            {
                if (dBRecord.SchemaDescriptor.PrimaryKeyName is not null && dBRecord.SchemaDescriptor.PKNameLocation != Location.None)
                {
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.ReservedGeneratedPKNameSchemaArgument, dBRecord.SchemaDescriptor.PKNameLocation, "Schema"));
                }

                if (dBRecord.SchemaDescriptor.PrimaryKeyGuid is not null && dBRecord.SchemaDescriptor.PKGuidLocation != Location.None)
                {
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.ReservedGeneratedPKGuidSchemaArgument, dBRecord.SchemaDescriptor.PKGuidLocation, "Schema"));
                }
            }

            var propertyNames = dBRecord.Properties.Select(p => p.Name.ToCamelCase());
            string[] cloudKeys = { "realmId", "owner" };

            foreach (var (Keys, IsPrimary, Location) in dBRecord.CompoundKeys)
            {
                foreach (var key in Keys)
                {
                    if (dBRecord.SchemaDescriptor.HasCloudSync)
                    {
                        if (!propertyNames.Contains(key.Name) && !cloudKeys.Contains(key.Name))
                        {
                            diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.CompoundIndexNotFound, key.Location, dBRecord.Symbol.Name));
                        }
                    }
                    else if (!propertyNames.Contains(key.Name))
                    {
                        diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.CompoundIndexNotFound, key.Location, dBRecord.Symbol.Name));
                    }
                }
            }

            foreach (var index in dBRecord.Properties)
            {
                if (!missingPrimary && index.IsAuto)
                {
                    if (!index.IsAutoGuidPrimary && !index.Symbol.IsNumeric())
                    {
                        diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.AutoIncrementNotAllowedType, index));
                    }
                    else if (index.Symbol.NullableAnnotation is not NullableAnnotation.Annotated)
                    {
                        diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.AutoIncrementNotNullable, index));
                    }
                }

                if (index.IsMultiEntry && !index.Symbol.IsEnumerable(compilation))
                {
                    var meType = new KeyValuePair<string, string?>("Type", $"IEnumerable<{index.Symbol.GetBasicOrArrayType()}>");
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.MultiEntryNotIEnumerable, index, meType));
                }

                if (!index.IsMultiEntry && index.Symbol.IsEnumerable(compilation))
                {
                    var arType = new KeyValuePair<string, string?>("Type", $"{index.Symbol.GetGenericType()}[]");
                    diagnostics.Add(new GeneratorDiagnostic(GeneratorDiagnostic.NonMultiEntryNotArray, index, arType));
                }

                var indexConverter = GetIndexConverterAttributeName(index.Symbol.Type, compilation);

                if (indexConverter is not null && index.IsIndex && (index.IndexConverter is null || !indexConverter.StartsWith(index.IndexConverter)))
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

        private static bool IsEnumerable(this IPropertySymbol ocs, Compilation compilation)
        {
            var typeName = ocs.Type.ToString();
            return typeName.Contains("IEnumerable<");
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
