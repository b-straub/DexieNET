using DNTGenerator.Diagnostics;
using Xunit;
using Fixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.IndexCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class IndexNotNumericTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task AutoIncrementNotNumericEmpty()
        {
            var test = @"";

            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AutoIncrementNotNumericGuidClass()
        {
            var test = @$"
using DexieNET;
using System;

namespace Test
{{
    public partial class Person : IDBStore
    {{
        [Index(IsPrimary = true, IsAuto = true)] Guid? PKey {{ get; set; }}
    }}
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AutoIncrementNotNumericClass()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public class Person : IDBStore
    {{
        string? NotIndexed {{ get; set; }}

        [Index(IsPrimary = true, IsAuto = true)] string? {{|{GeneratorDiagnostic.AutoIncrementNotNumeric.Id}:ID|}} {{ get; set; }}
    }}
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AutoIncrementNotNumericGuidRecord()
        {
            var test = @$"
using DexieNET;
using System;

namespace Test
{{
    public partial record Person
    (
        [property: Index(IsPrimary = true, IsAuto = true)] Guid? PKey
    ) : IDBStore;
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AutoIncrementNotNumericRecord()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public record Person
    (
        string NotIndexed,
        [property: Index(IsPrimary = true, IsAuto = true)] string? {{|{GeneratorDiagnostic.AutoIncrementNotNumeric.Id}:ID|}}
    ) : IDBStore;
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task AutoIncrementNotNumericClassFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public class Person : IDBStore
    {{
        string? NotIndexed {{ get; set; }}

        [Index(IsPrimary = true, IsAuto = true)] string? {{|{GeneratorDiagnostic.AutoIncrementNotNumeric.Id}:ID|}} {{ get; set; }}
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
            await Fixer.VerifyCodeFixAsync(test, fix, 2);
        }

        [Fact]
        public async Task AutoIncrementNotNumericRecordFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public record Person
    (
        string NotIndexed,
        [property: Index(IsPrimary = true, IsAuto = true)] string? {{|{GeneratorDiagnostic.AutoIncrementNotNumeric.Id}:ID|}}
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
            await Fixer.VerifyCodeFixAsync(test, fix, 2);
        }
    }
}