using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class BulkPut : DexieTest<TestDB>
    {
        public BulkPut(TestDB db, bool allKeys = false) : base(db)
        {
            AllKeys = allKeys;
        }

        public override string Name => AllKeys ? "BulkPutAllKeys" : "BulkPut";

        public bool AllKeys { get; private set; }

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            var res = await table.BulkAdd(persons, AllKeys);

            if (persons.Count() != DataGenerator.GetPersons().Count())
            {
                throw new InvalidOperationException("Count mismatch.");
            }

            if (AllKeys)
            {
                if (res.Count() != DataGenerator.GetPersons().Count())
                {
                    throw new InvalidOperationException("Keys mismatch.");
                }
            }

            var personsAdded = (await table.ToArray()).ToList();

            if (!persons.SequenceEqual(personsAdded, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var personsUpdated = personsAdded.Select(x => x with { Name = "Updated" });

            res = await table.BulkPut(personsUpdated, AllKeys);

            if (persons.Count() != DataGenerator.GetPersons().Count())
            {
                throw new InvalidOperationException("Count mismatch.");
            }

            if (AllKeys)
            {
                if (res.Count() != DataGenerator.GetPersons().Count())
                {
                    throw new InvalidOperationException("Keys mismatch.");
                }
            }

            personsAdded = (await table.ToArray()).ToList();

            if (!personsUpdated.SequenceEqual(personsAdded, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                res = await table.BulkPut(personsUpdated, AllKeys);
            });

            if (persons.Count() != DataGenerator.GetPersons().Count())
            {
                throw new InvalidOperationException("Count mismatch Transaction.");
            }

            if (AllKeys)
            {
                if (res.Count() != DataGenerator.GetPersons().Count())
                {
                    throw new InvalidOperationException("Keys mismatch Transaction.");
                }
            }

            personsAdded = (await table.ToArray()).ToList();

            if (!personsUpdated.SequenceEqual(personsAdded, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical Transaction.");
            }

            return "OK";
        }
    }
}
