using DNTGenerator.Diagnostics;
using Xunit;
using Fixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.AttributeArgumentCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class IndexAutoWithoutPrimaryTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task AutoWithoutPrimaryEmpty()
        {
            var test = @"";

            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AutoWithoutPrimaryClassFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public class Person : IDBStore
    {{
        [Index({{|{GeneratorDiagnostic.AutoWithoutPrimaryKeyArgument.Id}:IsAuto = true|}})] ulong? ID {{ get; set; }}
    }}
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    public class Person : IDBStore
    {
        [Index(IsPrimary = true, IsAuto = true)] ulong? ID { get; set; }
    }
}
";
            await Fixer.VerifyCodeFixAsync(test, fix, 0);
        }

        [Fact]
        public async Task AutoWithoutPrimaryRecordFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public record Person
    (
        [property: Index({{|{GeneratorDiagnostic.AutoWithoutPrimaryKeyArgument.Id}:IsAuto = true|}})] ulong? ID
    ) : IDBStore;
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    public record Person
    (
        [property: Index(IsPrimary = true, IsAuto = true)] ulong? ID
    ) : IDBStore;
}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }
    }
}