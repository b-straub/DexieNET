using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Between : DexieTest<TestDB>
    {
        public Between(TestDB db) : base(db)
        {
        }

        public override string Name => "Between";

        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);

            var table = DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var youngOldPersonsData = persons.Where(p => p.Age is >= 60 and < 75).OrderBy(p => p.Age);
            var youngOldPersons = await table.Where(p => p.Age).Between(60, 75).ToArray();

            if (!youngOldPersons.OrderBy(p => p.Age).SequenceEqual(youngOldPersonsData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var youngOldPersonsDataWithUppper = persons.Where(p => p.Age is >= 60 and <= 75).OrderBy(p => p.Age);
            var youngOldPersonsWithUppper = await table.Where(p => p.Age).Between(60, 75, true, true).ToArray();

            if (!youngOldPersonsWithUppper.OrderBy(p => p.Age).SequenceEqual(youngOldPersonsDataWithUppper, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (youngOldPersonsData.SequenceEqual(youngOldPersonsDataWithUppper, comparer) ||
               youngOldPersons.SequenceEqual(youngOldPersonsWithUppper, comparer))
            {
                throw new InvalidOperationException("Test Items not suitable.");
            }

            var keyLow = ("Person7", 30);
            var keyHigh = ("Person7", 70);

            var person7NameAgeData = persons.Where(p => p.Name == "Person7" && p.Age >= 30 && p.Age < 70).OrderBy(p => p.Age);
            var person7NameAge = await table.Where(p => p.Name, p => p.Age).Between(keyLow, keyHigh).ToArray();

            if (!person7NameAge.OrderBy(p => p.Age).SequenceEqual(person7NameAgeData, comparer))
            {
                throw new InvalidOperationException("Test Items not suitable.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var whereClause = table.Where(p => p.Age);
                var collection = whereClause.Between(60, 75);
                var collectionWithUpper = whereClause.Between(60, 75, true, true);
                youngOldPersons = await collection.ToArray();
                youngOldPersonsWithUppper = await collectionWithUpper.ToArray();

                var whereClauseNameAge = table.Where(p => p.Name, p => p.Age);
                var collectionNameAge = whereClauseNameAge.Between(keyLow, keyHigh);
                person7NameAge = await collectionNameAge.ToArray();
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

            if (!person7NameAge.OrderBy(p => p.Age).SequenceEqual(person7NameAgeData, comparer))
            {
                throw new InvalidOperationException("Test Items not suitable.");
            }

            return "OK";
        }
    }
}
