using DNTGenerator.Diagnostics;
using Xunit;
using Fixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.IndexCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class RecordCompoundIndexNotFoundTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task RecordEmpty()
        {
            var test = @"";

            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ClassCompoundIndexNotFound()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [CompoundIndex(""FirstName"", {{|{GeneratorDiagnostic.CompoundIndexNotFound.Id}:""LastName""|}}, IsPrimary = true)]
    public partial class Person : IDBStore
    {{
        string LastName1 {{ get; set; }}
        string FirstName {{ get; set; }}
    }}
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task RecordCompoundIndexNotFound()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [CompoundIndex({{|{GeneratorDiagnostic.CompoundIndexNotFound.Id}:""FirstName""|}}, ""LastName"", IsPrimary = true)]
    public partial record Person
    (
        string LastName,
        string FirstName1
    ) : IDBStore;
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }
    }
}