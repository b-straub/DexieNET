using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Update(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "Update";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons;
            await table.Clear();

            var person = DataGenerator.GetPerson1();

            var res = await table.Add(person);
            var personAdded = (await table.ToArray()).FirstOrDefault();

            PersonComparer comparer = new(true);
            if (!comparer.Equals(person, personAdded))
            {
                throw new InvalidOperationException("Item not identical.");
            }

            if (personAdded?.Id is null || personAdded.Id != res)
            {
                throw new InvalidOperationException("Updated preparation failed.");
            }

            var updateRes = await table.Update(res, p => p.Name, "Updated", p => p.Address.Street, "Updated");

            if (!updateRes)
            {
                throw new InvalidOperationException("Updated failed.");
            }

            var personUpdated = (await table.ToArray()).FirstOrDefault();

            if (personUpdated?.Name != "Updated" || personUpdated?.Address.Street != "Updated")
            {
                throw new InvalidOperationException("Item not updated.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                res = await table.Add(person);
                updateRes = await table.Update(res, p => p.Name, "Updated", p => p.Address.Street, "Updated");
            });

            if (!updateRes)
            {
                throw new InvalidOperationException("Updated failed.");
            }

            personUpdated = (await table.ToArray()).FirstOrDefault();

            if (personUpdated?.Name != "Updated" || personUpdated?.Address.Street != "Updated")
            {
                throw new InvalidOperationException("Item not updated.");
            }

            return "OK";
        }
    }
}
