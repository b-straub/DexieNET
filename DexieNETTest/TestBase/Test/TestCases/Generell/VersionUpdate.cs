using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class VersionUpdate : DexieTest<TestDB>
    {
        public VersionUpdate(TestDB db) : base(db)
        {
        }

        public override string Name => "VersionUpdate";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Friends;
            await table.Clear();
            await table.BulkAdd(DataGenerator.GetFriend());
            var fs = await table.ToArray();

            try
            {
                table.OrderBy(f => f.ShoeSize);
            }
            catch (Exception ex)
            {
                if (!ex.Message.StartsWith("Can not create WhereClause"))
                {
                    throw new InvalidOperationException("Test object not suitable.");
                }
            }

            DB.Close();
            DB.Version(1.5).Stores<Friend2>();
            await DB.Open();

            var version = DB.Version();
            if (version != 1.5)
            {
                throw new InvalidOperationException("Version not upgraded");
            }

            var table2 = DB.Friends2;

            var f2s = await table2.ToArray();
            var col = table2.Where(f => f.ShoeSize).Above(41);
            var friends2S = await col.ToArray();
            var friends2 = await table2.OrderBy(f => f.ShoeSize).ToArray();

            DB.Close();
            DB.Version(3).Stores<Friend3>().Upgrade(async tx =>
            {
                var table = tx.Friends();

                await table.ToCollection().Modify(f =>
                {
                    var names = f.Name.Split(' ');
                    var ageInfo = new AgeInfo2(DateTime.Now.AddYears(f.AgeInfo.Age));

                    var f3 = new Friend3(names[0], names[1], ageInfo);
                    return f3;
                });
            });
            await DB.Open();

            var table3 = DB.Friends3;
            var friends3 = await table3.ToArray();

            friends3 = await table3.OrderBy(f => f.LastName).Reverse().ToArray();

            if (friends3.First().LastName != "LLL")
            {
                throw new InvalidOperationException("Version not upgraded");
            }

            return "OK";
        }
    }
}
