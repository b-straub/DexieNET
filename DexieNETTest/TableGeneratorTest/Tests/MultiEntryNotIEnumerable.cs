using DNTGenerator.Diagnostics;
using Xunit;
using Fixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.IndexCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class MultiEntryNotIEnumerableTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task MultiEntryNotIEnumerableEmpty()
        {
            var test = @"";

            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task MultiEntryNotIEnumerableClassFix()
        {
            var test = @$"
using System.Collections.Generic;
using DexieNET;

namespace Test
{{
    public partial class MultiEntry : IDBStore
    {{
        [Index] string SI {{ get; set; }}
        [Index(IsMultiEntry = true)] IEnumerable<int> IME {{ get; set; }}
        [Index(IsMultiEntry = true)] ulong {{|{GeneratorDiagnostic.MultiEntryNotIEnumerable.Id}:ME1|}} {{ get; set; }}
        [Index(IsMultiEntry = true)] ulong[] {{|{GeneratorDiagnostic.MultiEntryNotIEnumerable.Id}:ME2|}} {{ get; set; }}
    }}
}}
";

            var fix = @"
using System.Collections.Generic;
using DexieNET;

namespace Test
{
    public partial class MultiEntry : IDBStore
    {
        [Index] string SI { get; set; }
        [Index(IsMultiEntry = true)] IEnumerable<int> IME { get; set; }
        [Index(IsMultiEntry = true)] IEnumerable<ulong> ME1 { get; set; }
        [Index(IsMultiEntry = true)] IEnumerable<ulong> ME2 { get; set; }
    }
}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task MultiEntryNotIEnumerableRecordFix()
        {
            var test = @$"
using System.Collections.Generic;
using DexieNET;

namespace Test
{{
    public partial record MultiEntry
    (
        [property: Index(IsMultiEntry = true)] ulong {{|{GeneratorDiagnostic.MultiEntryNotIEnumerable.Id}:ME1|}},
        [property: Index(IsMultiEntry = true)] ulong {{|{GeneratorDiagnostic.MultiEntryNotIEnumerable.Id}:ME2|}}
    ) : IDBStore;
}}
";

            var fix = @"
using System.Collections.Generic;
using DexieNET;

namespace Test
{
    public partial record MultiEntry
    (
        [property: Index(IsMultiEntry = true)] IEnumerable<ulong> ME1,
        [property: Index(IsMultiEntry = true)] IEnumerable<ulong> ME2
    ) : IDBStore;
}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }
    }
}