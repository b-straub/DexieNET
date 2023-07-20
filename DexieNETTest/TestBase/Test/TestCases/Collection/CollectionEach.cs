using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class CollectionEach : DexieTest<TestDB>
    {
        public CollectionEach(TestDB db) : base(db)
        {
        }

        public override string Name => "CollectionEach";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            // Each
            var eachDataNames = persons.Select(p => p.Name);
            List<string> eachNames = new();

            await table.ToCollection().Each(p => eachNames.Add(p.Name));

            if (!eachNames.SequenceEqual(eachDataNames))
            {
                throw new InvalidOperationException("Each items not identical.");
            }

            // EachKey
            var eachDataAge = persons.Select(p => p.Age).Where(a => a > 30).OrderBy(a => a);
            List<int> eachAge = new();

            await table.Where(p => p.Age).Above(30).EachKey(a => eachAge.Add(a));

            if (!eachAge.SequenceEqual(eachDataAge))
            {
                throw new InvalidOperationException("EachKey items not identical.");
            }

            // EachPrimaryKey
            List<ulong> eachPrimaryKeys = new();
            var eachDataPrimaryKeys = (await table.ToArray())
                .Where(p => p.Id is not null)
                .Select(p => (ulong)p.Id!);

            await table.ToCollection().EachPrimaryKey(k => eachPrimaryKeys.Add(k));

            if (!eachPrimaryKeys.SequenceEqual(eachDataPrimaryKeys))
            {
                throw new InvalidOperationException("EachPrimaryKey items not identical.");
            }

            // EachUniqueKey
            var eachDataNamesU = persons.Select(p => p.Name).Distinct().OrderBy(n => n);
            List<string> eachNamesU = new();

            await table.OrderBy(p => p.Name).EachUniqueKey(a => eachNamesU.Add(a));

            if (!eachNamesU.SequenceEqual(eachDataNamesU))
            {
                throw new InvalidOperationException("EachUniqueKey items not identical.");
            }

            eachNames.Clear();
            eachAge.Clear();
            eachPrimaryKeys.Clear();
            eachNamesU.Clear();

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                eachDataPrimaryKeys = (await table.ToArray())
                    .Where(p => p.Id is not null)
                    .Select(p => (ulong)p.Id!);

                var collection = table.ToCollection();
                await collection.Each(p => eachNames.Add(p.Name));

                var whereAge = table.Where(p => p.Age);
                var collectionAge = whereAge.Above(30);
                await collectionAge.EachKey(a => eachAge.Add(a));

                await collection.EachPrimaryKey(i => eachPrimaryKeys.Add(i));

                var collectionNames = table.OrderBy(p => p.Name);
                await collectionNames.EachUniqueKey(n => eachNamesU.Add(n));
            });

            if (!eachNames.SequenceEqual(eachDataNames))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!eachAge.SequenceEqual(eachDataAge))
            {
                throw new InvalidOperationException("EachKey items not identical.");
            }

            if (!eachPrimaryKeys.SequenceEqual(eachDataPrimaryKeys))
            {
                throw new InvalidOperationException("EachPrimaryKey items not identical.");
            }

            if (!eachNamesU.SequenceEqual(eachDataNamesU))
            {
                throw new InvalidOperationException("EachUniqueKey items not identical.");
            }

            return "OK";
        }
    }
}
