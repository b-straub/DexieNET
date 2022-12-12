using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class OpenClose : DexieTest<TestDB>
    {
        public OpenClose(TestDB db) : base(db)
        {
        }

        public override string Name => "OpenClose";

        public override async ValueTask<string?> RunTest()
        {
            var table = await DB.Persons();
            await table.Clear(); // open implicitly

            var isOpen = await DB.IsOpen();

            if (!isOpen)
            {
                throw new InvalidOperationException("IsOpen failed.");
            }

            await DB.Close();

            isOpen = await DB.IsOpen();

            if (isOpen)
            {
                throw new InvalidOperationException("Close failed.");
            }

            var newDBInstance = await DB.Open();

            isOpen = await newDBInstance.IsOpen();

            if (!isOpen)
            {
                throw new InvalidOperationException("Open failed.");
            }

            return "OK";
        }
    }
}
