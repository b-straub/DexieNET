using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class CollectionUntil : DexieTest<TestDB>
    {
        public CollectionUntil(TestDB db) : base(db)
        {
        }

        public override string Name => "CollectionUntil";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons;
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            // Until
            var untilData = persons.Select(p => p.Age).Where(p => p < 65).OrderBy(p => p).ToArray();
            var untilDataI = persons.Select(p => p.Age).Where(p => p <= 65).OrderBy(p => p).ToArray();
            var untilDataNA = persons.OrderBy(p => p.Name).ThenBy(p => p.Age)
                .TakeWhile(p => p.Name != "Person8").TakeWhile(p => p.Age <= 60).ToArray();

            List<int> until = new();
            List<int> untilI = new();

            var untilAge = await table.OrderBy(p => p.Age).Until(p => p.Age >= 65).ToArray();
            var untilAgeI = await table.OrderBy(p => p.Age).Until(p => p.Age >= 65, true).ToArray();
            var untilNA = await table.OrderBy(p => p.Name, p => p.Age).Until(p => p.Age > 60).ToArray();

            until.AddRange(untilAge.Select(p => p.Age));
            untilI.AddRange(untilAgeI.Select(p => p.Age));

            if (!until.SequenceEqual(untilData))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!untilI.SequenceEqual(untilDataI))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!untilNA.SequenceEqual(untilDataNA, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (until.Count == untilI.Count)
            {
                throw new InvalidOperationException("Test Items not suitable.");
            }

            until.Clear();
            untilI.Clear();

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var collection = table.OrderBy(p => p.Age);
                collection = collection.Until(p => p.Age >= 65);
                untilAge = await collection.ToArray();

                var collectionI = table.OrderBy(p => p.Age);
                collectionI = collectionI.Until(p => p.Age >= 65, true);
                untilAgeI = await collectionI.ToArray();

                var collectionNA = table.OrderBy(p => p.Name, p => p.Age);
                collectionNA = collectionNA.Until(p => p.Age > 60);
                untilNA = await collectionNA.ToArray();

            });

            until.AddRange(untilAge.Select(p => p.Age));
            untilI.AddRange(untilAgeI.Select(p => p.Age));

            if (!until.SequenceEqual(untilData))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!untilI.SequenceEqual(untilDataI))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!untilNA.SequenceEqual(untilDataNA, new PersonComparer(true)))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (until.Count == untilI.Count)
            {
                throw new InvalidOperationException("Test Items not suitable.");
            }

            return "OK";
        }
    }
}
