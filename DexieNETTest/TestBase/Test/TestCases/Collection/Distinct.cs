using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Distinct(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "Distinct";

        public override async ValueTask<string?> RunTest()
        {
            PersonComparer comparer = new(true);

            var table = DB.Persons;
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var query = new[] { "Friend", "Buddy" };

            var dataFriends = persons.Where(p => p.Tags.Contains("Friend"));
            var dataBuddys = persons.Where(p => p.Tags.Contains("Buddy"));

            var dataNoDistinct = dataBuddys.Concat(dataFriends);
            var noDistinct = await table.Where(p => p.Tags).AnyOf(query).ToArray();

            if (!dataNoDistinct.SequenceEqual(noDistinct, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var dataDistinct = dataNoDistinct.Distinct();
            var distinct = await table.Where(p => p.Tags).AnyOf(query).Distinct().ToArray();

            if (!dataDistinct.SequenceEqual(distinct, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (noDistinct.Count() == distinct.Count())
            {
                throw new InvalidOperationException("Items not suitable for test.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);

                var where = table.Where(p => p.Tags);

                var collection = where.AnyOf(query);
                noDistinct = await collection.ToArray();

                var collectionD = where.AnyOf(query);
                collectionD.Distinct();
                distinct = await collectionD.ToArray();
            });

            if (!dataNoDistinct.SequenceEqual(noDistinct, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!dataDistinct.SequenceEqual(distinct, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
