using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Reverse(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "Reverse";

        public override async ValueTask<string?> RunTest()
        {
            PersonComparer comparer = new(true);

            var table = DB.Persons;
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var dataReversed = persons.Reverse();
            var reversed = await table.Reverse().ToArray();

            if (!reversed.SequenceEqual(dataReversed, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var dataReversedO = persons.OrderBy(p => p.Age).Reverse();
            var reversedO = await table.OrderBy(p => p.Age).Reverse().ToArray();

            if (!reversedO.SequenceEqual(dataReversedO, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);

                var collection = table.Reverse();
                reversed = await collection.ToArray();

                var collectionAge = table.OrderBy(p => p.Age);
                collectionAge.Reverse();
                reversedO = await collectionAge.ToArray();
            });

            if (!reversed.SequenceEqual(dataReversed, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!reversedO.SequenceEqual(dataReversedO, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
