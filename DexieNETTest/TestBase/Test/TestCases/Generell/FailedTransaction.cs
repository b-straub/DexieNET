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
            var table = await DB.Person();
            await table.Add(DataGenerator.GetPerson1());
            var count = await table.Count();

            if (count != 1)
            {
                throw new InvalidOperationException("Failed transaction wrong preparation.");
            }

            bool exThrown = false;

            try
            {
                await DB.Transaction(async collectMode =>
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
            exThrown = false;

            try
            {
                await DB.Transaction(async _ =>
                {
                    await table.Clear();
                    var personKeys = await table.BulkAdd(persons, true);
                    var personValues = await table.BulkGet(personKeys);
                    await table.Add(personValues.FirstOrDefault());
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

            return "OK";
        }
    }
}
