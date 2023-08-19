using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class AddClass : DexieTest<TestDB>
    {
        public AddClass(TestDB db) : base(db)
        {
        }

        public override string Name => "AddClass";

        public override async ValueTask<string?> RunTest()
        {
            DB.Close();
            DB.Version(1).Stores();
            await DB.Open();

            var table = DB.PersonWithProperties;
            await table.Clear();

            var person = new PersonWithProperties("FirstName", "LastName");

            var key = await table.Add(person);
            var (firstName, lastName, id) = (await table.ToArray()).First();

            if (firstName != person.FirstName || lastName != person.LastName || id != key)
            {
                throw new InvalidOperationException("Item invalid.");
            }

            await table.Clear();
            var person1 = new PersonWithProperties("FirstName", "LastName");

            var key1 = await table.Put(person1);
            var (firstName1, lastName1, id1) = (await table.ToArray()).First();

            if (firstName1 != person1.FirstName || lastName1 != person1.LastName || id1 != key1)
            {
                throw new InvalidOperationException("Item invalid.");
            }

            person1.FirstName = "Update";

            var key2 = await table.Put(person1);
            var (firstName2, lastName2, id2) = (await table.ToArray()).First();

            if (firstName2 != person1.FirstName || lastName2 != person1.LastName || key1 != key2 || id2 != key2)
            {
                throw new InvalidOperationException("Item invalid.");
            }

            return "OK";
        }
    }
}
