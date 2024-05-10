/*
AttributeCodeFix.cs

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

using DNTGenerator.Diagnostics;
using DNTGenerator.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;

namespace DNTGenerator.CodeFix
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AttributeArgumentCodeFix)), Shared]
    internal class AttributeArgumentCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(GeneratorDiagnostic.DuplicatePrimaryKeySchemaArgument.Id,
                    GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id,
                    GeneratorDiagnostic.AutoWithoutPrimaryKeyArgument.Id,
                    GeneratorDiagnostic.ReservedGeneratedPKNameSchemaArgument.Id,
                    GeneratorDiagnostic.ReservedGeneratedPKGuidSchemaArgument.Id,
                    GeneratorDiagnostic.ReservedPrimaryKeyNameSchemaArgument.Id);
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

            var argumentSyntax = node.FirstAncestorOrSelf<AttributeArgumentSyntax>();

            if (argumentSyntax is null)
            {
                throw new ArgumentNullException(nameof(argumentSyntax), nameof(argumentSyntax));
            }

            var argumentListDeclaration = node.FirstAncestorOrSelf<AttributeArgumentListSyntax>();

            switch (diagnostic.Descriptor)
            {
                case var _ when diagnostic.Descriptor.EqualsId(GeneratorDiagnostic.ReservedPrimaryKeyNameSchemaArgument):

                    var parameter = diagnostic.CodeFixProperty("UniqueNonIDName") ?? throw new ArgumentException("No parameter for: " + diagnostic.Descriptor.Id);
                    codeFixMessage = diagnostic.Descriptor.CodeFixMessage(parameter);
                    if (string.IsNullOrEmpty(codeFixMessage))
                    {
                        throw new ArgumentException("No title for: " + diagnostic.Descriptor.Id);
                    }

                    context.RegisterCodeFix(
                       CodeAction.Create(
                           title: codeFixMessage,
                           createChangedDocument: _ => Task.FromResult(context.Document.RenameAttributeArgument(root, argumentSyntax, parameter)),
                           equivalenceKey: diagnostic.Descriptor.Id),
                       diagnostic);

                    break;
                case var _ when diagnostic.Descriptor.EqualsId(GeneratorDiagnostic.AutoWithoutPrimaryKeyArgument):

                    if (argumentListDeclaration is null)
                    {
                        throw new ArgumentNullException(nameof(argumentListDeclaration), nameof(argumentListDeclaration));
                    }

                    codeFixMessage = diagnostic.Descriptor.CodeFixMessage("Argument");
                    if (string.IsNullOrEmpty(codeFixMessage))
                    {
                        throw new ArgumentException("No title for: " + diagnostic.Descriptor.Id);
                    }

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: codeFixMessage,
                            createChangedDocument: _ => Task.FromResult(context.Document.AddAttributeArgument(root, argumentListDeclaration, "IsPrimary = true")),
                            equivalenceKey: diagnostic.Descriptor.Id),
                        diagnostic);

                    break;
                default:

                    if (argumentListDeclaration is null)
                    {
                        throw new ArgumentNullException(nameof(argumentListDeclaration), nameof(argumentListDeclaration));
                    }

                    codeFixMessage = diagnostic.Descriptor.CodeFixMessage("Argument");
                    if (string.IsNullOrEmpty(codeFixMessage))
                    {
                        throw new ArgumentException("No title for: " + diagnostic.Descriptor.Id);
                    }

                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: codeFixMessage,
                            createChangedDocument: _ => Task.FromResult(context.Document.RemoveAttributeArgument(root, argumentListDeclaration, argumentSyntax)),
                            equivalenceKey: diagnostic.Descriptor.Id),
                        diagnostic);
                    break;
            }
        }
    }
}
