using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class Add : DexieTest<TestDB>
    {
        public Add(TestDB db) : base(db)
        {
        }

        public override string Name => "Add";

        public override async ValueTask<string?> RunTest()
        {
            var table = DB.Persons;
            await table.Clear();

            var person = DataGenerator.GetPerson1();

            var key = await table.Add(person);
            var personAdded = (await table.ToArray()).FirstOrDefault();

            PersonComparer comparer = new(true);

            if (!comparer.Equals(person, personAdded))
            {
                throw new InvalidOperationException("Item not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await table.Clear();
                await table.Add(person);
            });

            personAdded = (await table.ToArray()).FirstOrDefault();

            if (!comparer.Equals(person, personAdded))
            {
                throw new InvalidOperationException("Item not identical from Transaction.");
            }

            return "OK";
        }
    }
}
