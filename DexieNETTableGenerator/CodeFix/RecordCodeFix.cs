/*
AttributeCodeFix.cs

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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RecordCodeFix)), Shared]
    internal class RecordCodeFix : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds
        {
            get
            {
                return ImmutableArray.Create(GeneratorDiagnostic.NotPartial.Id, GeneratorDiagnostic.NotPartialAutoGuid.Id);
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

            // Find the class or record declaration
            if (root.FindToken(diagnosticSpan.Start).Parent is not TypeDeclarationSyntax declaration)
            {
                throw new ArgumentNullException(nameof(declaration), nameof(declaration));
            }

            var name = declaration.Identifier.ValueText;

            string? codeFixMessage = diagnostic.Descriptor.CodeFixMessage(name);
            if (string.IsNullOrEmpty(codeFixMessage))
            {
                throw new ArgumentException("No title for: " + diagnostic.Descriptor.Id);
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: codeFixMessage,
                    createChangedDocument: _ => Task.FromResult(MakePartial(context.Document, root, declaration)),
                    equivalenceKey: diagnostic.Descriptor.Id),
                diagnostic);
        }

        private static Document MakePartial(Document document, SyntaxNode root, TypeDeclarationSyntax typeDeclaration)
        {
            var newNode = typeDeclaration.WithAdditionalModifier(SyntaxKind.PartialKeyword);

            return document.WithSyntaxRoot(root.ReplaceNode(
                        typeDeclaration, newNode));
        }
    }
}
