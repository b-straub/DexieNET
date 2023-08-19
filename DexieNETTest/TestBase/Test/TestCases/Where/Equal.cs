using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Equal : DexieTest<TestDB>
    {
        public Equal(TestDB db) : base(db)
        {
        }

        public override string Name => "Equal";

        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);

            var table = DB.Persons;
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var personsDataAge = persons.Where(p => p.Age == 11);
            var personsAge = await table.Where(p => p.Age).Equal(11).ToArray();

            if (!personsAge.SequenceEqual(personsDataAge, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var personHN = persons.First().Address.Housenumber;
            var personsHN = await table.Where(p => p.Address.Housenumber).Equal(personHN).ToArray();

            if (personHN != personsHN.First().Address.Housenumber)
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var personsDataName = persons.Where(p => p.Name == "Person1");
            var personsName = await table.Where(p => p.Name).EqualIgnoreCase("person1").ToArray();

            if (!personsName.SequenceEqual(personsDataName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var personsDataAgeNotIndexed = persons.Where(p => p.Age == 11 && p.NotIndexed == "NotIndexed");
            var personsAgeNotIndexed = await table.Where(p => p.Age, 11, p => p.NotIndexed, "NotIndexed").ToArray();

            if (!personsDataAgeNotIndexed.SequenceEqual(personsAgeNotIndexed, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);

                var whereClauseAge = table.Where(p => p.Age);
                var whereClauseName = table.Where(p => p.Name);

                var collectionAge = whereClauseAge.Equal(11);
                var collectionName = whereClauseName.EqualIgnoreCase("person1");

                personsAge = await collectionAge.ToArray();
                personsName = await collectionName.ToArray();
                personsHN = await table.Where(p => p.Address.Housenumber).Equal(personHN).ToArray();
                personsAgeNotIndexed = await table.Where(p => p.Age, 11, p => p.NotIndexed, "NotIndexed").ToArray();
            });

            if (!personsAge.SequenceEqual(personsDataAge, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (personHN != personsHN.First().Address.Housenumber)
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!personsName.SequenceEqual(personsDataName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!personsDataAgeNotIndexed.SequenceEqual(personsAgeNotIndexed, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
