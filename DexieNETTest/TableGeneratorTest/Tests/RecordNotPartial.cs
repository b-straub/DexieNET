using DNTGenerator.Diagnostics;
using Xunit;
using Fixer = DNTGeneratorTest.Helpers.CSharpCodeFixVerifier<DNTGenerator.Analyzer.DBRecordAnalyzer, DNTGenerator.CodeFix.RecordCodeFix>;

namespace DNTGeneratorTest.Tests
{
    public class RecordNotPartialTests
    {
        //No diagnostics expected to show up
        [Fact]
        public async Task RecordEmpty()
        {
            var test = @"";

            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ClassNotPartialOutbound()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema(OutboundPrimaryKey = true)]
    public class Person : IDBStore
    {{
        string? NotIndexed {{ get; set; }}
    }}
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task RecordNotPartialOutbound()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema(OutboundPrimaryKey = true)]
    public record Person
    (
        string NotIndexed
    ) : IDBStore;
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ClassNotPartial()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public class {{|{GeneratorDiagnostic.NotPartial.Id}:Person|}} : IDBStore
    {{
        string? NotIndexed {{ get; set; }}
    }}
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ClassNotPartialWithID()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema(PrimaryKeyName = ""PKey"")]
    public class {{|{GeneratorDiagnostic.NotPartial.Id}:Person|}} : IDBStore
    {{
        string? NotIndexed {{ get; set; }}
    }}
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task RecordNotPartial()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    public record {{|{GeneratorDiagnostic.NotPartial.Id}:Person|}}
    (
        string NotIndexed
    ) : IDBStore;
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task RecordNotPartialWithID()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema(PrimaryKeyName = ""PKey"")]
    public record {{|{GeneratorDiagnostic.NotPartial.Id}:Person|}}
    (
        string NotIndexed
    ) : IDBStore;
}}
";
            await Fixer.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task ClassNotPartialWithIDFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema(PrimaryKeyName = ""PKey"")]
    public class {{|{GeneratorDiagnostic.NotPartial.Id}:Person|}} : IDBStore
    {{
        string? NotIndexed {{ get; set; }}
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
        string? NotIndexed { get; set; }
    }
}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task RecordNotPartialWithIDFix()
        {
            var test = @$"
using DexieNET;

namespace Test
{{
    [Schema(PrimaryKeyName = ""PKey"")]
    public record {{|{GeneratorDiagnostic.NotPartial.Id}:Person|}}
    (
        string NotIndexed
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
        string NotIndexed
    ) : IDBStore;
}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task RecordNotPartialWithAutoGuidFix()
        {
            var test = @$"
using DexieNET;
using System;

namespace Test
{{
    public record {{|{GeneratorDiagnostic.NotPartial.Id}:Person|}}
    (
        [property: Index(IsPrimary = true, IsAuto = true)] string? ID = null
    ) : IDBStore;
}}
";

            var fix = @"
using DexieNET;
using System;

namespace Test
{
    public partial record Person
    (
        [property: Index(IsPrimary = true, IsAuto = true)] string? ID = null
    ) : IDBStore;
}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }

        [Fact]
        public async Task RecordNotPartialWithCloudSyncFix()
        {
            var test = @$"
using DexieNET;
using System;

namespace Test
{{
    [Schema(CloudSync = true)]
    public record {{|{GeneratorDiagnostic.NotPartial.Id}:Person|}}
    (
        [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
    ) : IDBStore;
}}
";

            var fix = @"
using DexieNET;
using System;

namespace Test
{
    [Schema(CloudSync = true)]
    public partial record Person
    (
        [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
    ) : IDBStore;
}
";
            await Fixer.VerifyCodeFixAsync(test, fix);
        }
    }
}