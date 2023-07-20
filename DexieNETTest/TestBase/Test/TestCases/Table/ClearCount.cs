using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class ClearCount : DexieTest<TestDB>
    {
        public ClearCount(TestDB db) : base(db)
        {
        }

        public override string Name => "ClearCount";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons();
            var persons = DataGenerator.GetPersons();
            await table.Clear();

            await table.BulkAdd(persons);

            var countS = await table.Count(c => c.ToString());
            if (countS != persons.Count().ToString())
            {
                throw new InvalidOperationException("Count failed.");
            }

            var countAS = await table.Where(p => p.Age).Above(40).Count(c => c.ToString());
            if (countAS != persons.Where(p => p.Age > 40).Count().ToString())
            {
                throw new InvalidOperationException("Count failed.");
            }

            await table.Clear();

            var res = await table.Count();

            if (res != 0)
            {
                throw new InvalidOperationException("Clear failed.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                await table.Clear(); // for yet unknown reasons when using Playwright with Webkit clear will not work here
                res = await table.Count();
            });

            if (res != 0)
            {
                throw new InvalidOperationException($"Transaction Clear failed E: 0, A: {res}.");
            }

            return "OK";
        }
    }
}
