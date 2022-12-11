using DNTGenerator.Diagnostics;
using Xunit;
using ArgumentFixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.AttributeArgumentCodeFix>;
using IndexFixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.IndexCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class RecordMultiplePrimaryKeysTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task RecordEmpty()
        {
            var test = @"";

            await IndexFixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ClassMultiplePrimaryKeys()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [CompoundIndex(""FirstName"", ""LastName"", {{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})]
    public partial class Person : IDBStore
    {{
        [Index({{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})] int ID1 {{ get; set; }}
        [Index({{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})] int ID2 {{ get; set; }}
        string LastName {{ get; set; }}
        string FirstName {{ get; set; }}
    }}
}}
";
            await IndexFixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task RecordMultiplePrimaryKeys()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [CompoundIndex(""FirstName"", ""LastName"", {{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})]
    public partial record Person
    (
        [property: Index({{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})] int ID1,
        [property: Index({{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})] int ID2,
        string LastName,
        string FirstName
    ) : IDBStore;
}}
";
            await IndexFixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ClassMultiplePrimaryKeysFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [CompoundIndex(""FirstName"", ""LastName"", {{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})]
    public partial class Person : IDBStore
    {{
        [Index({{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})] int ID1 {{ get; set; }}
        [Index({{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})] int ID2 {{ get; set; }}
        string LastName {{ get; set; }}
        string FirstName {{ get; set; }}
    }}
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    [CompoundIndex(""FirstName"", ""LastName"")]
    public partial class Person : IDBStore
    {
        [Index] int ID1 { get; set; }
        [Index(IsPrimary = true)] int ID2 { get; set; }
        string LastName { get; set; }
        string FirstName { get; set; }
    }
}
";
            await ArgumentFixer.VerifyCodeFixAsync(test, fix, 0);
        }

        [Fact]
        public async Task RecordMultiplePrimaryKeysFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [CompoundIndex(""FirstName"", ""LastName"", {{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})]
    public partial record Person
    (
        [property: Index({{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})] int ID1,
        [property: Index({{|{GeneratorDiagnostic.MultiplePrimaryKeysArgument.Id}:IsPrimary = true|}})] int ID2,
        string LastName,
        string FirstName
    ) : IDBStore;
}}
";

            var fix = @"
using DexieNET;

namespace Test
{
    [CompoundIndex(""FirstName"", ""LastName"")]
    public partial record Person
    (
        [property: Index] int ID1,
        [property: Index(IsPrimary = true)] int ID2,
        string LastName,
        string FirstName
    ) : IDBStore;
}
";
            await ArgumentFixer.VerifyCodeFixAsync(test, fix, 0);
        }

        [Fact]
        public async Task ClassMultiplePrimaryKeysSchemaFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.MultiplePrimaryKeysSchemaArgument.Id}:PrimaryKeyName = ""PKey""|}})]
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