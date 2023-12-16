using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class AddSecond(SecondDB db) : DexieTest<SecondDB>(db)
    {
        public override string Name => "AddSecond";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.TestStore<int>();
            await table.Clear();

            var key = await table.Add(new TestStore("Test"), 1);
            var testAdded = (await table.ToArray()).FirstOrDefault();

            if (key != 1 || testAdded?.Name != "Test")
            {
                throw new InvalidOperationException("Item not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                key = await table.Add(new TestStore("Test"), 1);
            });

            testAdded = (await table.ToArray()).FirstOrDefault();

            if (key != 1 || testAdded?.Name != "Test")
            {
                throw new InvalidOperationException("Item not identical.");
            }

            return "OK";
        }
    }
}
