using DNTGenerator.Diagnostics;
using Xunit;
using Fixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.IndexCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class MissingBoolConverterTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task RecordEmpty()
        {
            var test = @"";

            await Fixer.VerifyAnalyzerAsync(test);
        }

        //No diagnostics expected to show up
        [Fact]
        public async Task BoolNoIndex()
        {
            var test = @$"
using System;
using DexieNET;

namespace Test
{{
    public partial record Test
    (
        bool BoolNoIndex,
        [property: BoolIndex] bool BoolIndex
    ) : IDBStore;
}}
";

            await Fixer.VerifyAnalyzerAsync(test);
        }

        //No diagnostics expected to show up
        [Fact]
        public async Task ByteConverterProvided()
        {
            var test = @$"
using System;
using DexieNET;

namespace Test
{{
    public partial record Test
    (
        [property: ByteIndex] byte[] Blob
    ) : IDBStore;
}}
";

            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ClassWrongBoolIndexConverter()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public partial class Test : IDBStore
    {{
        [{{|{GeneratorDiagnostic.MissingIndexConverter.Id}:ByteIndex|}}] bool BoolIndex {{ get; init; }}
    }}
}}
";

            var fix = @$"
using DexieNET;

namespace Test
{{
    public partial class Test : IDBStore
    {{
        [BoolIndex] bool BoolIndex {{ get; init; }}
    }}
}}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task ClassMissingBoolIndexConverter()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public partial class Test : IDBStore
    {{
        [{{|{GeneratorDiagnostic.MissingIndexConverter.Id}:Index|}}] bool BoolIndex {{ get; init; }}
    }}
}}
";

            var fix = @$"
using DexieNET;

namespace Test
{{
    public partial class Test : IDBStore
    {{
        [BoolIndex] bool BoolIndex {{ get; init; }}
    }}
}}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task RecordMissingBoolIndexConverter()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public partial record Test
    (
        [property: {{|{GeneratorDiagnostic.MissingIndexConverter.Id}:Index|}}] bool BoolIndex
    ) : IDBStore;
}}
";

            var fix = @$"
using DexieNET;

namespace Test
{{
    public partial record Test
    (
        [property: BoolIndex] bool BoolIndex
    ) : IDBStore;
}}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }
    }
}