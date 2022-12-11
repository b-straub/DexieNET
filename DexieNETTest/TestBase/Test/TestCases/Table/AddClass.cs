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
            await DB.Close();
            await DB.Version(1).Stores();
            await DB.Open();

            var table = await DB.PersonWithProperties();
            await table.Clear();

            var person = new PersonWithProperties("FirstName", "LastName");

            var key = await table.Add(person);
            var (firstName, lastName, id) = (await table.ToArray()).First();

            if (firstName != person.FirstName || lastName != person.LastName || id != key)
            {
                throw new InvalidOperationException("Item invalid.");
            }

            return "OK";
        }
    }
}
