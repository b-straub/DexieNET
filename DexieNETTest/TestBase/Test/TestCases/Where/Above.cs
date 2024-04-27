using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Above(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "Above";

        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);

            var table = DB.Persons;
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var olderPersonsData = persons.Where(p => p.Age >= 65);
            var olderPersons = await table.Where(p => p.Age).AboveOrEqual(65).ToArray();

            if (!olderPersons.SequenceEqual(olderPersonsData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var oldPersonsData = persons.Where(p => p.Age > 65);
            var oldPersons = await table.Where(p => p.Age).Above(65).ToArray();

            if (!oldPersons.SequenceEqual(oldPersonsData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (olderPersonsData.SequenceEqual(oldPersonsData, comparer) ||
                olderPersons.SequenceEqual(oldPersons, comparer))
            {
                throw new InvalidOperationException("Test Items not suitable.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var whereClause = table.Where(p => p.Age);
                var collectionOld = whereClause.Above(65);
                var collectionOlder = whereClause.AboveOrEqual(65);

                oldPersons = await collectionOld.ToArray();
                olderPersons = await collectionOlder.ToArray();
            });

            if (!olderPersons.SequenceEqual(olderPersonsData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!oldPersons.SequenceEqual(oldPersonsData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (olderPersonsData.SequenceEqual(oldPersonsData, comparer) ||
                olderPersons.SequenceEqual(oldPersons, comparer))
            {
                throw new InvalidOperationException("Test Items not suitable.");
            }

            return "OK";
        }
    }
}
