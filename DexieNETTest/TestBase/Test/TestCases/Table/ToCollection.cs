﻿using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class ToCollection : DexieTest<TestDB>
    {
        public ToCollection(TestDB db) : base(db)
        {
        }

        public override string Name => "ToCollection";

        public override async ValueTask<string?> RunTest()
        {
            var table = await DB.Persons();
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
                var collection = await table.ToCollection();
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
