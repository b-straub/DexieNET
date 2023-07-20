using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class InAnyRange : DexieTest<TestDB>
    {
        public InAnyRange(TestDB db) : base(db)
        {
        }

        public override string Name => "InAnyRange";


        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);

            var table = DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var youngOldPersonsData = persons
                .Where(p => p.Age is >= 0 and < 18 or >= 60 and < 75)
                .OrderBy(p => p.Age);

            var range1 = new int[] { 0, 18 };
            var range2 = new int[] { 60, 75 };
            var ranges = new int[][] { range1, range2 };

            var youngOldPersons = await table.Where(p => p.Age).InAnyRange(ranges).ToArray();

            if (!youngOldPersons.OrderBy(p => p.Age).SequenceEqual(youngOldPersonsData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var youngOldPersonsDataWithUppper = persons
                .Where(p => p.Age is >= 0 and < 18 or >= 60 and <= 75)
                .OrderBy(p => p.Age);

            var youngOldPersonsWithUppper = await table.Where(p => p.Age).InAnyRange(ranges, true, true).ToArray();

            if (!youngOldPersonsWithUppper.OrderBy(p => p.Age).SequenceEqual(youngOldPersonsDataWithUppper, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (youngOldPersonsData.SequenceEqual(youngOldPersonsDataWithUppper, comparer) ||
               youngOldPersons.SequenceEqual(youngOldPersonsWithUppper, comparer))
            {
                throw new InvalidOperationException("Test Items not suitable.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var whereClause = table.Where(p => p.Age);
                var collection = whereClause.InAnyRange(ranges);
                var collectionWithUpper = whereClause.InAnyRange(ranges, true, true);
                youngOldPersons = await collection.ToArray();
                youngOldPersonsWithUppper = await collectionWithUpper.ToArray();
            });

            if (!youngOldPersons.OrderBy(p => p.Age).SequenceEqual(youngOldPersonsData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!youngOldPersonsWithUppper.OrderBy(p => p.Age).SequenceEqual(youngOldPersonsDataWithUppper, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (youngOldPersonsData.SequenceEqual(youngOldPersonsDataWithUppper, comparer) ||
               youngOldPersons.SequenceEqual(youngOldPersonsWithUppper, comparer))
            {
                throw new InvalidOperationException("Test Items not suitable.");
            }

            return "OK";
        }
    }
}
