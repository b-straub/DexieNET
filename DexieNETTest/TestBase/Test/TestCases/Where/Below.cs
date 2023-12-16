using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Below(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "Below";

        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);

            var table = DB.Persons;
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var youngerPersonsData = persons.Where(p => p.Age <= 25);
            var youngerPersons = await table.Where(p => p.Age).BelowOrEqual(25).ToArray();

            if (!youngerPersons.SequenceEqual(youngerPersonsData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var youngPersonsData = persons.Where(p => p.Age < 25);
            var youngPersons = await table.Where(p => p.Age).Below(25).ToArray();

            if (!youngPersons.SequenceEqual(youngPersonsData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (youngerPersonsData.SequenceEqual(youngPersonsData, comparer) ||
                youngerPersons.SequenceEqual(youngPersons, comparer))
            {
                throw new InvalidOperationException("Test Items not suitable.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var whereClause = table.Where(p => p.Age);
                var collectionYoung = whereClause.Below(25);
                var collectionYounger = whereClause.BelowOrEqual(25);

                youngPersons = await collectionYoung.ToArray();
                youngerPersons = await collectionYounger.ToArray();
            });

            if (!youngerPersons.SequenceEqual(youngerPersonsData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!youngPersons.SequenceEqual(youngPersonsData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (youngerPersonsData.SequenceEqual(youngPersonsData, comparer) ||
                youngerPersons.SequenceEqual(youngPersons, comparer))
            {
                throw new InvalidOperationException("Test Items not suitable.");
            }

            return "OK";
        }
    }
}
