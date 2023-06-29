using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class TransactionAwaited : DexieTest<TestDB>
    {
        public TransactionAwaited(TestDB db, bool fail = false) : base(db)
        {
            Fail = fail;
        }

        public override string Name => Fail ? "TransactionAwaitedFail" : "TransactionAwaited";

        public bool Fail { get; private set; }

        public override async ValueTask<string?> RunTest()
        {
            var table = await DB.Persons();
            await table.Clear();

            bool exThrown = false;

            try
            {
                await DB.Transaction(async ta =>
                {
                    await table.Clear();
                    await ta.WaitFor(async () =>
                    {
                        await Task.Delay(200);
                    });
                    await table.Add(DataGenerator.GetPerson1());

                    await DB.Transaction(async _ =>
                    {
                        var add = false;

                        await ta.WaitFor(async () =>
                        {
                            await Task.Delay(100);
                            add = true;
                        });

                        if (ta.Collecting || add)
                        {
                            await table.Add(DataGenerator.GetPerson2());
                        }

                        add = true;

                        await ta.WaitFor(async () =>
                        {
                            await Task.Delay(100);
                            add = false;
                            if (!ta.Collecting && Fail)
                            {
                                throw new InvalidOperationException("Provoke Fail.");
                            }
                        });

                        if (ta.Collecting || add)
                        {
                            await table.Add(DataGenerator.GetPerson3());
                        }
                    }
                    );
                });
            }
            catch (Exception ex)
            {
                exThrown = ex.GetType() == typeof(TransactionException);
            }

            var count = await table.Count();

            if (Fail)
            {
                if (!exThrown || await table.Count() != 0)
                {
                    throw new InvalidOperationException("Failed Transaction with WaitFor failed.");
                }
            }
            else
            {
                if (exThrown || count != 2)
                {
                    throw new InvalidOperationException("Transaction with WaitFor failed.");
                }
            }

            await table.Clear();

            try
            {
                await DB.Transaction(async _ =>
                {
                    await DB.Transaction(async ta =>
                    {
                        await ta.WaitFor(async () =>
                        {
                            await Task.Delay(200);
                        });
                        await table.Add(DataGenerator.GetPerson1());
                    }, TAType.TopLevel);

                    await DB.Transaction(async ta =>
                    {
                        await ta.WaitFor(async () =>
                        {
                            await Task.Delay(200);
                            if (!ta.Collecting && Fail)
                            {
                                throw new InvalidOperationException("Provoke Fail.");
                            }
                        });
                        await table.Add(DataGenerator.GetPerson2());
                    }, TAType.TopLevel);
                }, TAType.Parallel);
            }
            catch (Exception ex)
            {
                exThrown = ex.GetType() == typeof(TransactionException);
            }

            count = await table.Count();

            if (Fail)
            {
                if (!exThrown || await table.Count() != 1)
                {
                    throw new InvalidOperationException("Failed Parallel Transaction with WaitFor failed.");
                }
            }
            else
            {
                if (exThrown || count != 2)
                {
                    throw new InvalidOperationException("Parallel Transaction with WaitFor failed.");
                }
            }

            return "OK";
        }
    }
}
