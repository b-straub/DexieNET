using DNTGenerator.Diagnostics;
using Xunit;
using Fixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.IndexCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class IndexNotNullableTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task AutoIncrementNotNullableEmpty()
        {
            var test = @"";

            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AutoIncrementNotNullableClass()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public class Person : IDBStore
    {{
        string? NotIndexed {{ get; set; }}

        [Index(IsPrimary = true, IsAuto = true)] ulong {{|{GeneratorDiagnostic.AutoIncrementNotNullable.Id}:ID|}} {{ get; set; }}
    }}
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AutoIncrementNotNullableStruct()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public struct Person : IDBStore
    {{
        string? NotIndexed {{ get; set; }}

        [Index(IsPrimary = true, IsAuto = true)] ulong {{|{GeneratorDiagnostic.AutoIncrementNotNullable.Id}:ID|}} {{ get; set; }}
    }}
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AutoIncrementNotNullableRecord()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public record Person
    (
        string NotIndexed,
        [property: Index(IsPrimary = true, IsAuto = true)] ulong {{|{GeneratorDiagnostic.AutoIncrementNotNullable.Id}:ID|}}
    ) : IDBStore;
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AutoIncrementNotNullableRecordStruct()
        {
            string test = @$"
using DexieNET;

namespace Test
{{
    public record struct Person
    (
        string NotIndexed,
        [property: Index(IsPrimary = true, IsAuto = true)] ulong {{|{GeneratorDiagnostic.AutoIncrementNotNullable.Id}:ID|}}
    ) : IDBStore;
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AutoIncrementNotNullableClassFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public class Person : IDBStore
    {{
        string? NotIndexed {{ get; set; }}

        [Index(IsPrimary = true, IsAuto = true)] ulong {{|{GeneratorDiagnostic.AutoIncrementNotNullable.Id}:ID|}} {{ get; set; }}
    }}
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    public class Person : IDBStore
    {
        string? NotIndexed { get; set; }

        [Index(IsPrimary = true, IsAuto = true)] ulong? ID { get; set; }
    }
}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task AutoIncrementNotNullableRecordFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public record Person
    (
        string NotIndexed,
        [property: Index(IsPrimary = true, IsAuto = true)] ulong {{|{GeneratorDiagnostic.AutoIncrementNotNullable.Id}:ID|}}
    ) : IDBStore;
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    public record Person
    (
        string NotIndexed,
        [property: Index(IsPrimary = true, IsAuto = true)] ulong? ID
    ) : IDBStore;
}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }
    }
}