using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class KeyTest : DexieTest<TestDB>
    {
        public KeyTest(TestDB db) : base(db)
        {
        }

        public override string Name => "KeyTest";

        public override async ValueTask<string?> RunTest()
        {
            var tableP = await DB.Persons();

            await tableP.Where(tableP.PrimaryKey);
            await tableP.Where(t => t.Name);
            await tableP.Where(t => t.Age);
            await tableP.Where(t => t.Tags);
            await tableP.Where(t => t.Guid);
            await tableP.Where(t => t.Id);
            await tableP.Where(t => t.Name, t => t.Age);
            await tableP.Where(t => t.Name, t => t.Address.City);
            await tableP.Where(t => t.Address.City, t => t.Address.Street);

            await tableP.Where(t => t.Address.City);
            await tableP.Where(t => t.Address.Street);
            await tableP.Where(t => t.Address.Housenumber);
            await tableP.Where(t => t.Address.ZIP);
            await tableP.Where(t => t.Address.Country);

            await tableP.Where(t => t.Phone.Number.Number);
            await tableP.Where(t => t.Phone.Number.Country);

            var tableF = await DB.FieldTests();

            await tableF.Where(tableF.PrimaryKey);
            await tableF.Where(t => t.Id);
            await tableF.Where(t => t.Include);
            await tableF.Where(t => t.IncludeME);
            await tableF.Where(t => t.DateTime);
            await tableF.Where(t => t.Date);
            await tableF.Where(t => t.Time);
            await tableF.Where(t => t.TimeSpan);
            await tableF.Where(t => t.Blob);
            await tableF.Where(t => t.BlobME);
            await tableF.Where(t => t.Array);

            var tableC = await DB.PersonCompounds();
            await tableC.Where(tableC.PrimaryKey);
            await tableC.Where(p => p.FirstName);
            await tableC.Where(p => p.FirstName, p => p.LastName);

            int exceptions = 0;

            try
            {
                await tableP.Where(t => t.NotIndexed);
            }
            catch
            {
                exceptions++;
            }

            try
            {
                await tableP.Where(t => t.Phone.Type);
            }
            catch
            {
                exceptions++;
            }

            try
            {
                await tableP.Where(t => t.Age, t => t.Name);
            }
            catch
            {
                exceptions++;
            }

            try
            {
                await tableP.Where(t => t.Tags, t => t.Tags);
            }
            catch
            {
                exceptions++;
            }

            try
            {
                await tableC.Where(t => t.LastName);
            }
            catch
            {
                exceptions++;
            }

            if (exceptions != 4)
            {
                throw new InvalidOperationException("EntityKey test failed!");
            }

            return "OK";
        }
    }
}
