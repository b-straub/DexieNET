/*
SymbolMatcher.cs

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
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace DNTGenerator.Matcher
{
    internal static class SymbolMatcher
    {
        public static string DexieNETNamespace = "DexieNET";
        public static string IIndexAttributeName = DexieNETNamespace + "." + "IIndexAttribute";
        public static string GeneratedCodeAttribute = "System.CodeDom.Compiler.GeneratedCodeAttribute";
        public static string IndexConverterAttribute = DexieNETNamespace + "." + "IndexConverterAttribute`3";
        public static string SchemaAttributeName = DexieNETNamespace + "." + "SchemaAttribute";
        public static string CompoundIndexAttributeName = DexieNETNamespace + "." + "CompoundIndexAttribute";
        public static string DBNameAttributeName = DexieNETNamespace + "." + "DBNameAttribute";
        public static string DBAddPushSupportPushAttribute = DexieNETNamespace + "." + "DBAddPushSupportAttribute";

        public static bool MatchDeclaration(this SyntaxNode node)
        {
            if (node is TypeDeclarationSyntax syntax)
            {
                return syntax.BaseList is not null;
            }

            return false;
        }

        public static TypeDeclarationSyntax? MatchInterface(this GeneratorSyntaxContext context)
        {
            var typeDeclaration = (TypeDeclarationSyntax)context.Node;
            var interfaces = typeDeclaration?.BaseList?.DescendantNodesAndSelf().OfType<IdentifierNameSyntax>();
            bool implementInterface = interfaces.Any();

            return implementInterface ? typeDeclaration : null;
        }

        public static bool MatchIndexAttribute([NotNullWhen(true)] this INamedTypeSymbol? symbol, Compilation compilation)
        {
            return MatchType(symbol, IIndexAttributeName, compilation);
        }

        public static bool MatchGeneratedCodeAttribute([NotNullWhen(true)] this INamedTypeSymbol? symbol, Compilation compilation)
        {
            return MatchType(symbol, GeneratedCodeAttribute, compilation);
        }

        public static bool MatchIndexAttribute([NotNullWhen(true)] this AttributeSyntax? attributeSyntax, SyntaxNodeAnalysisContext context)
        {
            return MatchSyntax(attributeSyntax, IIndexAttributeName, context);
        }

        public static bool MatchIndexConverterAttribute([NotNullWhen(true)] this INamedTypeSymbol? symbol, Compilation compilation)
        {
            return MatchType(symbol, IndexConverterAttribute, compilation);
        }

        public static bool MatchIndexConverterAttribute([NotNullWhen(true)] this AttributeSyntax? attributeSyntax, SyntaxNodeAnalysisContext context)
        {
            return MatchSyntax(attributeSyntax, IndexConverterAttribute, context);
        }

        public static bool MatchSchemaAttribute([NotNullWhen(true)] this INamedTypeSymbol? symbol, Compilation compilation)
        {
            return MatchType(symbol, SchemaAttributeName, compilation);
        }

        public static bool MatchCompoundIndexAttribute([NotNullWhen(true)] this INamedTypeSymbol? symbol, Compilation compilation)
        {
            return MatchType(symbol, CompoundIndexAttributeName, compilation);
        }

        public static bool MatchDBNameAttribute([NotNullWhen(true)] this INamedTypeSymbol? symbol, Compilation compilation)
        {
            return MatchType(symbol, DBNameAttributeName, compilation);
        }
        
        public static bool MatchDBAddPushSupportAttribute([NotNullWhen(true)] this INamedTypeSymbol? symbol, Compilation compilation)
        {
            return MatchType(symbol, DBAddPushSupportPushAttribute, compilation);
        }

        public static bool MatchSystem([NotNullWhen(true)] this ISymbol? symbol)
        {
            return (symbol?.ContainingModule.Name.StartsWith("System")).True() ||
                (symbol?.Name.Equals("EqualityContract")).True();
        }

        public static bool MatchType([NotNullWhen(true)] this INamedTypeSymbol? symbol, string constructedFromName, Compilation compilation)
        {
            if (symbol is null)
            {
                return false;
            }

            INamedTypeSymbol? constructedFromSymbol = compilation.GetTypeByMetadataName(constructedFromName);

            bool matchType = symbol.ConstructedFrom.Equals(constructedFromSymbol, SymbolEqualityComparer.Default);
            bool matchBase = (symbol.BaseType?.ConstructedFrom.Equals(constructedFromSymbol, SymbolEqualityComparer.Default)).True();
            bool matchInterface = symbol.AllInterfaces.Where(i => i.ConstructedFrom.Equals(constructedFromSymbol, SymbolEqualityComparer.Default)).Any();

            return matchType || matchBase || matchInterface;
        }

        private static bool MatchSyntax([NotNullWhen(true)] this SyntaxNode? node, string constructedFromName, SyntaxNodeAnalysisContext context)
        {
            if (node is null)
            {
                return false;
            }

            INamedTypeSymbol? constructedFromSymbol = context.Compilation.GetTypeByMetadataName(constructedFromName);

            if (context.SemanticModel.GetSymbolInfo(node, context.CancellationToken).Symbol?.ContainingSymbol is not INamedTypeSymbol symbol)
            {
                return false;
            }

            bool matchType = symbol.ConstructedFrom.Equals(constructedFromSymbol, SymbolEqualityComparer.Default);
            bool matchBase = (symbol.BaseType?.ConstructedFrom.Equals(constructedFromSymbol, SymbolEqualityComparer.Default)).True();
            bool matchInterface = symbol.AllInterfaces.Where(i => i.ConstructedFrom.Equals(constructedFromSymbol, SymbolEqualityComparer.Default)).Any();

            return matchType || matchBase || matchInterface;
        }
    }
}