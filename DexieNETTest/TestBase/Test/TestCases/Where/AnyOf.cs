﻿using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class AnyOf : DexieTest<TestDB>
    {
        public AnyOf(TestDB db) : base(db)
        {
        }

        public override string Name => "AnyOf";

        public override async ValueTask<string?> RunTest()
        {
            var comparer = new PersonComparer(true);

            var table = await DB.Person();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var personsDataAge = persons.Where(p => p.Age is 11 or 75);
            var personsAge = await table.Where(p => p.Age).AnyOf(11, 75).ToArray();

            if (!personsAge.SequenceEqual(personsDataAge, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var personsDataName = persons.Where(p => p.Name is "Person1" or "Person6");
            var personsName = await table.Where(p => p.Name).AnyOfIgnoreCase("person1", "person6").ToArray();

            if (!personsName.SequenceEqual(personsDataName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            var personsDataTag = persons.Where(p => p.Tags.Contains("Friend") || p.Tags.Contains("Neighbor")).Distinct().OrderBy(p => p.Name);
            var personsTag = await table.Where(p => p.Tags).AnyOf("Friend", "Neighbor").Distinct().SortBy(p => p.Name);

            if (!personsTag.SequenceEqual(personsDataTag, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);

                var whereClauseAge = await table.Where(p => p.Age);
                var whereClauseName = await table.Where(p => p.Name);

                var collectionAge = await whereClauseAge.AnyOf(11, 75);
                var collectionName = await whereClauseName.AnyOfIgnoreCase("person1", "person6");

                personsAge = await collectionAge.ToArray();
                personsName = await collectionName.ToArray();

                personsTag = personsTag = await table.Where(p => p.Tags).AnyOf("Friend", "Neighbor").Distinct().SortBy(p => p.Name);
            });

            if (!personsAge.SequenceEqual(personsDataAge, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!personsName.SequenceEqual(personsDataName, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            if (!personsTag.SequenceEqual(personsDataTag, comparer))
            {
                throw new InvalidOperationException("Items not identical.");
            }

            return "OK";
        }
    }
}
