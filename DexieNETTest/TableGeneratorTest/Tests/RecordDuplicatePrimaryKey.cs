using DNTGenerator.Diagnostics;
using Xunit;
using ArgumentFixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.AttributeArgumentCodeFix>;
using IndexFixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.IndexCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class RecordDuplicatePrimaryKeyTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task RecordEmpty()
        {
            var test = @"";

            await IndexFixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task SchemaOK()
        {
            var test = @"
    using DexieNET;

    namespace Test
    {
        [Schema(PrimaryKeyName = ""PKey"")]
        public partial class Person : IDBStore
        {
            [Index] public string FirstName { get; init; }
            [Index] public string LastName { get; init; }
        }
    }
";

            await ArgumentFixer.VerifyAnalyzerAsync(test);
        }


        [Fact]
        public async Task ClassDuplicatePrimaryIndex()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.DuplicatePrimaryKeySchemaArgument.Id}:PrimaryKeyName = ""PKey""|}})]
    public partial class Person : IDBStore
    {{
        int? {{|{GeneratorDiagnostic.DuplicatePrimaryKeyMember.Id}:PKey|}} {{ get; set; }}
    }}
}}
";
            await IndexFixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task RecordDuplicatePrimaryIndex()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.DuplicatePrimaryKeySchemaArgument.Id}:PrimaryKeyName = ""PKey""|}})]
    public partial record Person
    (
        int {{|{GeneratorDiagnostic.DuplicatePrimaryKeyMember.Id}:PKey|}}
    ) : IDBStore;
}}
";
            await IndexFixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ClassDuplicatePrimaryIndexFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.DuplicatePrimaryKeySchemaArgument.Id}:PrimaryKeyName = ""PKey""|}})]
    public partial class Person : IDBStore
    {{
        string Test {{ get; set; }}
        int? {{|{GeneratorDiagnostic.DuplicatePrimaryKeyMember.Id}:PKey|}} {{ get; set; }}
    }}
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    [Schema(PrimaryKeyName = ""PKey"")]
    public partial class Person : IDBStore
    {
        string Test { get; set; }
    }
}
";
            await IndexFixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task RecordDuplicatePrimaryIndexFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.DuplicatePrimaryKeySchemaArgument.Id}:PrimaryKeyName = ""PKey""|}})]
    public partial record Person
    (
        string Test,
        int {{|{GeneratorDiagnostic.DuplicatePrimaryKeyMember.Id}:PKey|}}
    ) : IDBStore;
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    [Schema(PrimaryKeyName = ""PKey"")]
    public partial record Person
    (
        string Test
    ) : IDBStore;
}
";
            await IndexFixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task RecordStructDuplicatePrimaryIndexFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.DuplicatePrimaryKeySchemaArgument.Id}:PrimaryKeyName = ""PKey""|}})]
    public partial record struct Person
    (
        string Test,
        int {{|{GeneratorDiagnostic.DuplicatePrimaryKeyMember.Id}:PKey|}}
    ) : IDBStore;
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    [Schema(PrimaryKeyName = ""PKey"")]
    public partial record struct Person
    (
        string Test
    ) : IDBStore;
}
";
            await IndexFixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task RecordDuplicatePrimarySchemaFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public record TestRecord(string Test);

    [Schema({{|{GeneratorDiagnostic.DuplicatePrimaryKeySchemaArgument.Id}:PrimaryKeyName = ""PKey""|}})]
    public partial record Person
    (
        string Test,
        int {{|{GeneratorDiagnostic.DuplicatePrimaryKeyMember.Id}:PKey|}}
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
        int PKey
    ) : IDBStore;
}
";
            await ArgumentFixer.VerifyCodeFixAsync(test, fix);
        }
    }
}