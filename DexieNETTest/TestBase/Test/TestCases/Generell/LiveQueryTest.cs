using DexieNET;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace DexieNETTest.TestBase.Test
{
    internal class LiveQueryTest(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "LiveQueryTest";

        private async Task<Unit> BulkAddOp()
        {
            var persons = DataGenerator.GetPersons();
            await DB.Persons.BulkAdd(persons);
            return Unit.Default;
        }

        private ulong? keyP = null;

        private async Task PersonAddOp()
        {
            keyP = await DB.Persons.Add(DataGenerator.GetPerson3());
        }

        private async Task StudentAddOp()
        {
            ArgumentNullException.ThrowIfNull(keyP);
            var student = new Student("Physics", (int)keyP);
            await DB.Students.Add(student);
        }

        private async Task PersonPutOp()
        {
            var person = await DB.Persons.ToCollection().First();
            person = person! with { Age = 100 };

            await DB.Transaction(async _ =>
            {
                await DB.Persons.Put(person);
            });
        }

        private bool _exThrown = false;

        private async Task FailedTransactionOp()
        {
            try
            {
                await DB.Transaction(async ta =>
                {
                    var person = await DB.Persons.ToCollection().First();
                    person = ta.Collecting ? person : person! with { Age = 90 };
                    await DB.Persons.Put(person);
                    await DB.Persons.Add(person);
                });
            }
            catch (Exception ex)
            {
                _exThrown = ex.GetType() == typeof(TransactionException);
            }
        }

        public override async ValueTask<string?> RunTest()
        {
            var disposeBag = new CompositeDisposable();
            var disposeSubject = new BehaviorSubject<int>(0);
            string? firstError = null;

            var tablePersons = DB.Persons;
            await tablePersons.Clear();
            var tableStudents = DB.Students;
            await tableStudents.Clear();

            var persons = DataGenerator.GetPersons();
            var expectedCountP = persons.Where(p => p.Age >= 50).Count();

            var observableP = DB.LiveQuery(async () =>
            {
                var values = await tablePersons.Where(p => p.Age).AboveOrEqual(50).ToArray();
                if (values.Any() && expectedCountP != values.Count())
                {
                    throw new InvalidOperationException($"observableP: {values.Count()} -=> LiveQuery failed.");
                }
                return values;
            });
            
            var observableS = DB.LiveQuery(async () =>
            {
                var values = await tableStudents.ToArray();
                if (values.Any() && values.Count() != 1)
                {
                    throw new InvalidOperationException($"observableS: {values.Count()} -> LiveQuery failed.");
                }
                return values;
            });
            
            var observableSP = DB.LiveQuery(async () =>
            {
                Student? student = null;

                var persons = await tablePersons.ToArray();
                student = (await tableStudents.ToArray()).FirstOrDefault();

                return persons.Where(p => (int?)p.Id == student?.Id).Select(p => p.Age);
            });
            
            int executionCountP = 0;

            var disposableP = observableP.Subscribe(values =>
            {
                executionCountP++;
                disposeSubject.OnNext(disposeSubject.Value + 1);
            }, onError: e => firstError ??= e.Message);

            disposeBag.Add(disposableP);

            int executionCountS = 0;

            var disposableS = observableS.Subscribe(values =>
            {
                executionCountS++;
                disposeSubject.OnNext(disposeSubject.Value + 1);
            }, onError: e => firstError ??= e.Message);

            disposeBag.Add(disposableS);

            int executionCountSP = 0;

            IEnumerable<int> result = Enumerable.Empty<int>();

            var disposableSP = observableSP.Subscribe(values =>
            {
                result = values.ToArray();
                executionCountSP++;
                disposeSubject.OnNext(disposeSubject.Value + 1);
            }, onError: e => firstError ??= e.Message);

            disposeBag.Add(disposableSP);

            var opDisposable = Observable.Interval(TimeSpan.FromMilliseconds(10))
                .SelectMany(async op =>
                {
                    switch (op)
                    {
                        case 10:
                            await BulkAddOp();
                            break;
                        case 20:
                            expectedCountP++;
                            await PersonAddOp();
                            break;
                        case 30:
                            await StudentAddOp();
                            break;
                        case 40:
                            expectedCountP++;
                            await PersonPutOp();
                            break;
                        case 50:
                            await FailedTransactionOp();
                            break;
                        default:
                            break;
                    }

                    return Unit.Default;
                })
            .TakeWhile(_ => !_exThrown)
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
                       if (_exThrown || firstError is not null)
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

            if (executionCountP != 4)
            {
                throw new InvalidOperationException($"executionCountP : {executionCountP}-> LiveQuery failed.");
            }

            if (executionCountS != 2)
            {
                throw new InvalidOperationException($"executionCountS: {executionCountS} -> LiveQuery failed.");
            }

            if (executionCountSP != 5)
            {
                throw new InvalidOperationException($"executionCountSP: {executionCountSP} -> LiveQuery failed.");
            }

            if (!_exThrown)
            {
                throw new InvalidOperationException("exThrown -> LiveQuery failed.");
            }

            return "OK";
        }
    }
}
