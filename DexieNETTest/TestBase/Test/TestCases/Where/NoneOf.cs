using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class NoneOf : DexieTest<TestDB>
    {
        public NoneOf(TestDB db) : base(db)
        {
        }

        public override string Name => "NoneOf";

        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);

            var table = await DB.Person();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var personsDataAge = persons.Where(p => p.Age is not 11 and not 75).OrderBy(p => p.Age);
            var personsAge = await table.Where(p => p.Age).NoneOf(11, 75).ToArray();

            if (!personsAge.OrderBy(p => p.Age).SequenceEqual(personsDataAge, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);

                var whereClauseAge = await table.Where(p => p.Age);
                var collectionAge = await whereClauseAge.NoneOf(11, 75);
                personsAge = await collectionAge.ToArray();
            });

            if (!personsAge.OrderBy(p => p.Age).SequenceEqual(personsDataAge, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
