﻿using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class TransactionsNested(TestDB db, bool fail = false) : DexieTest<TestDB>(db)
    {
        public override string Name => Fail ? "TransactionsNestedFail" : "TransactionsNested";

        public bool Fail { get; private set; } = fail;

        public async Task Log(string message)
        {
            var tableLog = DB.Logentries;

            await DB.Transaction(async _ =>
            {
                await tableLog.Add(new Logentry(message, DateTime.Now));
            }, TAType.TopLevel);
        }

        public override async ValueTask<string?> RunTest()
        {
            var tableLog = DB.Logentries;
            await tableLog.Clear();
            await Log("StartLogging");

            var tableFieldTest = DB.FieldTests;
            var fieldsData = DataGenerator.GetFieldTestRandom().ToArray();

            var tablePersons = DB.Persons;
            await tablePersons.Clear();

            var persons = DataGenerator.GetPersons();
            var personsR1 = DataGenerator.GetPersonsRandom(20, "Test1");
            var personsR2 = DataGenerator.GetPersonsRandom(20, "Test2");
            var personsR3 = DataGenerator.GetPersonsRandom(20, "Test3");

            bool exThrown = false;

            var items = Enumerable.Empty<Person>();
            var itemsFT = Enumerable.Empty<FieldTest>();
            Person? itemU = null;

            double countFT = 0;

            try
            {
                await DB.Transaction(async _ =>
                {
                    await Log("StartTransaction");

                    await tablePersons.Clear(); // for yet unknown reasons when using Playwright with Webkit clear will not work here
                    var keys = await tablePersons.BulkAdd(persons);
                    var collection = tablePersons.ToCollection();
                    var count1 = await collection.Count();

                    await DB.Transaction(async _ =>
                    {
                        await tableFieldTest.Clear(); // for yet unknown reasons when using Playwright with Webkit clear will not work here
                        await tableFieldTest.BulkAdd(fieldsData);
                        var collection = tableFieldTest.ToCollection();

                        await DB.Transaction(async _ =>
                        {
                            countFT = await collection.Count();
                        });

                        itemsFT = await collection.ToArray();
                    });

                    await DB.Transaction(async transaction =>
                    {
                        var collection = tablePersons.ToCollection();
                        var count2 = await collection.Count();
                        var item = await tablePersons.Get(keys.FirstOrDefault());

                        if (item is null && !transaction.Collecting)
                        {
                            transaction.Abort();
                            throw new ArgumentNullException(nameof(item));
                        }

                        await DB.Transaction(async _ =>
                        {
                            await tablePersons.BulkAdd(personsR1);
                            var count3 = await collection.Count();
                            await Log($"Count: {count3}");

                            var keyU = await tablePersons.Put(transaction.Collecting ? item : item! with { Name = "Updated" });

                            itemU = await tablePersons.Get(keyU);

                            if (Fail)
                            {
                                await tablePersons.Add(itemU);
                            }
                            await Log($"Updated ID: {itemU?.Id}");
                        });
                    });

                    items = await collection.ToArray();

                    await Log("EndTransaction");
                });
            }
            catch (Exception ex)
            {
                exThrown = ex.GetType() == typeof(TransactionException);
            }

            await Log("EndLogging");

            if (itemsFT.Count() != countFT)
            {
                throw new InvalidOperationException("Inner nested Transaction not executed.");
            }

            var logs = await tableLog.ToArray();

            if (Fail)
            {
                items = await tablePersons.ToArray();

                if (!exThrown || items.Any() || logs.Count() != 4)
                {
                    throw new InvalidOperationException("Failed nested Transaction executed.");
                }
            }
            else if (exThrown || !items.Any() || itemU?.Name != "Updated" || logs.Count() != 6)
            {
                throw new InvalidOperationException("Nested Transaction not executed.");
            }

            return "OK" + logs.Select(l => l.Message).Aggregate(string.Empty, (curr, next) => curr + ", " + next);
        }
    }
}
