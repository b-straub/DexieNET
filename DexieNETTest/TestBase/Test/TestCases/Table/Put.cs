using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Put : DexieTest<TestDB>
    {
        public Put(TestDB db) : base(db)
        {
        }

        public override string Name => "Put";

        public override async ValueTask<string?> RunTest()
        {
            var table = await DB.Persons();
            await table.Clear();

            var person = DataGenerator.GetPerson1();

            var res = await table.Add(person);
            person = person with { ID = res };

            var personAdded = (await table.ToArray()).FirstOrDefault();

            PersonComparer comparer = new();

            if (!comparer.Equals(person, personAdded))
            {
                throw new InvalidOperationException("Item not identical.");
            }

            person = person with { Name = "Updated" };
            res = await table.Put(person);

            personAdded = (await table.ToArray()).FirstOrDefault();

            if (personAdded?.Name != "Updated")
            {
                throw new InvalidOperationException("Item not updated.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                res = await table.Put(person);
            });

            personAdded = (await table.ToArray()).FirstOrDefault();

            if (personAdded?.Name != "Updated")
            {
                throw new InvalidOperationException("Item not updated Transaction.");
            }

            return "OK";
        }
    }
}
