using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Get : DexieTest<TestDB>
    {
        public Get(TestDB db) : base(db)
        {
        }

        public override string Name => "Get";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons();

            await table.Clear();

            var person = DataGenerator.GetPerson1();
            var keyP = await table.Add(person);

            var personAdded = await table.Get(keyP);

            PersonComparer pComparer = new(true);

            if (!pComparer.Equals(person, personAdded))
            {
                throw new InvalidOperationException("Item not identical.");
            }

            await table.Clear();
            var persons = DataGenerator.GetPersons();
            await table.BulkAdd(persons);

            var person6 = await table.Get(p => p.Name, "Person6");

            if (person6?.Name != "Person6")
            {
                throw new InvalidOperationException("Item not identical.");
            }

            person6 = await table.Get(p => p.Name, "Person6", p => p.Age, 75);

            if (person6?.Name != "Person6")
            {
                throw new InvalidOperationException("Item not identical.");
            }

            person6 = await table.Get(p => p.Name, "Person6", p => p.Age, 75, p => p.Phone.Number.Number, "22222");

            if (person6?.Name != "Person6")
            {
                throw new InvalidOperationException("Item not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                keyP = await table.Add(person);
                personAdded = await table.Get(keyP);

                await table.Clear();
                await table.BulkAdd(persons);
                person6 = await table.Get(p => p.Name, "Person6");
            });

            if (!pComparer.Equals(person, personAdded))
            {
                throw new InvalidOperationException("Item not identical.");
            }

            if (person6.Name != "Person6")
            {
                throw new InvalidOperationException("Item not identical.");
            }

            return "OK";
        }
    }
}
