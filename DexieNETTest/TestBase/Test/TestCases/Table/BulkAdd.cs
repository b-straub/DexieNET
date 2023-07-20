using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class BulkAdd : DexieTest<TestDB>
    {
        public BulkAdd(TestDB db, bool allKeys = false) : base(db)
        {
            AllKeys = allKeys;
        }

        public override string Name => AllKeys ? "BulkAddAllKeys" : "BulkAdd";

        public bool AllKeys { get; private set; }

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            var keys = await table.BulkAdd(persons, AllKeys);

            if (persons.Count() != DataGenerator.GetPersons().Count())
            {
                throw new InvalidOperationException("Count mismatch.");
            }

            if (AllKeys)
            {
                if (keys.Count() != DataGenerator.GetPersons().Count())
                {
                    throw new InvalidOperationException("Keys mismatch.");
                }
            }

            var personsAdded = (await table.ToArray());

            if (!persons.SequenceEqual(personsAdded, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons, AllKeys);
            });

            if (persons.Count() != DataGenerator.GetPersons().Count())
            {
                throw new InvalidOperationException("Count mismatch Transaction.");
            }

            if (AllKeys)
            {
                if (keys?.Count() != DataGenerator.GetPersons().Count())
                {
                    throw new InvalidOperationException("Keys mismatch Transaction.");
                }
            }

            personsAdded = (await table.ToArray());

            if (!persons.SequenceEqual(personsAdded, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical Transaction.");
            }

            return "OK";
        }
    }
}
