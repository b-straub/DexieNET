/*
DBRecordAnalyzer.cs

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
using DNTGenerator.Query;
using DNTGenerator.Verifier;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace DNTGenerator.Analyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class DBRecordAnalyzer : DiagnosticAnalyzer
    {
        public static DiagnosticDescriptor[] Diagnostics =
        {
            GeneratorDiagnostic.NotPartial,
            GeneratorDiagnostic.NotPartialAutoGuid,
            GeneratorDiagnostic.NotPartialCloud,
            GeneratorDiagnostic.ReservedStoreName,
            GeneratorDiagnostic.AutoIncrementNotNullable,
            GeneratorDiagnostic.AutoIncrementNotAllowedType,
            GeneratorDiagnostic.AutoWithoutPrimaryKeyArgument,
            GeneratorDiagnostic.MultiEntryNotIEnumerable,
            GeneratorDiagnostic.NonMultiEntryNotArray,
            GeneratorDiagnostic.DuplicatePrimaryKeyMember,
            GeneratorDiagnostic.DuplicatePrimaryKeySchemaArgument,
            GeneratorDiagnostic.MultiplePrimaryKeysArgument,
            GeneratorDiagnostic.ReservedGeneratedPKNameSchemaArgument,
            GeneratorDiagnostic.ReservedGeneratedPKGuidSchemaArgument,
            GeneratorDiagnostic.ReservedPrimaryKeyNameMember,
            GeneratorDiagnostic.ReservedPrimaryKeyNameSchemaArgument,
            GeneratorDiagnostic.CompoundIndexNotFound,
            GeneratorDiagnostic.MissingIndexConverter
        };

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => Diagnostics.ToImmutableArray();

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.RegisterSyntaxNodeAction(AnalyzeDeclaration, SyntaxKind.RecordDeclaration, SyntaxKind.ClassDeclaration,
                SyntaxKind.RecordStructDeclaration, SyntaxKind.StructDeclaration);
        }

        private static void AnalyzeDeclaration(SyntaxNodeAnalysisContext context)
        {
            var td = context.Node as TypeDeclarationSyntax;

            if (td is not null)
            {
                var dbRecord = td.CreateDBRecord(context.Compilation, context.CancellationToken);

                if (dbRecord is not null)
                {
                    var diagnostics = dbRecord.Verify(context.Compilation);
                    foreach (var diagnostic in diagnostics)
                    {
                        diagnostic.ReportDiagnostic(context.ReportDiagnostic);
                    }
                }
            }
        }
    }
}
