using DexieNETTest.Tests.Infrastructure;
using Microsoft.Playwright;
using System;
using System.Threading.Tasks;
using Xunit;

namespace DexieNETTest.Tests.Tests
{
    public abstract class DexieNETTestBase : IAsyncLifetime
    {
        private readonly IWAFixture _fixture;

        public DexieNETTestBase(IWAFixture fixture)
        {
            _fixture = fixture;
        }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public async Task InitializeAsync()
        {
            await _fixture.InitializeAsync();
        }

        [Theory]
        // General
        [InlineData("VersionUpdate")]
        [InlineData("KeyTest")]
        [InlineData("OpenClose")]
        [InlineData("FailedTransaction")]
        [InlineData("TransactionsParallel")]
        [InlineData("TransactionsParallelFail")]
        [InlineData("TransactionsNested")]
        [InlineData("TransactionsNestedFail", IWAFixture.BrowserType.Webkit)] // only Playwright
        [InlineData("EqualSpecialFields")]
        [InlineData("Reverse")]
        [InlineData("CompoundPrimary")]
        [InlineData("LiveQueryTest")]
        // Table
        [InlineData("ClearCount", IWAFixture.BrowserType.Webkit)] // only Playwright
        [InlineData("Add")]
        [InlineData("AddPNotAuto")]
        [InlineData("AddPExtern", IWAFixture.BrowserType.Webkit)] // only Playwright
        [InlineData("AddClass")]
        [InlineData("AddComplex")]
        [InlineData("BulkAdd")]
        [InlineData("BulkAddAllKeys")]
        [InlineData("Put")]
        [InlineData("PutPExtern")]
        [InlineData("PutPNotAuto")]
        [InlineData("BulkPutAllKeys")]
        [InlineData("Update")]
        [InlineData("Get")]
        [InlineData("BulkGet")]
        [InlineData("Delete")]
        [InlineData("BulkDelete")]
        [InlineData("ToCollection")]
        [InlineData("TableFilter")]
        [InlineData("OrderBy")]
        // Collection
        [InlineData("CollectionFirstLast")]
        [InlineData("CollectionFilter")]
        [InlineData("CollectionEach")]
        [InlineData("CollectionModify")]
        [InlineData("CollectionPrimaryKeys")]
        [InlineData("CollectionUntil")]
        [InlineData("Distinct")]
        [InlineData("Keys")]
        [InlineData("LimitOffset")]
        [InlineData("Or")]
        // WhereClause
        [InlineData("Above")]
        [InlineData("InAnyRange")]
        [InlineData("AnyOf")]
        [InlineData("Below")]
        [InlineData("Between")]
        [InlineData("Equal")]
        [InlineData("NoneOf")]
        [InlineData("NotEqual")]
        [InlineData("StartsWith")]
        public async Task TestCase(string name, IWAFixture.BrowserType typeToSkip = IWAFixture.BrowserType.None)
        {
            if (typeToSkip == _fixture.Type)
            {
                return;
            }

            Assert.True(_fixture.Page is not null);

            var timeout = 500;

            if (!_fixture.OnePass)
            {
                timeout = _fixture.Type switch
                {
                    IWAFixture.BrowserType.Chromium => 5000,
                    IWAFixture.BrowserType.Firefox => 50000,
                    IWAFixture.BrowserType.Webkit => 5000,
                    _ => throw new ArgumentOutOfRangeException(nameof(_fixture.Type), nameof(_fixture.Type))
                };

                await _fixture.Page.GotoAsync($"https://localhost:{_fixture.Port}/{name}");
            }

            var options = new LocatorAssertionsToBeVisibleOptions()
            {
                Timeout = timeout
            };

            await Assertions.Expect(_fixture.Page.Locator($"text=DexieNET -> {name}: OK")).ToBeVisibleAsync(options);
        }
    }
}

