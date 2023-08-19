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

            var table = DB.Persons;
            await table.Clear();

            var person = DataGenerator.GetPerson1();

            var res = await table.Put(person);

            var personAdded = (await table.ToArray()).FirstOrDefault();

            PersonComparer comparerNoID = new(true);

            if (!comparerNoID.Equals(person, personAdded))
            {
                throw new InvalidOperationException("Item not identical.");
            }

            PersonComparer comparer = new();

            person = personAdded with { Name = "Updated" };
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
