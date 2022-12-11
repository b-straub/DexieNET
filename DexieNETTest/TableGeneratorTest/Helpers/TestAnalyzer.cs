using DexieNETTableGeneratorTest.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using NuGet.Frameworks;

namespace DNTGeneratorTest.Helpers
{
    internal static class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public static DiagnosticResult Diagnostic()
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic();

        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpCodeFixVerifier<TAnalyzer, TCodeFix, XUnitVerifier>.Diagnostic(diagnosticId);

        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => new(descriptor);

        public static Task VerifyAnalyzerAsync(string source, params DiagnosticResult[] expected)
        {
            var test = new Test<TAnalyzer, TCodeFix> { TestCode = source };

            var generatedSources = GeneratorFactory.RunGenerator(source);
            test.TestState.GeneratedSources.AddRange(generatedSources);

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync();
        }

        public static Task VerifyCodeFixAsync(string source, string fixedSource, int? codeActionIndex = null)
            => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource, codeActionIndex);

        public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource, int? codeActionIndex = null)
            => VerifyCodeFixAsync(source, new[] { expected }, fixedSource, codeActionIndex);

        public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource, int? codeActionIndex = null)
        {
            var test = new Test<TAnalyzer, TCodeFix>
            {
                TestCode = source,
                FixedCode = fixedSource,
                CodeActionIndex = codeActionIndex,
                CodeFixTestBehaviors = codeActionIndex is not null ? CodeFixTestBehaviors.SkipFixAllCheck : CodeFixTestBehaviors.None
            };

            var generatedSources = GeneratorFactory.RunGenerator(source);
            test.TestState.GeneratedSources.AddRange(generatedSources);

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync();
        }
    }

    internal class Test<TAnalyzer, TCodeFix> : CSharpCodeFixTest<TAnalyzer, TCodeFix, XUnitVerifier>
        where TAnalyzer : DiagnosticAnalyzer, new()
        where TCodeFix : CodeFixProvider, new()
    {
        public Test()
        {
            ReferenceAssemblies = Net70;

            SolutionTransforms.Add((solution, projectId) =>
            {
                Project project = solution.GetProject(projectId)!;
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(typeof(DexieNET.IndexAttribute).Assembly.Location));
                project = project.AddMetadataReference(MetadataReference.CreateFromFile(typeof(DexieNET.IDBStore).Assembly.Location));
                return project.Solution;
            });
        }

        private static ReferenceAssemblies Net70 => _lazyNet70.Value;

        private static readonly Lazy<ReferenceAssemblies> _lazyNet70 =
                new(() =>
                {
                    if (!NuGetFramework.Parse("net7.0").IsPackageBased)
                    {
                        // The NuGet version provided at runtime does not recognize the 'net7.0' target framework
                        throw new NotSupportedException("The 'net7.0' target framework is not supported by this version of NuGet.");
                    }

                    return new ReferenceAssemblies(
                         "net7.0",
                         new PackageIdentity(
                              "Microsoft.NETCore.App.Ref",
                              "7.0.0"),
                         Path.Combine("ref", "net7.0"));
                });
    }
}