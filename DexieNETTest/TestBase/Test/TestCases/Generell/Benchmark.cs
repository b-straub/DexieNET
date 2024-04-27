using DexieNET;
using System.Diagnostics;

namespace DexieNETTest.TestBase.Test
{
    internal class Benchmark(TestDB db, CancellationToken cancellationToken) : DexieTest<TestDB>(db)
    {
        private readonly CancellationToken _cancellationToken = cancellationToken;

        public override string Name => "Benchmark";

        public override async ValueTask<string?> RunTest()
        {
            Stopwatch sw = new();
            sw.Start();

            PersonComparer comparer = new(true);

            var table = DB.Persons;
            var persons = DataGenerator.GetPersonsRandom(20000);

            /*await table.Clear();

            
            await table.BulkAdd(persons);
            await table.BulkAdd(DataGenerator.GetPersons());

            sw.Stop();
            var addTime = sw.ElapsedMilliseconds;

            sw.Restart();
            var col = await table.Where(p => p.Name).StartsWith("A");
            var count = await col.Count();
            var names = await col.Clone().Offset(count > 10 ? count - 10 : 0).Limit(5).ToArray();

            sw.Stop();
            var swTime = sw.ElapsedMilliseconds;
            sw.Restart();*/

            var addTime = 0L;
            var count = 0d;
            var names = Enumerable.Empty<Person>();

            try
            {
                await DB.Transaction(async tx =>
                {
                    await table.Clear();

                    foreach (var personChunk in persons.Chunk(1000))
                    {
                        await table.BulkAdd(personChunk);
                        if (_cancellationToken.IsCancellationRequested)
                        {
                            tx?.Abort();
                        }
                    }

                    count = await table.Count();
                    sw.Stop();
                    addTime = sw.ElapsedMilliseconds;

                    sw.Restart();
                    var whereClauseName = table.Where(p => p.Name);

                    var collectionName = whereClauseName.StartsWith("A");
                    count = await collectionName.Count();
                    var collectionNameCloned = collectionName.Clone();
                    collectionNameCloned.Offset(count > 10 ? count - 10 : 0);
                    collectionNameCloned.Limit(5);
                    names = await collectionNameCloned.ToArray();
                });
            }
            catch (Exception ex)
            {
                if (ex.Message.StartsWith("Transaction has already completed"))
                {
                    throw new InvalidOperationException("Benchmark canceled");
                }

                throw new InvalidOperationException(ex.Message);
            }

            sw.Stop();
            var swTime = sw.ElapsedMilliseconds;
            await table.Clear();

            if (!names.SequenceEqual(names, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var namesOut = names.Select(p => p.Name)
                .Aggregate(string.Empty, (current, next) => current + next.ToString() + ", ");

            return $"Add {addTime} ms; Process {swTime} ms; Result: {namesOut}";
        }
    }
}
