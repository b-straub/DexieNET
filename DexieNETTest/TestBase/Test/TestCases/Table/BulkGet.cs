﻿using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class BulkGet(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "BulkGet";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons;
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            var keys = await table.BulkAdd(persons, true);

            var personsAdded = await table.BulkGet(keys);

            PersonComparer pComparer = new(true);

            if (!persons.SequenceEqual(personsAdded, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                keys = await table.BulkAdd(persons, true);
                personsAdded = await table.BulkGet(keys);
            });

            if (personsAdded is null || !persons.SequenceEqual(personsAdded, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
