using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class KeyTest : DexieTest<TestDB>
    {
        public KeyTest(TestDB db) : base(db)
        {
        }

        public override string Name => "KeyTest";

        public override ValueTask<string?> RunTest()
        {
            var tableP = DB.Persons;

            tableP.Where(tableP.PrimaryKey);
            tableP.Where(t => t.Name);
            tableP.Where(t => t.Age);
            tableP.Where(t => t.Tags);
            tableP.Where(t => t.Guid);
            tableP.Where(t => t.Id);
            tableP.Where(t => t.Name, t => t.Age);
            tableP.Where(t => t.Name, t => t.Address.City);
            tableP.Where(t => t.Address.City, t => t.Address.Street);

            tableP.Where(t => t.Address.City);
            tableP.Where(t => t.Address.Street);
            tableP.Where(t => t.Address.Housenumber);
            tableP.Where(t => t.Address.ZIP);
            tableP.Where(t => t.Address.Country);

            tableP.Where(t => t.Phone.Number.Number);
            tableP.Where(t => t.Phone.Number.Country);

            var tableF = DB.FieldTests;

            tableF.Where(tableF.PrimaryKey);
            tableF.Where(t => t.Id);
            tableF.Where(t => t.Include);
            tableF.Where(t => t.IncludeME);
            tableF.Where(t => t.DateTime);
            tableF.Where(t => t.Date);
            tableF.Where(t => t.Time);
            tableF.Where(t => t.TimeSpan);
            tableF.Where(t => t.Blob);
            tableF.Where(t => t.BlobME);
            tableF.Where(t => t.Array);

            var tableC = DB.PersonCompounds;
            tableC.Where(tableC.PrimaryKey);
            tableC.Where(p => p.FirstName);
            tableC.Where(p => p.FirstName, p => p.LastName);

            int exceptions = 0;

            try
            {
                tableP.Where(t => t.NotIndexed);
            }
            catch
            {
                exceptions++;
            }

            try
            {
                tableP.Where(t => t.Phone.Type);
            }
            catch
            {
                exceptions++;
            }

            try
            {
                tableP.Where(t => t.Age, t => t.Name);
            }
            catch
            {
                exceptions++;
            }

            try
            {
                tableP.Where(t => t.Tags, t => t.Tags);
            }
            catch
            {
                exceptions++;
            }

            try
            {
                tableC.Where(t => t.LastName);
            }
            catch
            {
                exceptions++;
            }

            if (exceptions != 4)
            {
                throw new InvalidOperationException("EntityKey test failed!");
            }

            return new("OK");
        }
    }
}
