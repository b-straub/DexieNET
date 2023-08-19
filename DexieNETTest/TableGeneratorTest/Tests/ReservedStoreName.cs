using DexieNET;
using DNTGenerator.Diagnostics;
using Xunit;
using Fixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.RecordCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class ResrvedStoreNameTests
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
        public async Task RecordValidMembers()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public partial record Members
    (
        string NotIndexed
    ) : IDBStore;
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task RecordInvalidMembers()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema(CloudSync = true)]
    public partial record  {{|{GeneratorDiagnostic.ReservedStoreName.Id}:Members|}}
    (
        string NotIndexed
    ) : IDBStore;
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task RecordInvalidMembersAttribute()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema({{|{GeneratorDiagnostic.ReservedStoreName.Id}:StoreName = ""Members""|}}, CloudSync = true)]
    public partial record TestStore
    (
        string NotIndexed
    ) : IDBStore;
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }
    }
}