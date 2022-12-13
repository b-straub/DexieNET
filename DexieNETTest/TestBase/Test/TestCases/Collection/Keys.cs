using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Keys : DexieTest<TestDB>
    {
        public Keys(TestDB db) : base(db)
        {
        }

        public override string Name => "Keys";

        public override async ValueTask<string?> RunTest()
        {
            var table = await DB.Persons();
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var primaryKeys1 = await table.ToCollection().Keys();
            var primaryKeys2 = await table.OrderBy(table.PrimaryKey).Keys();

            if (!primaryKeys1.SequenceEqual(primaryKeys2))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            var sortedKeys = await table.OrderBy(p => p.Age).Keys();
            var sortedPersons = await table.OrderBy(p => p.Age).ToArray();

            if (sortedPersons.First()?.Age != sortedKeys.First())
            {
                throw new InvalidOperationException("Keys not found.");
            }

            var whereKeys = await table.Where(p => p.Name).Equal("Person1").Keys();

            if (whereKeys.First() != "Person1")
            {
                throw new InvalidOperationException("Keys not found.");
            }

            var indexKeys = await table.ToCollection().Keys();

            if (indexKeys.Count() != persons.Count())
            {
                throw new InvalidOperationException("Keys not found.");
            }

            var uniqueData = persons.Select(p => p.Name).Distinct();
            var uniqueKeys = await table.OrderBy(p => p.Name).UniqueKeys();

            if (uniqueKeys.Count() != uniqueData.Count())
            {
                throw new InvalidOperationException("UniqueKeys not found.");
            }

            static string callbackAge(IEnumerable<int> keys)
            {
                return keys.Aggregate(string.Empty, (prev, curr) => prev.ToString() + curr.ToString());
            }

            var cbData = persons.Select(p => p.Age)
                .OrderBy(a => a)
                .Aggregate(string.Empty, (prev, curr) => prev.ToString() + curr.ToString());

            var cbKeys = await table.OrderBy(p => p.Age).Keys(callbackAge);

            if (cbData != cbKeys)
            {
                throw new InvalidOperationException("Keys not indentical.");
            }

            var cbDataU = persons.Select(p => p.Name)
                .Distinct()
                .Count();

            var cbKeysU = await table.OrderBy(p => p.Name).UniqueKeys(k => k.Count());

            if (cbDataU != cbKeysU)
            {
                throw new InvalidOperationException("UniqueKeys count not indentical.");
            }

            var queryData = persons
                .Where(p => p.Address.City == "TestCity2" && p.Address.Street == "TestStreet2")
                .Select(p => (p.Address.City, p.Address.Street));

            var cityStreetKey = ("TestCity2", "TestStreet2");
            IEnumerable<(string City, string Street)> queryKeys = await table.Where(p => p.Address.City, "TestCity2",
                p => p.Address.Street, "TestStreet2").Keys();

            var (City, Street) = queryKeys.FirstOrDefault();

            if (City != queryData.FirstOrDefault().City)
            {
                throw new InvalidOperationException("Key not found.");
            }

            if (!queryData.SequenceEqual(queryKeys))
            {
                throw new InvalidOperationException("Keys not found.");
            }

            var queryEqualKeys = await table.Where(p => p.Address.City, p => p.Address.Street).Equal(cityStreetKey).Keys();

            if (!queryData.SequenceEqual(queryEqualKeys))
            {
                throw new InvalidOperationException("Keys not found.");
            }

            var queryTags = new[] { "Friend", "Buddy" };

            var dataFriends = persons.Where(p => p.Tags.Contains("Friend"));
            var dataBuddys = persons.Where(p => p.Tags.Contains("Buddy"));

            var dataTags = dataBuddys.Concat(dataFriends).SelectMany(p => p.Tags)
                .Where(t => queryTags.Contains(t)).Distinct()
                .OrderBy(t => t);

            var tagKeys = (await (await table.Where(p => p.Tags).AnyOf(queryTags)).Keys())
                .Distinct().OrderBy(t => t);

            if (!dataTags.SequenceEqual(tagKeys))
            {
                throw new InvalidOperationException("Keys not found.");
            }

            var person6Key = await table.Where(p => p.Name, "Person6", p => p.Age, 75, p => p.Phone.Number.Number, "22222").Keys();

            if (person6Key.FirstOrDefault().Item1 != "Person6")
            {
                throw new InvalidOperationException("Item not identical.");
            }

            var person6 = await table.Where(p => p.Name, p => p.Age, p => p.Phone.Number.Number).Equal(("Person6", 75, "22222")).First();

            if (person6?.Name != "Person6")
            {
                throw new InvalidOperationException("Item not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var collectionAge = await table.OrderBy(p => p.Age);
                sortedKeys = await collectionAge.Keys();

                var whereClauseName = await table.Where(p => p.Name);
                var collectionName = await whereClauseName.Equal("Person1");
                whereKeys = await collectionName.Keys();

                var collectionKeys = await table.ToCollection();
                indexKeys = await collectionKeys.Keys();

                var collectionUniqueName = await table.OrderBy(p => p.Name);
                uniqueKeys = await collectionUniqueName.UniqueKeys();

                var collectionQuery = await table.Where(p => p.Address.City, "TestCity2",
                    p => p.Address.Street, "TestStreet2");
                queryKeys = await collectionQuery.Keys();

                var whereClauseCS = await table.Where(p => p.Address.City, p => p.Address.Street);
                var collectionCS = await whereClauseCS.Equal(cityStreetKey);
                queryEqualKeys = await collectionCS.Keys();

                var whereClauseTags = await table.Where(p => p.Tags);
                var collectionTags = await whereClauseTags.AnyOf(queryTags);
                tagKeys = (await collectionTags.Keys()).Distinct().OrderBy(t => t);

                primaryKeys1 = await table.ToCollection().Keys();
                primaryKeys2 = await table.OrderBy(table.PrimaryKey).Keys();

                person6Key = await table.Where(p => p.Name, "Person6", p => p.Age, 75, p => p.Phone.Number.Number, "22222").Keys();
                person6 = await table.Where(p => p.Name, p => p.Age, p => p.Phone.Number.Number).Equal(("Person6", 75, "22222")).First();
            });


            if (sortedPersons.First()?.Age != sortedKeys.First())
            {
                throw new InvalidOperationException("Keys not found.");
            }

            if (indexKeys.Count() != persons.Count())
            {
                throw new InvalidOperationException("Keys not found.");
            }

            if (uniqueKeys.Count() != uniqueData.Count())
            {
                throw new InvalidOperationException("UniqueKeys not found.");
            }

            if (!queryData.SequenceEqual(queryKeys))
            {
                throw new InvalidOperationException("Keys not found.");
            }

            if (!queryData.SequenceEqual(queryEqualKeys))
            {
                throw new InvalidOperationException("Keys not found.");
            }

            if (!dataTags.SequenceEqual(tagKeys))
            {
                throw new InvalidOperationException("Keys not found.");
            }

            if (!primaryKeys1.SequenceEqual(primaryKeys2))
            {
                throw new InvalidOperationException("Keys not identical.");
            }

            if (person6Key.FirstOrDefault().Item1 != "Person6")
            {
                throw new InvalidOperationException("Item not identical.");
            }

            if (person6?.Name != "Person6")
            {
                throw new InvalidOperationException("Item not identical.");
            }

            return "OK";
        }
    }
}
