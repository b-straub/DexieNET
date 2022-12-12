using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class NotEqual : DexieTest<TestDB>
    {
        public NotEqual(TestDB db) : base(db)
        {
        }

        public override string Name => "NotEqual";

        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);

            var table = await DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var personsDataAge = persons.Where(p => p.Age != 11).OrderBy(p => p.Age);
            var personsAge = await table.Where(p => p.Age).NotEqual(11).ToArray();

            if (!personsAge.OrderBy(p => p.Age).SequenceEqual(personsDataAge, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var personsDataName = persons.Where(p => p.Name != "Person1").OrderBy(p => p.Name);
            var personsName = await table.Where(p => p.Name).NotEqual("Person1").ToArray();

            if (!personsName.OrderBy(p => p.Name).SequenceEqual(personsDataName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);

                var whereClauseAge = await table.Where(p => p.Age);
                var whereClauseName = await table.Where(p => p.Name);

                var collectionAge = await whereClauseAge.NotEqual(11);
                var collectionName = await whereClauseName.NotEqual("Person1");

                personsAge = await collectionAge.ToArray();
                personsName = await collectionName.ToArray();
            });

            if (!personsAge.OrderBy(p => p.Age).SequenceEqual(personsDataAge, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!personsName.OrderBy(p => p.Name).SequenceEqual(personsDataName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
