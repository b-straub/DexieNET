using DexieNET;
using Microsoft.AspNetCore.Components;

namespace DexieNETTest.TestBase.Test
{
    public sealed partial class DBTests : IDisposable
    {
        [Inject]
        public IDexieNETService<TestDB>? DexieNETService { get; set; }

        [Inject]
        public IDexieNETService<SecondDB>? DexieNETServiceSecond { get; set; }

        [Parameter]
        public RenderFragment? ChildContent { get; set; }

        [Parameter]
        public bool Benchmark { get; set; } = false;

        [Parameter]
        public string? TestName { get; set; }


        private TestDB? _db;

        private string? _error;
        private readonly List<(string Category, DexieTest<TestDB> Test)> _tests = new();
        private readonly List<(string Category, string Result, bool Error)> _testResults = new();

        private string? _lastCategory;
        private bool _running;
        private bool _testsCompleted = false;

        private readonly CancellationTokenSource _cancellationTokenSource = new();

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            await base.OnAfterRenderAsync(firstRender);

            if (firstRender && DexieNETService is not null && DexieNETServiceSecond is not null)
            {
                _running = false;
                var playwrightTest = TestName?.ToLowerInvariant() == "playwright";

                try
                {
                    await DexieNETService.DexieNETFactory.Delete();
                    _db = await DexieNETService.DexieNETFactory.Create();

                    if (_db is null)
                    {
                        throw new InvalidOperationException("Can not create database.");
                    }

                    _db.Version(1).Stores();

                    SecondDB? _dbsecond = null;

                    if (string.IsNullOrEmpty(TestName))
                    {
                        await DexieNETServiceSecond.DexieNETFactory.Delete();
                        _dbsecond = await DexieNETServiceSecond.DexieNETFactory.Create();

                        if (_dbsecond is null)
                        {
                            throw new InvalidOperationException("Can not create second database.");
                        }

                        _dbsecond.Version(1).Stores();
                    }

                    var testFactory = new TestFactory(_db);

                    if (Benchmark)
                    {
                        _tests.Add(("Benchmark", new Benchmark(_db, _cancellationTokenSource.Token)));
                    }
                    else
                    {
                        _tests.AddRange(testFactory.GetTests(TestName));
                    }

                    _running = true;
                    await InvokeAsync(StateHasChanged);

                    await foreach (var testResult in RunTestsAsync(_dbsecond, Benchmark))
                    {
                        _testResults.Add(testResult);
                        await InvokeAsync(StateHasChanged);
                    }

                    _running = false;
                    _testsCompleted = true;
                    await InvokeAsync(StateHasChanged);

                    await DexieNETService.DexieNETFactory.Delete();
                    await DexieNETServiceSecond.DexieNETFactory.Delete();
                }
                catch (Exception ex)
                {
                    _error = ex.Message;
                }

                await InvokeAsync(StateHasChanged);
            }
        }

        public void Dispose()
        {
            _db?.Close();
        }

        public void Cancel()
        {
            _cancellationTokenSource.Cancel();
        }

        private async IAsyncEnumerable<(string Category, string Result, bool Error)> RunTestsAsync(SecondDB? dbSecond, bool benchmark = false)
        {
            string? result;
            bool error = false;

            if (!benchmark && dbSecond is not null)
            {
                var testSecond = new AddSecond(dbSecond);

                try
                {
                    result = await testSecond.RunTest();
                }
                catch (Exception ex)
                {
                    result = ex.Message;
                    error = true;
                }
                yield return ("SecondDB", testSecond?.Name + ": " + result, error);
            }

            foreach (var (category, test) in _tests)
            {
                error = false;

                if (_cancellationTokenSource.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    result = await test.RunTest();
                }
                catch (Exception ex)
                {
                    result = ex.Message;
                    error = true;
                }

                if (result is not null)
                {
                    yield return (category, "DexieNET -> " + test.Name + ": " + result, error);
                }
            }
        }
    }
}
