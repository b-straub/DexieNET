using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Delete(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "Delete";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons;
            await table.Clear();

            var person = DataGenerator.GetPerson1();

            var key = await table.Add(person);
            await table.Delete(key);

            var res = await table.Count();

            if (res != 0)
            {
                throw new InvalidOperationException("Delete failed.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                key = await table.Add(person);
                await table.Delete(key);
            });

            res = await table.Count();

            if (res != 0)
            {
                throw new InvalidOperationException("Delete failed.");
            }

            return "OK";
        }
    }
}
