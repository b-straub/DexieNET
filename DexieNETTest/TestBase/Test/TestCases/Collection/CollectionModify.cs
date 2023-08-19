using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class CollectionModify : DexieTest<TestDB>
    {
        public CollectionModify(TestDB db) : base(db)
        {
        }

        public override string Name => "CollectionModify";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons;
            await table.Clear();

            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            const string pName = "Person7";
            var foundPersonsCount = await table.Where(p => p.Name).Equal(pName).Count();
            if (foundPersonsCount == 0)
            {
                throw new InvalidOperationException("Test Data invalid for modified.");
            }

            var modifiedCount = await table.ToCollection().Modify(p => p.Name, "Updated", p => p.Address.Street, "Updated");
            Person? personUpdated = (await table.ToArray()).LastOrDefault();

            if (personUpdated?.Name != "Updated" || personUpdated?.Address.Street != "Updated")
            {
                throw new InvalidOperationException("Item not modified.");
            }

            if (modifiedCount != persons.Count())
            {
                throw new InvalidOperationException("Some Items not modified.");
            }

            await table.Clear();
            await table.BulkAdd(persons);

            modifiedCount = await table.ToCollection().Modify(p => p.Name, null);
            personUpdated = (await table.ToArray()).LastOrDefault();
            foundPersonsCount = await table.Where(p => p.Name).Equal(pName).Count();

            if (personUpdated?.Name != null || foundPersonsCount > 0)
            {
                throw new InvalidOperationException("Item not modified.");
            }

            if (modifiedCount != persons.Count())
            {
                throw new InvalidOperationException("Some Items not modified.");
            }

            await table.Clear();
            await table.BulkAdd(persons);

            modifiedCount = await table.ToCollection().Modify(p => p with { Name = "Updated", Address = new Address("Updated", p.Address.Housenumber, p.Address.City, p.Address.ZIP, p.Address.Country) });
            personUpdated = (await table.ToArray()).LastOrDefault();

            if (personUpdated?.Name != "Updated" || personUpdated?.Address.Street != "Updated")
            {
                throw new InvalidOperationException("Item not modified.");
            }

            if (modifiedCount != persons.Count())
            {
                throw new InvalidOperationException("Some Items not modified.");
            }

            modifiedCount = await table.ToCollection().Modify(p => null);

            if (await table.Count() > 0)
            {
                throw new InvalidOperationException("Items not modified.");
            }

            if (modifiedCount != persons.Count())
            {
                throw new InvalidOperationException("Some Items not modified.");
            }


            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var collection = table.ToCollection();
                modifiedCount = await collection.Modify(p => p.Name, "Updated", p => p.Address.Street, "Updated");
                personUpdated = (await table.ToArray()).LastOrDefault();
            });

            if (personUpdated?.Name != "Updated" || personUpdated?.Address.Street != "Updated")
            {
                throw new InvalidOperationException("Item not modified.");
            }

            if (modifiedCount != persons.Count())
            {
                throw new InvalidOperationException("Some Items not modified.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var collection = table.ToCollection();
                modifiedCount = await collection.Modify(p => p.Name, null);
                personUpdated = (await table.ToArray()).LastOrDefault();
                var whereClause = table.Where(p => p.Name);
                var colFound = whereClause.Equal(pName);
                foundPersonsCount = await colFound.Count();
            });

            if (personUpdated?.Name != null || foundPersonsCount > 0)
            {
                throw new InvalidOperationException("Item not modified.");
            }

            if (modifiedCount != persons.Count())
            {
                throw new InvalidOperationException("Some Items not modified.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var collection = table.ToCollection();

                foundPersonsCount = await collection.Modify(p =>
                        p with
                        {
                            Name = "Updated",
                            Address = new Address("Updated", p.Address.Housenumber, p.Address.City,
                        p.Address.ZIP, p.Address.Country)
                        });
                personUpdated = (await table.ToArray()).LastOrDefault();
            });

            if (personUpdated?.Name != "Updated" || personUpdated?.Address.Street != "Updated")
            {
                throw new InvalidOperationException("Item not modified.");
            }

            if (foundPersonsCount != persons.Count())
            {
                throw new InvalidOperationException("Some Items not modified.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.BulkAdd(persons);
                var collection = table.ToCollection();

                modifiedCount = await collection.Modify(_ => null);
                foundPersonsCount = await table.Count();
            });

            if (foundPersonsCount > 0)
            {
                throw new InvalidOperationException("Items not modified.");
            }

            if (modifiedCount != persons.Count())
            {
                throw new InvalidOperationException("Some Items not modified.");
            }

            return "OK";
        }
    }
}
