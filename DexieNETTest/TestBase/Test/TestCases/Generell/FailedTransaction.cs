using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class FailedTransaction : DexieTest<TestDB>
    {
        public FailedTransaction(TestDB db) : base(db)
        {
        }

        public override string Name => "FailedTransaction";

        public bool AllKeys { get; set; } = false;

        public override async ValueTask<string?> RunTest()
        {
            var table = await DB.Persons();
            await table.Add(DataGenerator.GetPerson1());
            var count = await table.Count();

            if (count != 1)
            {
                throw new InvalidOperationException("Failed transaction wrong preparation.");
            }

            bool exThrown = false;

            try
            {
                await DB.Transaction(async _ =>
                {
                    await table.Clear();
                    var key = await table.Add(DataGenerator.GetPerson1());
                    var person = await table.Get(key);
                    await table.Add(person);
                });
            }
            catch (Exception ex)
            {
                exThrown = ex.GetType() == typeof(TransactionException);
            }

            count = await table.Count();
            if (!exThrown || count != 1)
            {
                throw new InvalidOperationException("Failed transaction executed.");
            }

            var persons = DataGenerator.GetPersons();

            async Task FailedAdd(IEnumerable<ulong> personKeys, bool ignoreFail)
            {
                await DB.Transaction(async _ =>
                {
                    var personValues = await DB.Persons().BulkGet(personKeys);

                    if (!ignoreFail)
                    {
                        await DB.Persons().Add(personValues.FirstOrDefault());
                    }
                    else
                    {
                        try
                        {
                            await DB.Persons().Add(personValues.FirstOrDefault());
                        }
                        catch (Exception ex)
                        {
                            exThrown = ex.GetType() != typeof(TransactionException);
                        }
                    }
                });
            }

            exThrown = false;
            try
            {
                await DB.Transaction(async _ =>
                {
                    await table.Clear();
                    var personKeys = await table.BulkAdd(persons, true);
                    await FailedAdd(personKeys, false);
                });
            }
            catch (Exception ex)
            {
                exThrown = ex.GetType() == typeof(TransactionException);
            }

            count = await table.Count();

            if (!exThrown || await table.Count() != 1)
            {
                throw new InvalidOperationException("Failed transaction executed.");
            }

            exThrown = false;
            await DB.Transaction(async _ =>
            {
                await table.Clear();
                var personKeys = await table.BulkAdd(persons, true);
                await FailedAdd(personKeys, true);
                await table.Add(DataGenerator.GetPerson3());
            });

            count = await table.Count();

            if (!exThrown || await table.Count() != 12)
            {
                throw new InvalidOperationException("Catched failed transaction not executed.");
            }

            return "OK";
        }
    }
}
