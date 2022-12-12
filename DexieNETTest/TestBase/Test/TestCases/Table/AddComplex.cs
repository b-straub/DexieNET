using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    internal class AddComplex : DexieTest<TestDB>
    {
        public AddComplex(TestDB db) : base(db)
        {
        }

        public override string Name => "AddComplex";

        public override async ValueTask<string?> RunTest()
        {
            var tablePerson = await DB.Persons();
            var tableStudents = await DB.Students();

            await tablePerson.Clear();
            await tableStudents.Clear();

            var person = DataGenerator.GetPerson1();

            var keyP = await tablePerson.Add(person);
            var student = new Student("Physics", (int)keyP);
            var keyS = await tableStudents.Add(student);

            var personAdded = await tablePerson.Get(keyP);
            var studentAdded = await tableStudents.Get(keyS);

            PersonComparer pComparer = new(true);

            if (!pComparer.Equals(person, personAdded))
            {
                throw new InvalidOperationException("Item not identical.");
            }

            StudentComparer sComparer = new(true);

            if (!sComparer.Equals(student, studentAdded))
            {
                throw new InvalidOperationException("Item not identical.");
            }

            await DB.Transaction(async _ =>
            {
                await tablePerson.Clear();
                await tableStudents.Clear();

                keyP = await tablePerson.Add(person);

                student = new Student("Physics", (int)keyP);
                var keyS = await tableStudents.Add(student);

                personAdded = await tablePerson.Get(keyP);
                studentAdded = await tableStudents.Get(keyS);
            });

            if (!pComparer.Equals(person, personAdded))
            {
                throw new InvalidOperationException("Item not identical");
            }

            if (!sComparer.Equals(student, studentAdded))
            {
                throw new InvalidOperationException("Item not identical.");
            }

            return "OK";
        }
    }
}
