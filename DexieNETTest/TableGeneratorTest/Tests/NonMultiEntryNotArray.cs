using DNTGenerator.Diagnostics;
using Xunit;
using Fixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.IndexCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class NonMultiEntryNotArrayTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task NonMultiEntryNotArrayEmpty()
        {
            var test = @"";

            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task NonMultiEntryNotArrayClassFix()
        {
            var test = @$"
using System.Collections.Generic;
using DexieNET;

namespace Test
{{
    public partial class NonMultiEntry : IDBStore
    {{
        [Index] IEnumerable<ulong> {{|{GeneratorDiagnostic.NonMultiEntryNotArray.Id}:NME|}} {{ get; set; }}
    }}
}}
";

            var fix = @"
using System.Collections.Generic;
using DexieNET;

namespace Test
{
    public partial class NonMultiEntry : IDBStore
    {
        [Index] ulong[] NME { get; set; }
    }
}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task NonMultiEntryNotArrayRecordFix()
        {
            var test = @$"
using System.Collections.Generic;
using DexieNET;

namespace Test
{{
    public partial record NonMultiEntry
    (
        [property: Index] IEnumerable<ulong> {{|{GeneratorDiagnostic.NonMultiEntryNotArray.Id}:NME|}}
    ) : IDBStore;
}}
";

            var fix = @"
using System.Collections.Generic;
using DexieNET;

namespace Test
{
    public partial record NonMultiEntry
    (
        [property: Index] ulong[] NME
    ) : IDBStore;
}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }
    }
}