using DexieNET;
using R3;

namespace DexieNETTest.TestBase.Test
{
    internal class LiveQueryWriteTest(TestDB db) : DexieTest<TestDB>(db)
    {
        public override string Name => "LiveQueryWriteTest";

        public override async ValueTask<string?> RunTest()
        {
            var finished = false;
            var tablePersons = DB.Persons;
            await tablePersons.Clear();
            var tableStudents = DB.Students;
            await tableStudents.Clear();
            
            var student = new Student("Physics", -1);
            var keyStudent = await DB.Students.Add(student);
            
            var lqP = DB.LiveQuery(async () =>
            {
                var values = await tablePersons.Where(p => p.Age).AboveOrEqual(50).ToArray();
                return values;
            });

            var disposableP = lqP.AsObservable.SubscribeAwait(async (values, ct) =>
            {
                Console.WriteLine($"Count: {values.Count()}");
                await tableStudents.Delete(keyStudent);
                finished = true;
            });
            
            var persons = DataGenerator.GetPersons();
            await DB.Persons.BulkAdd(persons);

            do
            {
                await Task.Delay(100);
            } while (!finished);
            
            var studentsCount = await tableStudents.Count();
            
            if (studentsCount != 0)
            {
                throw new InvalidOperationException($"studentsCount : {studentsCount}-> LiveQueryWriteTest failed.");
            }
            
            disposableP.Dispose();
            return "OK";
        }
    }
}
