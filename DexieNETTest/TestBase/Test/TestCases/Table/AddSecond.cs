using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class AddSecond : DexieTest<SecondDB>
    {
        public AddSecond(SecondDB db) : base(db)
        {
        }

        public override string Name => "AddSecond";

        public override async ValueTask<string?> RunTest()
        {
            var table = await DB.TestStore<int>();
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
