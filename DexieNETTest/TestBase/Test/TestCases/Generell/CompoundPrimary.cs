using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class CompoundPrimary : DexieTest<TestDB>
    {
        public CompoundPrimary(TestDB db) : base(db)
        {
        }

        public override string Name => "CompoundPrimary";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.PersonCompounds;
            await table.Clear();

            var persons = DataGenerator.GetPersonCompounds();
            IEnumerable<(string FirstName, string LastName)> personKeys = await table.BulkAdd(persons, true);

            var personData = persons.Select(p => p.LastName);
            personKeys = await table.ToCollection().Keys();

            if (!personData.SequenceEqual(personKeys.Select(k => k.LastName)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var person2 = await table.Where(table.PrimaryKey).Equal(("First2", "Last2")).First();

            if (person2?.LastName != "Last2")
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var person2V = await table.Where(p => p.FirstName).Equal("First2").First();

            if (person2V?.LastName != "Last2")
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var person2G = await table.Get(("First2", "Last2"));

            if (person2G?.LastName != "Last2")
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var person2BG = await table.BulkGet(new[] { ("First2", "Last2") });

            if (person2BG?.FirstOrDefault()?.LastName != "Last2")
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);

                personKeys = await table.ToCollection().Keys();
                person2 = await table.Where(table.PrimaryKey).Equal(("First2", "Last2")).First();
                person2G = await table.Get(("First2", "Last2"));
                person2BG = await table.BulkGet(new[] { ("First2", "Last2") });
            });

            if (!personData.SequenceEqual(personKeys.Select(k => k.LastName)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (person2?.LastName != "Last2")
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (person2G?.LastName != "Last2")
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (person2BG?.FirstOrDefault()?.LastName != "Last2")
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
