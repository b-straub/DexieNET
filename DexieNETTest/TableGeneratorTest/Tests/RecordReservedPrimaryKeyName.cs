using DNTGenerator.Diagnostics;
using Xunit;
using ArgumentFixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.AttributeArgumentCodeFix>;
using IndexFixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.IndexCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class RecordReservedPrimaryKeyName
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task RecordEmpty()
        {
            var test = @"";

            await IndexFixer.VerifyAnalyzerAsync(test);
        }


        [Fact]
        public async Task ClassReservedPrimaryKeyNameSchemaFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.ReservedPrimaryKeyNameSchemaArgument.Id}:PrimaryKeyName = ""ID""|}})]
    public partial class Person : IDBStore
    {{
        string Test {{ get; set; }}
    }}
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    [Schema(PrimaryKeyName = ""ID1"")]
    public partial class Person : IDBStore
    {
        string Test { get; set; }
    }
}
";
            await ArgumentFixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task ClassReservedPrimaryKeyNameMemberFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public partial class Person : IDBStore
    {{
        string Test {{ get; set; }}
        int? {{|{GeneratorDiagnostic.ReservedPrimaryKeyNameMember.Id}:ID|}} {{ get; set; }}
    }}
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    public partial class Person : IDBStore
    {
        string Test { get; set; }
        int? ID1 { get; set; }
    }
}
";
            await IndexFixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task RecordReservedPrimaryKeyNameMemberFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public record TestRecord(string Test);

    public partial record Person
    (
        string Test,
        string ID2,
        string ID1,
        int {{|{GeneratorDiagnostic.ReservedPrimaryKeyNameMember.Id}:Id|}}
    ) : IDBStore;
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    public record TestRecord(string Test);

    public partial record Person
    (
        string Test,
        string ID2,
        string ID1,
        int ID11
    ) : IDBStore;
}
";
            await IndexFixer.VerifyCodeFixAsync(test, fix);
        }
    }
}