using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class OrderBy : DexieTest<TestDB>
    {
        public OrderBy(TestDB db) : base(db)
        {
        }

        public override string Name => "OrderBy";

        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);

            var table = await DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var sortedPersonsDataA = persons.OrderBy(p => p.Age);
            var sortedPersonsDataT = persons.OrderBy(p => p.Tags.Aggregate("", (current, next) => current + next)).ToArray();
            var sortedPersons = await table.OrderBy(p => p.Age).ToArray();
            var sortedPersonsASB = await table.ToCollection().SortBy(p => p.Age);
            var sortedPersonsTSB = await table.ToCollection().SortBy(p => p.Tags);
            var sortedPersonsNSB = await table.Where(p => p.Age).Above(30).SortBy(p => p.Name);

            if (!sortedPersons.SequenceEqual(sortedPersonsDataA, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!sortedPersonsASB.SequenceEqual(sortedPersonsDataA, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!sortedPersonsTSB.SequenceEqual(sortedPersonsDataT, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var sortedPersonsNameAgeData = persons.OrderBy(p => p.Name).ThenBy(p => p.Age);
            var sortedPersonsNameAge = await table.OrderBy(p => p.Name, p => p.Age).ToArray();

            if (!sortedPersonsNameAge.SequenceEqual(sortedPersonsNameAgeData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var sortedTagsData = persons.SelectMany(p => p.Tags).OrderBy(s => s);
            var sortedTags = await table.OrderBy(p => p.Tags).Keys();

            if (!sortedTagsData.SequenceEqual(sortedTags))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var collectionAge = await table.OrderBy(p => p.Age);
                sortedPersons = await collectionAge.ToArray();
                sortedPersonsASB = await table.ToCollection().SortBy(p => p.Age);

                var collectionNameAge = await table.OrderBy(p => p.Name, p => p.Age);
                sortedPersonsNameAge = await collectionNameAge.ToArray();

                var collectionTags = await table.OrderBy(p => p.Tags);
                sortedTags = await collectionTags.Keys();
            });

            if (!sortedPersons.SequenceEqual(sortedPersonsDataA, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!sortedPersonsASB.SequenceEqual(sortedPersonsDataA, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!sortedPersonsNameAge.SequenceEqual(sortedPersonsNameAgeData, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!sortedTags.SequenceEqual(sortedTags))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
