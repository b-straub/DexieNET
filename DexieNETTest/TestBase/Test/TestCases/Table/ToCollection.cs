using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class ToCollection(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "ToCollection";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons;
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);
            var count = await table.ToCollection().Count();

            if (count != persons.Count())
            {
                throw new InvalidOperationException("Items count not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var collection = table.ToCollection();
                count = await collection.Count();
            });

            if (count != persons.Count())
            {
                throw new InvalidOperationException("Items count not identical.");
            }

            return "OK";
        }
    }
}
