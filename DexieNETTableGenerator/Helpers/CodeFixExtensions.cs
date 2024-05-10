/*
CodeFixExtensions.cs

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

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace DNTGenerator.Helpers
{
    internal static class GeneratorDiagnosticExtensions
    {
        public static bool EqualsId(this DiagnosticDescriptor source, DiagnosticDescriptor other)
        {
            return source.Id == other.Id;
        }

        public static bool HasError(this IEnumerable<Diagnostic> source)
        {
            return source.Any(d => d.Severity is DiagnosticSeverity.Error);
        }

        public static string CodeFixMessage(this DiagnosticDescriptor source, params string[] parameters)
        {
            var message = source.CustomTags.Any() ? source.CustomTags.ElementAt(0) : string.Empty;
            if (parameters.Any())
            {
                message = string.Format(message, parameters);
            }

            return message;
        }

        public static string? CodeFixProperty(this Diagnostic source, string property)
        {
            if (source.Properties.TryGetValue(property, out string? value))
            {
                return value;
            }

            return null;
        }

        public static IEnumerable<string> CodeFixMessages(this DiagnosticDescriptor source, string? name = null)
        {
            var messages = source.CustomTags.Any() ? source.CustomTags : Enumerable.Empty<string>();

            if (name is not null)
            {
                messages = messages.Select(m => string.Format(m, name));
            }

            return messages;
        }

        public static TypeDeclarationSyntax WithAdditionalModifier(this TypeDeclarationSyntax syntax, SyntaxKind modifier)
        {
            var modifierToken = SyntaxFactory.Token(modifier);

            var modifiers = syntax.Modifiers;

            if (modifier is not SyntaxKind.PartialKeyword)
            {
                if (modifier is SyntaxKind.PublicKeyword)
                {
                    modifiers = modifiers.Insert(0, modifierToken);
                }
                else
                {
                    modifiers = modifiers.InsertAfter(modifierToken, SyntaxKind.PublicKeyword);
                }
            }
            else
            {
                modifiers = modifiers.Add(modifierToken.WithTrailingTrivia(SyntaxFactory.Space));
            }

            return syntax.WithModifiers(modifiers);
        }

        public static SyntaxTokenList InsertAfter(this SyntaxTokenList source, SyntaxToken item, SyntaxKind key)
        {
            var i = source.Select((Value, Index) => new { Value, Index }).FirstOrDefault(x => x.Value.IsKind(key));

            if (i == null)
            {
                return source.Insert(0, item);
            }
            else if (i.Index + 1 >= source.Count)
            {
                return source.Add(item);
            }
            else
            {
                return source.Insert(i.Index + 1, item);
            }
        }

        public static string? IdentifierName(this SyntaxNode node)
        {
            var name = node.Kind() switch
            {
                SyntaxKind.Parameter => ((ParameterSyntax)node).Identifier.ValueText,
                SyntaxKind.PropertyDeclaration => ((PropertyDeclarationSyntax)node).Identifier.ValueText,
                _ => null,
            };

            return name;
        }

        public static TypeSyntax? Type(this SyntaxNode node)
        {
            var type = node.Kind() switch
            {
                SyntaxKind.Parameter => ((ParameterSyntax)node).Type,
                SyntaxKind.PropertyDeclaration => ((PropertyDeclarationSyntax)node).Type,
                _ => null,
            };

            return type;
        }
    }
}
