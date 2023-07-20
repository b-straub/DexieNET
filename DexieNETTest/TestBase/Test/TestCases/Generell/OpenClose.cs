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
            var table = DB.Persons();
            await table.Clear(); // open implicitly

            var isOpen = DB.IsOpen();

            if (!isOpen)
            {
                throw new InvalidOperationException("IsOpen failed.");
            }

            DB.Close();

            isOpen = DB.IsOpen();

            if (isOpen)
            {
                throw new InvalidOperationException("Close failed.");
            }

            var newDBInstance = await DB.Open();

            isOpen = newDBInstance.IsOpen();

            if (!isOpen)
            {
                throw new InvalidOperationException("Open failed.");
            }

            return "OK";
        }
    }
}
