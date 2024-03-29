﻿using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class LimitOffset : DexieTest<TestDB>
    {
        public LimitOffset(TestDB db) : base(db)
        {
        }

        public override string Name => "LimitOffset";

        public override async ValueTask<string?> RunTest()
        {
            const int limit = 5;

            PersonComparer comparer = new(true);

            var table = await DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersonsRandom(limit * 10);
            await table.BulkAdd(persons);

            var dataLimited = persons.Take(limit);
            var limited = await table.Limit(limit).ToArray();

            if (!limited.SequenceEqual(dataLimited, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var dataOffseted = persons.Skip(limit * 9);
            var offseted = await table.Offset(limit * 9).ToArray();

            if (!offseted.SequenceEqual(dataOffseted, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);

                var collection = await table.Limit(limit);
                limited = await collection.ToArray();

                collection = await table.Offset(limit * 9);
                offseted = await collection.ToArray();
            });

            if (!limited.SequenceEqual(dataLimited, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!offseted.SequenceEqual(dataOffseted, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
