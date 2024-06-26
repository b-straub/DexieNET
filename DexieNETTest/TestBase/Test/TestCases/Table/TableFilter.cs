﻿using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class TableFilter(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "TableFilter";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons;
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var oldPersonsData = persons.Where(p => p.Age > 30);
            var col = table.Filter(p => p.Age > 30);
            var oldPersons = await col.ToArray();
            var oldPersonsCount = await col.Count();

            if (!oldPersons.SequenceEqual(oldPersonsData, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (oldPersonsCount != oldPersonsData.Count())
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var collection = table.Filter(p => p.Age > 30);
                oldPersons = await collection.ToArray();
                oldPersonsCount = await collection.Count();
            });

            if (!oldPersons.SequenceEqual(oldPersonsData, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (oldPersonsCount != oldPersonsData.Count())
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
