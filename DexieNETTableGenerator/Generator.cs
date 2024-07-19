﻿/*
Generator.cs

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

using DNTGenerator.Analyzer;
using DNTGenerator.Diagnostics;
using DNTGenerator.Matcher;
using DNTGenerator.Query;
using DNTGenerator.SourceDump;
using DNTGenerator.Verifier;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Immutable;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DexieNETTest.TableGeneratorTest")]
namespace DNTGenerator
{
    [Generator]
    public class Generator : IIncrementalGenerator
    {
        /// <summary>
        /// Initialize the source generator
        /// </summary>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<TypeDeclarationSyntax?> records = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (syntaxNode, _) => syntaxNode.MatchDeclaration(),
                    transform: static (context, _) => context.MatchInterface())
                .Where(static m => m is not null)!;

            IncrementalValueProvider<(Compilation, ImmutableArray<TypeDeclarationSyntax?>)> compilationWithRecords
                = context.CompilationProvider.Combine(records.Collect());

            // Generate the source using the compilation and enums
            context.RegisterImplementationSourceOutput(compilationWithRecords,
                static (context, compilationWithRecords) => AddDBRecordClasses(context, compilationWithRecords));
        }

        private static void AddDBRecordClasses(SourceProductionContext context, (Compilation compilation, ImmutableArray<TypeDeclarationSyntax?> records) compilationWithRecords)
        {
            var dbRecords = compilationWithRecords.records.DBRecords(compilationWithRecords.compilation, context.CancellationToken).ToList();

            foreach (var dbRecord in dbRecords)
            {
                if (dbRecord.Type is DBRecord.RecordType.Interface)
                {
                    dbRecords.SetDerivedProperties(dbRecord, context.CancellationToken);
                }
            }

            var dbRecordsToUse = dbRecords.Where(r => r.Type is not DBRecord.RecordType.Interface);
            if (!dbRecordsToUse.Any())
            {
                return;
            }

            var error = false;
            var forCloud =  false;

            foreach (var dbRecord in dbRecordsToUse)
            {
                if (!forCloud && dbRecord.SchemaDescriptor.HasCloudSync)
                {
                    forCloud = true;
                }

                var diagnostics = dbRecord.Verify(compilationWithRecords.compilation);
                foreach (var diagnostic in diagnostics)
                {
                    if (!DBRecordAnalyzer.Diagnostics.Any(d => d.Id == diagnostic.Id))
                    {
                        diagnostic.ReportDiagnostic(context.ReportDiagnostic);
                    }
                    error = true;
                }
            }

            if (error)
            {
                context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostic.Error, Location.None, compilationWithRecords.compilation.AssemblyName));
                return;
            }

            string source = string.Empty;

            List<string>? nameSpaces = dbRecordsToUse
                .Select(c => c.Namespace).Distinct().ToList();

            var name = Assembly.GetExecutingAssembly().GetName().Name;
            var version = Assembly.GetExecutingAssembly().GetName().Version;

#if DEBUG
            bool success = true;
#endif
            string generated = $@"//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by {name} Version: {version}
// </auto-generated>
//------------------------------------------------------------------------------

";

            foreach (string? ns in nameSpaces)
            {
                var usedNS = ns == "<global namespace>" ? "GlobalNamspace" : ns;

                try
                {
                    source = dbRecordsToUse.DumpNamespace(usedNS, forCloud);
                }
                catch (Exception ex)
                {
                    context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostic.Internal, Location.None, ex.Message));
#if DEBUG
                    success = false;
#endif
                }

                string sourceName = $"{usedNS}.Generated.cs";

                if (source.Any())
                {
                    context.AddSource(sourceName, generated + source);
                }
            }

#if DEBUG
            if (success)
            {
                context.ReportDiagnostic(Diagnostic.Create(GeneratorDiagnostic.Success, Location.None, compilationWithRecords.compilation.AssemblyName));
            }
#endif
        }
    }
}
