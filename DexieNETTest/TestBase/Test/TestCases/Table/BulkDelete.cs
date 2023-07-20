using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class BulkDelete : DexieTest<TestDB>
    {
        public BulkDelete(TestDB db) : base(db)
        {
        }

        public override string Name => "BulkDelete";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            var keys = await table.BulkAdd(persons, true);

            var evenKeys = keys.Where(i => i % 2 == 0);
            var oddKeys = keys.Where(i => i % 2 != 0);

            await table.BulkDelete(evenKeys);

            var keysRemain = (await table.ToArray())
                .Select(p => p.Id)
                .Where(i => i is not null)
                .Select(i => (ulong)i!);

            if (!Enumerable.SequenceEqual(oddKeys, keysRemain))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                keys = await table.BulkAdd(persons, true);

                evenKeys = keys.Where(i => i % 2 == 0);
                await table.BulkDelete(evenKeys);

                keysRemain = (await table.ToArray())
                .Select(p => p.Id)
                .Where(i => i is not null)
                .Select(i => (ulong)i!);
            });

            oddKeys = keys.Where(i => i % 2 != 0);

            if (!Enumerable.SequenceEqual(oddKeys, keysRemain))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
