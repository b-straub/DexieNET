using DNTGenerator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;

namespace DexieNETTableGeneratorTest.Helpers
{
    internal class GeneratorFactory
    {
        public static IEnumerable<(string Name, SourceText Source)> RunGenerator(params string[] sources)
        {
            List<SyntaxTree> syntaxTrees = new();

            foreach (string? source in sources)
            {
                string? st = source;
                SyntaxTree? syntaxTree = CSharpSyntaxTree.ParseText(SourceText.From(st, Encoding.UTF8), CSharpParseOptions.Default.WithLanguageVersion(LanguageVersion.Preview));
                syntaxTrees.Add(syntaxTree);
            }

            CSharpCompilationOptions? compilationOptions = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithNullableContextOptions(NullableContextOptions.Enable)
                .WithOptimizationLevel(OptimizationLevel.Debug)
                .WithGeneralDiagnosticOption(ReportDiagnostic.Default);

            var references = AppDomain.CurrentDomain.GetAssemblies()
                               .Where(assembly => !assembly.IsDynamic)
                               .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
                               .Cast<MetadataReference>();

            Compilation compilation = CSharpCompilation.Create("testgenerator", syntaxTrees, references, compilationOptions);
            CSharpParseOptions? parseOptions = syntaxTrees.FirstOrDefault()?.Options as CSharpParseOptions;

            Generator? generator = new();

            GeneratorDriver driver = CSharpGeneratorDriver.Create(ImmutableArray.Create(generator.AsSourceGenerator()), parseOptions: parseOptions);
            driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation? generatorCompilation, out ImmutableArray<Diagnostic> generatorDiagnostics);

            var t = generatorCompilation.SyntaxTrees.FirstOrDefault()?.ToString();
            return generatorCompilation.SyntaxTrees.Skip(1).Select((s, i) => ($"Generated{i}", SourceText.From(s.ToString())));
        }
    }
}
