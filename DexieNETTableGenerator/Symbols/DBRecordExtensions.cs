﻿/*
DBRecordExtensions.cs

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
using DNTGenerator.Matcher;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Threading;

namespace DNTGenerator.Verifier
{
    internal static class DBRecordExtensions
    {
        public static string DexieNETNamespace = "DexieNET";
        public static string IndexAttributeName = DexieNETNamespace + "." + "IndexAttribute";
        public static string SchemaAttributeName = DexieNETNamespace + "." + "SchemaAttribute";

        public record IndexDescriptor(IPropertySymbol Symbol, string Name, string TypeName, Location AttributeLocation, Location PKLocation, string? IndexConverter, bool IsIndex, bool IsPrimary = false, bool IsAuto = false, bool IsUniqe = false, bool IsMultiEntry = false);
        public class IndexDescriptorComparer : IComparer<IndexDescriptor>
        {
            public int Compare(IndexDescriptor x, IndexDescriptor y)
            {
                if (x.IsPrimary && !y.IsPrimary)
                {
                    return -1;
                }

                if (!x.IsPrimary && y.IsPrimary)
                {
                    return 1;
                }

                return 0;
            }
        }

        public record struct SchemaDescriptor(string StoreName, string? PrimaryKeyName, Location PKNameLocation, bool HasOutboundPrimaryKey, bool HasExplicitStoreName, Location Location);

        public static SchemaDescriptor GetSchemaDescriptor(this INamedTypeSymbol symbol, Compilation compilation, CancellationToken cancellationToken)
        {
            var attr = symbol.GetAttributes().Where(a => a.AttributeClass.MatchSchemaAttribute(compilation)).FirstOrDefault();

            if (attr is null)
            {
                return new(symbol.Name.ToLowerInvariant(), null, Location.None, false, false, Location.None);
            }

            var node = attr.ApplicationSyntaxReference?.GetSyntax(cancellationToken);
            if (node is null)
            {
                throw new InvalidOperationException($"Invalid Schema name for: {nameof(symbol)}");
            }
            var arguments = node.DescendantNodesAndSelf().OfType<AttributeArgumentSyntax>();

            var storeName = (string?)attr.NamedArguments.Where(na => na.Key == "StoreName").FirstOrDefault().Value.Value;

            string? primaryKeyName = null;
            Location? pkNameLocation = null;

            if (attr.NamedArguments.Where(na => na.Key == "PrimaryKeyName").Any())
            {
                primaryKeyName = (string?)attr.NamedArguments.Where(na => na.Key == "PrimaryKeyName").FirstOrDefault().Value.Value;
                pkNameLocation = arguments.Where(a => (string?)(a?.NameEquals?.Name?.Identifier.Value) == "PrimaryKeyName").FirstOrDefault().GetLocation();
            }

            var outboundPrimaryKey = ((bool?)attr.NamedArguments.Where(na => na.Key == "OutboundPrimaryKey").FirstOrDefault().Value.Value).True();

            var store = storeName ?? symbol.Name;

            var location = attr.ApplicationSyntaxReference is null ? Location.None : Location.Create(attr.ApplicationSyntaxReference.SyntaxTree, attr.ApplicationSyntaxReference.Span);
            return new(store.ToLowerInvariant(), primaryKeyName, pkNameLocation ?? Location.None, outboundPrimaryKey, storeName is not null, location);
        }

        public static IEnumerable<(IEnumerable<(string Name, Location Location)> Keys, bool IsPrimary, Location PKLocation)> GetCompoundKeys(this INamedTypeSymbol symbol, Compilation compilation, CancellationToken cancellationToken)
        {
            var attrs = symbol.GetAttributes().Where(a => a.AttributeClass.MatchCompoundIndexAttribute(compilation));

            List<(IEnumerable<(string Name, Location Location)>, bool, Location)> keys = new();

            foreach (var attr in attrs)
            {
                var node = attr.ApplicationSyntaxReference?.GetSyntax(cancellationToken);
                if (node is null)
                {
                    throw new InvalidOperationException($"Invalid CompoundKey name for: {nameof(symbol)}");
                }

                var arguments = node.DescendantNodesAndSelf().OfType<AttributeArgumentSyntax>();
                if (arguments.Count() < attr.ConstructorArguments.Length)
                {
                    throw new InvalidOperationException($"Invalid CompoundKey name for: {nameof(symbol)}");
                }

                IEnumerable<(string? Name, Location Location)> names = attr.ConstructorArguments.Select((a, i) => ((string?)a.Value, arguments.ElementAt(i).GetLocation()));

                if (names.Any(n => n.Name is null))
                {
                    throw new InvalidOperationException($"Invalid CompoundKey name for: {nameof(symbol)}");
                }

                bool primaryKey = false;
                Location? pkLocation = null;

                if (attr.NamedArguments.Where(na => na.Key == "IsPrimary").Any())
                {
                    primaryKey = ((bool?)attr.NamedArguments.Where(na => na.Key == "IsPrimary").FirstOrDefault().Value.Value).True();
                    pkLocation = arguments.Where(a => (string?)(a?.NameEquals?.Name?.Identifier.Value) == "IsPrimary").FirstOrDefault().GetLocation();
                }

                IEnumerable<(string Name, Location Location)> keyNames = names.Where(n => n.Name is not null)
                        .Select(n => (n.Name!.ToLowerInvariant(), n.Location));

                keys.Add((keyNames, primaryKey, pkLocation ?? Location.None));
            }

            return keys;
        }

        public static bool HasGeneratedPrimaryKey(this DBRecord record)
        {
            return !record.SchemaDescriptor.HasOutboundPrimaryKey &&
                !record.Properties.Where(p => p.IsPrimary).Any() &&
                !record.CompoundKeys.Where(p => p.IsPrimary).Any();
        }

        public static bool HasGeneratedPrimaryKeys(this IEnumerable<DBRecord> records)
        {
            return records
                .Where(r => r.HasGeneratedPrimaryKey()).Any();
        }

        public static string? GetPrimaryIndexTypeName(this DBRecord record)
        {
            var primaryIndexTypeName = record.Properties
                .Where(i => i.IsPrimary)
                .Select(i => i.TypeName)
                .FirstOrDefault();

            if (primaryIndexTypeName is null)
            {
                var keyNames = record.CompoundKeys
                    .Where(c => c.IsPrimary)
                    .SelectMany(c => c.Keys);

                if (keyNames.Any())
                {
                    var typeNames = keyNames.SelectMany(k =>
                        record.Properties.Where(p => p.Name.ToLowerInvariant() == k.Name)).Select(i => i.TypeName);

                    primaryIndexTypeName = typeNames.Aggregate("(", (current, next) => current + $"{next}, ");
                    primaryIndexTypeName = primaryIndexTypeName.TrimEnd(new[] { ',', ' ' });
                    primaryIndexTypeName += ")";
                }
            }

            return primaryIndexTypeName;
        }

        public static string? GetPrimaryIndexName(this DBRecord record, bool forSchema)
        {
            var primaryIndexName = record.Properties
                .Where(i => i.IsPrimary)
                .Select(i => i.Name)
                .FirstOrDefault();

            if (primaryIndexName is null && !record.SchemaDescriptor.HasOutboundPrimaryKey)
            {
                primaryIndexName = record.GetCompoundPrimaryKey();

                if (primaryIndexName is null)
                {
                    if (record.SchemaDescriptor.PrimaryKeyName is null)
                    {
                        primaryIndexName ??= forSchema ? "++ID" : "ID";
                    }
                    else
                    {
                        primaryIndexName ??= forSchema ? $"++{record.SchemaDescriptor.PrimaryKeyName}" : record.SchemaDescriptor.PrimaryKeyName;
                    }
                }
            }

            return forSchema ? primaryIndexName?.ToLowerInvariant() : primaryIndexName;
        }

        public static (string StoreName, string Schema, bool Update) GetSchema(this DBRecord record)
        {
            var comparer = new IndexDescriptorComparer();

            var keys = record.Properties.Where(i => i.IsIndex)
                .OrderBy(static i => i, comparer);

            var keyNames = keys.Select(i =>
            {
                var name = i.Name.ToLowerInvariant();

                if (i.IsAuto)
                {
                    return "++" + name;
                }

                if (i.IsUniqe)
                {
                    return "&" + name;
                }

                if (i.IsMultiEntry)
                {
                    return "*" + name;
                }

                return name;
            }).ToList();

            keyNames.AddRange(record.GetCompoundKeys(true));

            var schema = keyNames.Aggregate(string.Empty, (current, next) => current + $"{next}, ");

            if (!record.Properties.Any(i => i.IsPrimary))
            {
                if (!record.SchemaDescriptor.HasOutboundPrimaryKey)
                {
                    schema = record.GetPrimaryIndexName(true) + ", " + schema;
                }
                else
                {
                    schema = ", " + schema;
                }
            }

            schema = schema.TrimEnd(new[] { ',', ' ' });

            return new(record.SchemaDescriptor.StoreName, schema, record.SchemaDescriptor.HasExplicitStoreName);
        }

        public static string Keys(this DBRecord record, bool multiEntryOnly)
        {
            var comparer = new IndexDescriptorComparer();

            var keys = multiEntryOnly ?
                record.Properties.Where(i => i.IsIndex && i.IsMultiEntry)
                    .OrderBy(static i => i, comparer)
                    .Select(i => i.Name.ToLowerInvariant()).ToList()
                :
                record.Properties.Where(i => i.IsIndex)
                    .OrderBy(static i => i, comparer)
                    .Select(i => i.Name.ToLowerInvariant()).ToList();

            if (!multiEntryOnly)
            {
                keys.AddRange(record.GetCompoundKeys(false));
            }

            var keyNames = keys.Distinct().Aggregate(string.Empty, (current, next) => current + $@"""{next}"", ");

            keyNames = keyNames.TrimEnd(new[] { ',', ' ' });

            if (!multiEntryOnly && !record.Properties.Any(i => i.IsPrimary))
            {
                if (!record.SchemaDescriptor.HasOutboundPrimaryKey)
                {
                    keyNames = @$"""{record.GetPrimaryIndexName(false)?.ToLowerInvariant()}""" + ", " + keyNames;
                }
            }

            keyNames = keyNames.TrimEnd(new[] { ',', ' ' });

            return keyNames.Any() ? $"new string[] {{{keyNames}}}" : "Enumerable.Empty<string>().ToArray()";
        }

        public static IEnumerable<string> GetIndexConverters(this DBRecord record)
        {
            return record.Properties
                .Select(p => p.IndexConverter)
                .WhereNotNull();
        }


        public static IEnumerable<IndexDescriptor> GetIndexProperties(this INamedTypeSymbol typeSymbol, Compilation compilation, CancellationToken cancellationToken)
        {
            var indexCandidates = typeSymbol.GetMembers().Where(m => m.Kind is SymbolKind.Property)
                .Where(static p => !p.MatchSystem())
                .Select(p => ((IPropertySymbol)p).MakeIndexDescriptor(compilation, cancellationToken))
                .WhereNotNull()
                .SelectRecursive(id => GetIndexPropertiesDo(id, compilation, cancellationToken));

            return indexCandidates;
        }

        private static IEnumerable<IndexDescriptor> GetIndexPropertiesDo(IndexDescriptor index, Compilation compilation, CancellationToken cancellationToken)
        {
            if (index.IsIndex || index.Symbol.Type is not INamedTypeSymbol type ||
                index.Symbol.MatchSystem())
            {
                return Enumerable.Empty<IndexDescriptor>();
            }

            return type.GetMembers().Where(m => m.Kind is SymbolKind.Property)
                .Where(static p => !p.MatchSystem())
                .Select(p => ((IPropertySymbol)p).MakeIndexDescriptor(compilation, cancellationToken, index.Name))
                .WhereNotNull();
        }

        private static IndexDescriptor? MakeIndexDescriptor(this IPropertySymbol symbol, Compilation compilation, CancellationToken cancellationToken, string? parentName = null)
        {
            var name = parentName is null ? symbol.Name : parentName + "." + symbol.Name;
            var typeName = symbol.Type.ToDisplayString().TrimEnd('?');
            var attr = symbol.GetAttributes().Where(a => a.AttributeClass.MatchIndexAttribute(compilation)).FirstOrDefault();

            if (attr is null)
            {
                var generated = symbol.GetAttributes().Where(a => a.AttributeClass.MatchGeneratedCodeAttribute(compilation)).Any();
                return generated ? null : new(symbol, name, typeName, Location.None, Location.None, null, false);
            }

            var node = attr.ApplicationSyntaxReference?.GetSyntax(cancellationToken);
            if (node is null)
            {
                throw new InvalidOperationException($"Invalid CompoundKey name for: {nameof(symbol)}");
            }

            var arguments = node.DescendantNodesAndSelf().OfType<AttributeArgumentSyntax>();

            var isPrimary = ((bool?)attr.NamedArguments.Where(na => na.Key == "IsPrimary").FirstOrDefault().Value.Value).True();
            var pkLocation = arguments.Where(a => (string?)(a?.NameEquals?.Name?.Identifier.Value) == "IsPrimary").FirstOrDefault()?.GetLocation();

            var isAuto = ((bool?)attr.NamedArguments.Where(na => na.Key == "IsAuto").FirstOrDefault().Value.Value).True();
            var isUnique = ((bool?)attr.NamedArguments.Where(na => na.Key == "IsUnique").FirstOrDefault().Value.Value).True();
            var isMultiEntry = ((bool?)attr.NamedArguments.Where(na => na.Key == "IsMultiEntry").FirstOrDefault().Value.Value).True();
            var indexConverter = symbol.GetIndexConverter(compilation);

            var attrLocation = attr.ApplicationSyntaxReference is null ? Location.None : Location.Create(attr.ApplicationSyntaxReference.SyntaxTree, attr.ApplicationSyntaxReference.Span);
            var id = new IndexDescriptor(symbol, name, typeName, attrLocation, pkLocation ?? Location.None, indexConverter, true, isPrimary, isAuto, isUnique, isMultiEntry);

            return id;
        }

        private static string? GetIndexConverter(this IPropertySymbol symbol, Compilation compilation)
        {
            var attr = symbol.GetAttributes().Where(a => a.AttributeClass.MatchIndexConverterAttribute(compilation)).FirstOrDefault();
            return attr?.AttributeClass?.Name;
        }

        private static string? GetCompoundPrimaryKey(this DBRecord record)
        {
            string? primaryIndexName = null;

            if (record.CompoundKeys.Any(c => c.IsPrimary))
            {
                primaryIndexName = record.CompoundKeys.Where(c => c.IsPrimary).First().Keys.GetCompoundKey();
            }

            return primaryIndexName;
        }

        private static IEnumerable<string> GetCompoundKeys(this DBRecord record, bool forSchema)
        {
            var combinedKeys = record.CompoundKeys.Where(c => !c.IsPrimary).Select(c => c.Keys.GetCompoundKey());
            var virtualKeys = record.CompoundKeys.Select(c => c.Keys.First().Name);

            return forSchema ? combinedKeys : combinedKeys.Concat(virtualKeys);
        }

        private static string GetCompoundKey(this IEnumerable<(string Name, Location Location)> keys)
        {
            var key = keys.Select(k => k.Name).Aggregate("[", (current, next) => current + next + "+");
            key = key.TrimEnd('+');
            key += "]";
            return key;
        }
    }
}