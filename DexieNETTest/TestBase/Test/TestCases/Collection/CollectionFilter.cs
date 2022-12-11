using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class CollectionFilter : DexieTest<TestDB>
    {
        public CollectionFilter(TestDB db) : base(db)
        {
        }

        public override string Name => "CollectionFilter";

        public override async ValueTask<string?> RunTest()
        {
            var table = await DB.Person();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var oldBuddysData = persons.Where(p => p.Tags.Contains("Buddy") && p.Age > 30);

            var col = await table.ToCollection().Filter(p => p.Tags.Contains("Buddy")).Filter(p => p.Age > 30);
            var oldBuddys = await col.ToArray();
            var oldBuddysCount = await col.Count();

            if (!oldBuddys.SequenceEqual(oldBuddysData, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (oldBuddysCount != oldBuddysData.Count())
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var collection = await table.ToCollection();
                await collection.Filter(p => p.Tags.Contains("Buddy"));
                await collection.Filter(p => p.Age > 30);
                oldBuddysData = await collection.ToArray();
                oldBuddysCount = await collection.Count();
            });

            if (!oldBuddysData.SequenceEqual(oldBuddysData, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (oldBuddysCount != oldBuddysData.Count())
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
