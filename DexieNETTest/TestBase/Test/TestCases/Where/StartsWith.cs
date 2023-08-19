using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class StartsWith : DexieTest<TestDB>
    {
        public StartsWith(TestDB db) : base(db)
        {
        }

        public override string Name => "StartsWith";

        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);

            var table = DB.Persons;
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var personsDataName = persons.Where(p => p.Name.StartsWith("A"));
            var personsName = await table.Where(p => p.Name).StartsWith("A").ToArray();

            if (!personsName.SequenceEqual(personsDataName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var personsNameIgnoreCase = await table.Where(p => p.Name).StartsWithIgnoreCase("a").ToArray();

            if (!personsNameIgnoreCase.SequenceEqual(personsDataName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var personsDataNames = persons.Where(p => p.Name.StartsWith("A") || p.Name.StartsWith("P")).OrderBy(p => p.Name);
            var personsNames = await table.Where(p => p.Name).StartsWithAnyOf("A", "P").ToArray();

            if (!personsNames.OrderBy(p => p.Name).SequenceEqual(personsDataNames, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var personsNamesIgnoreCase = await table.Where(p => p.Name).StartsWithAnyOfIgnoreCase("a", "p").ToArray();

            if (!personsNamesIgnoreCase.OrderBy(p => p.Name).SequenceEqual(personsDataNames, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);

                var whereClauseName = table.Where(p => p.Name);

                var collectionName = whereClauseName.StartsWith("A");
                var collectionNames = whereClauseName.StartsWithAnyOf("A", "P");

                personsName = await collectionName.ToArray();
                personsNames = await collectionNames.ToArray();
            });

            if (!personsName.SequenceEqual(personsDataName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!personsNames.OrderBy(p => p.Name).SequenceEqual(personsDataNames, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);

                var whereClauseName = table.Where(p => p.Name);

                var collectionName = whereClauseName.StartsWithIgnoreCase("a");
                var collectionNames = whereClauseName.StartsWithAnyOfIgnoreCase("a", "p");

                personsNameIgnoreCase = await collectionName.ToArray();
                personsNamesIgnoreCase = await collectionNames.ToArray();
            });

            if (!personsNameIgnoreCase.SequenceEqual(personsDataName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!personsNamesIgnoreCase.OrderBy(p => p.Name).SequenceEqual(personsDataNames, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
