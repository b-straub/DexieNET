using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class TransactionsParallel(TestDB db, bool fail = false) : DexieTest<TestDB>(db)
    {
        public override string Name => Fail ? "TransactionsParallelFail" : "TransactionsParallel";

        public bool Fail { get; private set; } = fail;

        public override async ValueTask<string?> RunTest()
        {
            var tablePersons = DB.Persons;
            var personsR1 = DataGenerator.GetPersonsRandom(20, "Test1");
            var personsR2 = DataGenerator.GetPersonsRandom(20, "Test2");
            var personsR3 = DataGenerator.GetPersonsRandom(20, "Test3");
            var pList = new List<string>();

            bool exThrown = false;
            var keys = Enumerable.Empty<ulong>();
            Person? item = null;

            if (Fail)
            {
                try
                {
                    await DB.Transaction(async _ =>
                    {
                        await tablePersons.Clear();
                        await tablePersons.BulkAdd(personsR1);

                        await DB.Transaction(async ta =>
                        {
                            await tablePersons.BulkAdd(personsR2);
                        });
                    }, TAType.Parallel);
                }
                catch (Exception ex)
                {
                    exThrown = ex.GetType() == typeof(InvalidOperationException);
                }

                var count = await tablePersons.Count();

                if (!exThrown)
                {
                    throw new InvalidOperationException("Nested parallel transaction executed.");
                }
            }

            exThrown = false;

            try
            {
                await DB.Transaction(async ta =>
                {
                    await tablePersons.Clear();
                    await tablePersons.BulkAdd(personsR1);
                    await tablePersons.BulkAdd(personsR2);
                    keys = await tablePersons.BulkAdd(personsR3);
                });

                await DB.Transaction(async _ =>
                {
                    await DB.Transaction(async _ =>
                    {
                        var wc = tablePersons.Where(t => t.Name);
                        var c = wc.Equal("Test1");
                        await c.EachKey(k => pList.Add(k));
                    }, TAType.TopLevel);

                    await DB.Transaction(async _ =>
                    {
                        var wc = tablePersons.Where(t => t.Name);
                        var c = wc.Equal("Test2");
                        await c.EachKey(k => pList.Add(k));
                    }, TAType.TopLevel);

                    await DB.Transaction(async _ =>
                    {
                        var wc = tablePersons.Where(t => t.Name);
                        var c = wc.Equal("Test3");
                        await c.EachKey(k => pList.Add(k));

                        item = Fail ? await tablePersons.Get(keys.LastOrDefault()) : DataGenerator.GetPerson3();
                        await tablePersons.Add(item);
                    }, TAType.TopLevel);

                }, TAType.Parallel);
            }
            catch (Exception ex)
            {
                exThrown = ex.GetType() == typeof(TransactionException);
            }

            var tableCount = await tablePersons.Count();

            if (Fail)
            {
                if (!exThrown || tableCount != 60)
                {
                    throw new InvalidOperationException("Failed parallel transaction executed.");
                }

                return "OK";
            }

            var test2Index = pList.FindIndex(n => n == "Test2");

            if (exThrown || tableCount != 61)
            {
                throw new InvalidOperationException("Parallel Transactions failed.");
            }

            if (test2Index >= 20)
            {
                return "Transactions not executed in parallel.";
            }

            return "OK";
        }
    }
}
