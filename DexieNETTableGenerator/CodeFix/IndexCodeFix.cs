/*
IndexCodeFix.cs

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
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace DNTGenerator.CodeFix
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IndexCodeFix)), Shared]
    internal class IndexCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(GeneratorDiagnostic.AutoIncrementNotNullable.Id,
                    GeneratorDiagnostic.AutoIncrementNotAllowedType.Id,
                    GeneratorDiagnostic.MultiEntryNotIEnumerable.Id,
                    GeneratorDiagnostic.NonMultiEntryNotArray.Id,
                    GeneratorDiagnostic.DuplicatePrimaryKeyMember.Id,
                    GeneratorDiagnostic.MissingIndexConverter.Id,
                    GeneratorDiagnostic.ReservedPrimaryKeyNameMember.Id);
            }
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/main/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            if (!FixableDiagnosticIds.Contains(diagnostic.Id))
            {
                return;
            }

            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            if (root is null)
            {
                throw new ArgumentNullException(nameof(root), nameof(root));
            }

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            string? codeFixMessage = null;

            var node = root.FindToken(diagnosticSpan.Start).Parent;
            if (node is null)
            {
                throw new ArgumentNullException(nameof(node), nameof(node));
            }

            TypeSyntax? typeSyntax = null;

            if (!diagnostic.Descriptor.EqualsId(GeneratorDiagnostic.MissingIndexConverter))
            {
                var name = node.IdentifierName();
                if (name is null)
                {
                    throw new ArgumentNullException(nameof(name), nameof(name));
                }

                switch (diagnostic.Descriptor)
                {
                    case var _ when diagnostic.Descriptor.EqualsId(GeneratorDiagnostic.DuplicatePrimaryKeyMember):

                        var typeDeclarationSyntax = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();

                        if (typeDeclarationSyntax is null)
                        {
                            throw new ArgumentNullException(nameof(typeDeclarationSyntax), nameof(typeDeclarationSyntax));
                        }

                        codeFixMessage = diagnostic.Descriptor.CodeFixMessage(name);
                        if (string.IsNullOrEmpty(codeFixMessage))
                        {
                            throw new ArgumentException("No title for: " + diagnostic.Descriptor.Id);
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: codeFixMessage,
                                createChangedDocument: _ => Task.FromResult(context.Document.RemoveType(root, typeDeclarationSyntax, node)),
                                equivalenceKey: diagnostic.Descriptor.Id),
                            diagnostic);

                        break;
                    case var _ when diagnostic.Descriptor.EqualsId(GeneratorDiagnostic.ReservedPrimaryKeyNameMember):

                        var parameter = diagnostic.CodeFixProperty("UniqueNonIDName");
                        if (parameter is null)
                        {
                            throw new ArgumentException("No parameter for: " + diagnostic.Descriptor.Id);
                        }

                        codeFixMessage = diagnostic.Descriptor.CodeFixMessage(parameter);
                        if (string.IsNullOrEmpty(codeFixMessage))
                        {
                            throw new ArgumentException("No title for: " + diagnostic.Descriptor.Id);
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: codeFixMessage,
                                createChangedDocument: _ => Task.FromResult(context.Document.RenameMember(root, node, parameter)),
                                equivalenceKey: diagnostic.Descriptor.Id),
                            diagnostic);

                        break;
                    case var _ when diagnostic.Descriptor.EqualsId(GeneratorDiagnostic.AutoIncrementNotNullable):

                        // index must be a type
                        typeSyntax = node.DescendantNodesAndSelf().OfType<TypeSyntax>().Last();

                        if (typeSyntax is null)
                        {
                            throw new ArgumentNullException(nameof(typeSyntax), nameof(typeSyntax));
                        }

                        codeFixMessage = diagnostic.Descriptor.CodeFixMessage(name);
                        if (string.IsNullOrEmpty(codeFixMessage))
                        {
                            throw new ArgumentException("No title for: " + diagnostic.Descriptor.Id);
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: codeFixMessage,
                                createChangedDocument: _ => Task.FromResult(context.Document.MakeNullable(root, typeSyntax)),
                                equivalenceKey: diagnostic.Descriptor.Id),
                            diagnostic);

                        break;
                    case var _ when diagnostic.Descriptor.EqualsId(GeneratorDiagnostic.AutoIncrementNotAllowedType):

                        var codeFixMessages = diagnostic.Descriptor.CodeFixMessages(name);

                        if (codeFixMessages.Count() != 5)
                        {
                            throw new ArgumentException("No titles for: " + diagnostic.Descriptor.Id);
                        }

                        var inlineActions = ImmutableArray.Create(CodeAction.Create(
                                title: codeFixMessages.ElementAt(1),
                                createChangedDocument: _ => Task.FromResult(context.Document.MakeUShort(root, node)),
                                equivalenceKey: codeFixMessages.ElementAt(1)),
                                CodeAction.Create(
                                title: codeFixMessages.ElementAt(2),
                                createChangedDocument: _ => Task.FromResult(context.Document.MakeUInt(root, node)),
                                equivalenceKey: codeFixMessages.ElementAt(2)),
                                CodeAction.Create(
                                title: codeFixMessages.ElementAt(3),
                                createChangedDocument: _ => Task.FromResult(context.Document.MakeULong(root, node)),
                                equivalenceKey: codeFixMessages.ElementAt(3)),
                                CodeAction.Create(
                                title: codeFixMessages.ElementAt(4),
                                createChangedDocument: _ => Task.FromResult(context.Document.MakeString(root, node)),
                                equivalenceKey: codeFixMessages.ElementAt(4)));

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: codeFixMessages.ElementAt(0),
                                nestedActions: inlineActions,
                                isInlinable: true), diagnostic);

                        break;
                    case var _ when diagnostic.Descriptor.EqualsId(GeneratorDiagnostic.MultiEntryNotIEnumerable):
                    case var _ when diagnostic.Descriptor.EqualsId(GeneratorDiagnostic.NonMultiEntryNotArray):

                        var typeParameter = diagnostic.CodeFixProperty("Type");
                        if (typeParameter is null)
                        {
                            throw new ArgumentNullException(nameof(typeParameter), nameof(typeParameter));
                        }

                        codeFixMessage = diagnostic.Descriptor.CodeFixMessage(name, typeParameter);
                        if (string.IsNullOrEmpty(codeFixMessage))
                        {
                            throw new ArgumentException("No title for: " + diagnostic.Descriptor.Id);
                        }

                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: codeFixMessage,
                                createChangedDocument: _ => Task.FromResult(context.Document.ReplaceType(root, node, typeParameter)),
                                equivalenceKey: diagnostic.Descriptor.Id),
                            diagnostic);

                        break;
                }
            }
            else
            {
                var popertyOrParameter = (node.Ancestors()
                    .Where(s => s.Kind() is SyntaxKind.PropertyDeclaration || s.Kind() is SyntaxKind.Parameter)
                    .FirstOrDefault());

                if (popertyOrParameter is null)
                {
                    throw new ArgumentNullException(nameof(popertyOrParameter), nameof(popertyOrParameter));
                }

                var name = popertyOrParameter.Kind() switch
                {
                    SyntaxKind.Parameter => ((ParameterSyntax)popertyOrParameter).Identifier.ValueText,
                    SyntaxKind.PropertyDeclaration => ((PropertyDeclarationSyntax)popertyOrParameter).Identifier.ValueText,
                    _ => null,
                };

                if (node is not IdentifierNameSyntax identifierNameSyntax)
                {
                    throw new ArgumentNullException(nameof(identifierNameSyntax), nameof(identifierNameSyntax));
                }

                var parameter = diagnostic.CodeFixProperty("Attribute");
                if (parameter is null)
                {
                    throw new ArgumentException("No parameter for: " + diagnostic.Descriptor.Id);
                }

                codeFixMessage = diagnostic.Descriptor.CodeFixMessage(parameter);
                if (string.IsNullOrEmpty(codeFixMessage))
                {
                    throw new ArgumentException("No title for: " + diagnostic.Descriptor.Id);
                }

                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: codeFixMessage,
                        createChangedDocument: _ => Task.FromResult(context.Document.ChangeIndexConverter(root, identifierNameSyntax, parameter)),
                        equivalenceKey: diagnostic.Descriptor.Id),
                    diagnostic);
            }
        }
    }
}
