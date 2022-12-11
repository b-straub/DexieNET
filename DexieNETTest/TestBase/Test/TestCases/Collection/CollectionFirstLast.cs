using DexieNET;
using System.Reflection.Metadata.Ecma335;

namespace DexieNETTest.TestBase.Test
{
    internal class CollectionFirstLast : DexieTest<TestDB>
    {
        public CollectionFirstLast(TestDB db) : base(db)
        {
        }

        public override string Name => "CollectionFirstLast";

        public override async ValueTask<string?> RunTest()
        {
            PersonComparer comparer = new(true);

            var table = await DB.Person();
            await table.Clear();

            var first = await table.ToCollection().First();
            var last = await table.ToCollection().Last();

            if (first is not null || last is not null)
            {
                throw new InvalidOperationException("Items not null.");
            }

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var personsData = await table.ToArray();

            first = await table.ToCollection().First();
            last = await table.ToCollection().Last();

            if (!comparer.Equals(first, personsData.First()) || !comparer.Equals(last, personsData.Last()))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var firstA = await table.ToCollection().First(t => t?.Age);
            var lastA = await table.ToCollection().Last(t => t?.Age);

            if (firstA != personsData.First().Age || lastA != personsData.Last().Age)
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();

                var collection = await table.ToCollection();

                first = await collection.First();
                last = await collection.Last();

                await table.BulkAdd(persons);
                personsData = await table.ToArray();

                collection = await table.ToCollection();
                first = await collection.First();
                last = await collection.Last();
            });


            if (!comparer.Equals(first, personsData.First()) || !comparer.Equals(last, personsData.Last()))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
