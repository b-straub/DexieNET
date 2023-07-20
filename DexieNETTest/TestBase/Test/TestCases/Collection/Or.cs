using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Or : DexieTest<TestDB>
    {
        public Or(TestDB db) : base(db)
        {
        }

        public override string Name => "Or";

        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);

            var table = DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var personsDataAgeName = persons.Where(p => p.Age == 11 || p.Name == "BPerson5");
            var personsAgeName = await table.Where(p => p.Age).Equal(11).Or(p => p.Name).Equal("BPerson5").ToArray();

            if (!personsAgeName.SequenceEqual(personsDataAgeName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var personsDataAgeNameKeys = persons.Where(p => p.Name == "Person1" || p.Age == 65).Select(p => p.Age);
            var personsNameAgeNameCollection = table.Where(p => p.Age).Equal(65).Or(p => p.Name).EqualIgnoreCase("person1");

            var personsNameAgeNameKeys = await personsNameAgeNameCollection.Keys(p => p.Age);

            if (!personsDataAgeNameKeys.Intersect(personsNameAgeNameKeys).Any())
            {
                throw new InvalidOperationException("Items not identical.");
            }

            int exceptions = 0;

            try
            {
                await personsNameAgeNameCollection.Keys();
            }
            catch
            {
                exceptions++;
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);

                personsAgeName = await table.Where(p => p.Age).Equal(11).Or(p => p.Name).Equal("BPerson5").ToArray();
                personsNameAgeNameKeys = await table.Where(p => p.Age).Equal(65).Or(p => p.Name).EqualIgnoreCase("person1").Keys(p => p.Age);

                try
                {
                    await personsNameAgeNameCollection.Keys();
                }
                catch
                {
                    exceptions++;
                }
            });

            if (!personsAgeName.SequenceEqual(personsDataAgeName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!personsDataAgeNameKeys.Intersect(personsNameAgeNameKeys).Any())
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (exceptions != 2)
            {
                throw new InvalidOperationException("Or keys test failed!");
            }

            return "OK";
        }
    }
}
