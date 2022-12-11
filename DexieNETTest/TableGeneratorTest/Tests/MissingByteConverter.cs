using DNTGenerator.Diagnostics;
using Xunit;
using Fixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.IndexCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class MissingByteConverterTests
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
        public async Task BoolConverterProvided()
        {
            var test = @$"
using System;
using DexieNET;

namespace Test
{{
    public partial record Test
    (
        [property: BoolIndex] bool BoolIndex
    ) : IDBStore;
}}
";

            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ClassWrongByteIndexConverter()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public partial class Test : IDBStore
    {{
        [{{|{GeneratorDiagnostic.MissingIndexConverter.Id}:BoolIndex|}}] byte[] ByteIndex {{ get; init; }}
    }}
}}
";

            var fix = @$"
using DexieNET;

namespace Test
{{
    public partial class Test : IDBStore
    {{
        [ByteIndex] byte[] ByteIndex {{ get; init; }}
    }}
}}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task ClassMissingByteIndexConverter()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public partial class Test : IDBStore
    {{
        [{{|{GeneratorDiagnostic.MissingIndexConverter.Id}:Index|}}] byte[] ByteIndex {{ get; init; }}
    }}
}}
";

            var fix = @$"
using DexieNET;

namespace Test
{{
    public partial class Test : IDBStore
    {{
        [ByteIndex] byte[] ByteIndex {{ get; init; }}
    }}
}}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task RecordMissingByteIndexConverter()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public partial record Test
    (
        [property: {{|{GeneratorDiagnostic.MissingIndexConverter.Id}:Index|}}] byte[] ByteIndex
    ) : IDBStore;
}}
";

            var fix = @$"
using DexieNET;

namespace Test
{{
    public partial record Test
    (
        [property: ByteIndex] byte[] ByteIndex
    ) : IDBStore;
}}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }
    }
}