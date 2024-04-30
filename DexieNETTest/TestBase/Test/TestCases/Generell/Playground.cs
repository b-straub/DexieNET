using DexieNET;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace DexieNETTest.TestBase.Test
{
    internal class Playground(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "Playground";

        private async Task<Unit> BulkAddOp()
        {
            var persons = DataGenerator.GetPersons();
            await DB.Persons.BulkAdd(persons);
            return Unit.Default;
        }

        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);
            var disposeBag = new CompositeDisposable();
            var disposeSubject = new BehaviorSubject<int>(0);
            string? firstError = null;

            var tablePersons = DB.Persons;
            await tablePersons.Clear();

            var persons = DataGenerator.GetPersons();
            var expectedCountP = persons.Where(p => p.Age >= 50).Count();

            var observableP = DB.LiveQuery(async () =>
            {
                //await tablePersons.Count(); // First LiveQuery will not populate changed keys otherwise

                var values = await tablePersons.Where(p => p.Age).AboveOrEqual(50).ToArray();
                if (values.Any() && expectedCountP != values.Count())
                {
                    throw new InvalidOperationException($"observableP: {values.Count()} -=> LiveQuery failed.");
                }
                return values;
            });

            var disposableP = observableP.Subscribe(values =>
            {
                disposeSubject.OnNext(disposeSubject.Value + 1);
            }, onError: e => firstError ??= e.Message);

            disposeBag.Add(disposableP);

            var opDisposable = Observable.Interval(TimeSpan.FromMilliseconds(10))
                .SelectMany(async op =>
                {
                    switch (op)
                    {
                        case 10:
                            await BulkAddOp();
                            break;
                        default:
                            break;
                    }

                    return Unit.Default;
                })
            .Finally(() => disposeSubject.OnNext(disposeSubject.Value + 1))
            .Subscribe();

            var finished = false;

            void testFinished()
            {
                disposeBag.Dispose();
                finished = true;
            }

            disposeSubject
               .Timeout(TimeSpan.FromSeconds(10))
               .Subscribe(
                   onNext: d =>
                   {
                        if (d > 1)
                        {
                            testFinished();
                        }
                   },
                   onError: e =>
                   {
                       if (e.GetType() == typeof(TimeoutException))
                       {
                           testFinished();
                       }
                   });

            do
            {
                await Task.Delay(100);
            } while (!finished);

            opDisposable.Dispose();

            if (firstError is not null)
            {
                throw new InvalidOperationException(firstError);
            }

            return "OK";
        }
    }
}
