using DNTGenerator.Diagnostics;
using Xunit;
using ArgumentFixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.AttributeArgumentCodeFix>;
using IndexFixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.IndexCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class RecordGeneratedPrimaryKeysTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task RecordEmpty()
        {
            var test = @"";

            await IndexFixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task RecordGeneratedNameSchemaFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.ReservedGeneratedPKNameSchemaArgument.Id}:PrimaryKeyName = ""PKey""|}})]
    public partial record Person
    (
        [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
    ) : IDBStore;
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    public partial record Person
    (
        [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
    ) : IDBStore;
}
";
            await ArgumentFixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task ClassGeneratedNameCompoundSchemaFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.ReservedGeneratedPKNameSchemaArgument.Id}:PrimaryKeyName = ""PKey""|}})]
    [CompoundIndex(""FirstName"", ""LastName"", IsPrimary = true)]
    public partial class Person : IDBStore
    {{
        string LastName {{ get; set; }}
        string FirstName {{ get; set; }}
    }}
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    [CompoundIndex(""FirstName"", ""LastName"", IsPrimary = true)]
    public partial class Person : IDBStore
    {
        string LastName { get; set; }
        string FirstName { get; set; }
    }
}
";
            await ArgumentFixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task RecordGeneratedGuidSchemaFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.ReservedGeneratedPKGuidSchemaArgument.Id}:PrimaryKeyGuid = false|}})]
    public partial record Person
    (
        [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
    ) : IDBStore;
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    public partial record Person
    (
        [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
    ) : IDBStore;
}
";
            await ArgumentFixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task ClassGeneratedGuidSchemaFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.ReservedGeneratedPKGuidSchemaArgument.Id}:PrimaryKeyGuid = false|}})]
    [CompoundIndex(""FirstName"", ""LastName"", IsPrimary = true)]
    public partial class Person : IDBStore
    {{
        string LastName {{ get; set; }}
        string FirstName {{ get; set; }}
    }}
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    [CompoundIndex(""FirstName"", ""LastName"", IsPrimary = true)]
    public partial class Person : IDBStore
    {
        string LastName { get; set; }
        string FirstName { get; set; }
    }
}
";
            await ArgumentFixer.VerifyCodeFixAsync(test, fix);
        }
    }
}