using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class CollectionPrimaryKeys : DexieTest<TestDB>
    {
        public CollectionPrimaryKeys(TestDB db) : base(db)
        {
        }

        public override string Name => "CollectionPrimaryKeys";

        public override async ValueTask<string?> RunTest()
        {
            var table = await DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var personsData = await table.ToArray();

            var primaryKeysData = personsData
                .Where(p => p.Id is not null)
                .Select(p => (ulong)p.Id!);

            var primaryKeys = await table.ToCollection().PrimaryKeys();

            if (!primaryKeysData.SequenceEqual(primaryKeys))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var primaryKeysDataAge = personsData
               .Where(p => p.Age > 30 && p.Id is not null)
               .Select(p => (ulong)p.Id!)
               .OrderBy(p => p);

            var primaryKeysAge = await table.Where(p => p.Age).Above(30).PrimaryKeys();

            if (!primaryKeysDataAge.SequenceEqual(primaryKeysAge.OrderBy(p => p)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                var collection = await table.ToCollection();
                primaryKeys = await collection.PrimaryKeys();

                var whereClause = await table.Where(p => p.Age);
                var collectionAbove = await whereClause.Above(30);
                primaryKeysAge = await collectionAbove.PrimaryKeys();
            });

            if (!primaryKeysData.SequenceEqual(primaryKeys))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!primaryKeysDataAge.SequenceEqual(primaryKeysAge.OrderBy(p => p)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
