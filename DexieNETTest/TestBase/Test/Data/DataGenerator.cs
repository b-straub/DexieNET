
using DexieNETTest.TestBase.Components;

namespace DexieNETTest.TestBase.Test
{
    internal static class DataGenerator
    {
        private static IList<Address> GetAddresses()
        {
            return new Address[]
            {
                new Address("TestStreet1", "10", "TestCity1", "00000", "Germany"),
                new Address("TestStreet2", "20", "TestCity2", "11111", "France"),
                new Address("TestStreet3", "30", "TestCity3", "22222", "USA")
            };
        }

        private static IList<PhoneNumber> GetPhoneNumbers()
        {
            return new PhoneNumber[]
            {
                new PhoneNumber("0001", "00000"),
                new PhoneNumber("0002", "11111"),
                new PhoneNumber("0003", "22222"),
            };
        }

        public static Person GetPerson1()
        {
            var addresses = GetAddresses();
            return new Person("Person1", 40, addresses[0], new Phone(GetPhoneNumbers()[0], PhoneType.Mobile), new string[] { "Friend" }, Guid.NewGuid(), "NotIndexed");
        }

        public static Person GetPerson2()
        {
            var addresses = GetAddresses();
            return new Person("Person2", 50, addresses[1], new Phone(GetPhoneNumbers()[1], PhoneType.Mobile), new string[] { "Friend", "Buddy" }, Guid.NewGuid(), "NotIndexed");
        }

        public static Person GetPerson3()
        {
            var addresses = GetAddresses();
            return new Person("Person3", 60, addresses[2], new Phone(GetPhoneNumbers()[2], PhoneType.Mobile), new string[] { "Friend", "Buddy", "Neighbor" }, Guid.NewGuid(), "NotIndexed");
        }

        public static IEnumerable<Person> GetPersons()
        {
            var addresses = GetAddresses();

            return new Person[]
            {
                new Person("Person1", 11, addresses[0], new Phone(GetPhoneNumbers()[0], PhoneType.Mobile), new string[] { "Friend", "Neighbor" }, Guid.NewGuid(), "NotIndexed"),
                new Person("APerson2", 17, addresses[1], new Phone(GetPhoneNumbers()[1], PhoneType.LandLine), new string[] { "Friend", "Buddy", "Neighbor" }, Guid.NewGuid(), "NotIndexed"),
                new Person("APerson3", 25, addresses[2], new Phone(GetPhoneNumbers()[2], PhoneType.Satellite), new string[] { "Buddy" }, Guid.NewGuid(), "NotIndexed"),
                new Person("BPerson4", 33, addresses[0], new Phone(GetPhoneNumbers()[0], PhoneType.Mobile), new string[] { "Friend", "Buddy" }, Guid.NewGuid(), "NotIndexed"),
                new Person("BPerson5", 65, addresses[1], new Phone(GetPhoneNumbers()[1], PhoneType.LandLine), new string[] { "Neighbor" }, Guid.NewGuid(), "NotIndexed"),
                new Person("Person6", 75, addresses[2], new Phone(GetPhoneNumbers()[2], PhoneType.Satellite), new string[] { "Friend", "Neighbor" }, Guid.NewGuid(), "NotIndexed"),
                new Person("Person7", 85, addresses[0], new Phone(GetPhoneNumbers()[2], PhoneType.Satellite), new string[] { "Friend" }, Guid.NewGuid(), "NotIndexed"),
                new Person("Person7", 40, addresses[0], new Phone(GetPhoneNumbers()[2], PhoneType.Satellite), new string[] { "Friend" }, Guid.NewGuid(), "NotIndexed"),
                new Person("Person7", 60, addresses[0], new Phone(GetPhoneNumbers()[2], PhoneType.Satellite), new string[] { "Friend" }, Guid.NewGuid(), "NotIndexed"),
                new Person("Person7", 90, addresses[0], new Phone(GetPhoneNumbers()[2], PhoneType.Satellite), new string[] { "Friend" }, Guid.NewGuid(), "NotIndexed"),
                new Person("Person8", 30, addresses[0], new Phone(GetPhoneNumbers()[0], PhoneType.Satellite), new string[] { "Friend", "Neighbor" }, Guid.NewGuid(), "NotIndexed")
            };
        }

        public static IEnumerable<PersonCompound> GetPersonCompounds()
        {
            return new PersonCompound[]
            {
                new PersonCompound("Last1", "First1"),
                new PersonCompound("Last2", "First2"),
                new PersonCompound("Last3", "First3")
            };
        }

        private static readonly Random random = new();

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        public static IList<Person> GetPersonsRandom(int count = 10000)
        {
            var addresses = GetAddresses();
            return Enumerable.Range(0, count).Select(_ => new Person(RandomString(10), 11, addresses[0], new Phone(GetPhoneNumbers()[0], PhoneType.Mobile), new string[] { "Friend" }, Guid.NewGuid(), "NotIndexed"))
                .ToList();
        }

        public static IList<Person> GetPersonsRandom(int count, string name)
        {
            var addresses = GetAddresses();
            return Enumerable.Range(0, count).Select(_ => new Person(name, 11, addresses[0], new Phone(GetPhoneNumbers()[0], PhoneType.Mobile), new string[] { "Friend" }, Guid.NewGuid(), "NotIndexed"))
                .ToList();
        }

        public static IList<FieldTest> GetFieldTestRandom(int count = 100)
        {
            return Enumerable.Range(0, count)
                .Select(_ => random.Next(-5, 5))
                .Select(r => new FieldTest(
                    r >= 0, new DateOnly(2000 + r, 1, 1),
                    new TimeOnly(12 + r, 0), new TimeSpan(r * 1000),
                    new DateTime((DateTime.Now + TimeSpan.FromHours(r)).Ticks),
                    new byte[] { (byte)(r + 5), 0x0, 0x10, (byte)(r + 5) },
                    new int[] { (r + 500), 0x0, 0x10, (r + 500) }))
                .ToList();
        }

        public static IEnumerable<Friend> GetFriend()
        {
            return new Friend[]
            {
                new Friend("AAA BBB", 41, new AgeInfo1(30)),
                new Friend("CCC DDD", 44, new AgeInfo1(11)),
                new Friend("EEE FFF", 39, new AgeInfo1(44)),
                new Friend("GGG HHH", 45, new AgeInfo1(25)),
                new Friend("III JJJ", 26, new AgeInfo1(61)),
                new Friend("KKK LLL", 42, new AgeInfo1(73))
            };
        }

        public static IEnumerable<Friends> GetFriends()
        {
            return GetFriend().Select(f => new Friends(f.Name, f.AgeInfo.Age));
        }
    }
}
