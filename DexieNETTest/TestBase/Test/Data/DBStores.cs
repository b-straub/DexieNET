using DexieNET;

namespace DexieNETTest.TestBase.Test
{
    [DBName("SecondDB")]
    public interface ISecond : IDBStore
    {
    }

    [Schema(StoreName = nameof(TestStore), OutboundPrimaryKey = true)]
    public record TestStore
    (
        [property: Index] string Name
    ) : ISecond;

    [Schema(OutboundPrimaryKey = true)]
    public record NoIndex
    (
        string Name
    ) : IDBStore;

    public interface ITestDB : IDBStore
    {
    }

    public record Address
    (
        [property: Index] string Street,
        [property: Index] string Housenumber,
        [property: Index] string City,
        [property: Index] string ZIP,
        [property: Index] string Country
    );

    public enum PhoneType
    {
        Mobile,
        LandLine,
        Satellite
    }

    public record PhoneNumber
    (
        [property: Index] string Country,
        [property: Index] string Number
    );

    public record Phone
    (
        PhoneNumber Number,
        PhoneType Type
    );

    [CompoundIndex("Name", "Age")]
    [CompoundIndex("Name", "Address.City")]
    [CompoundIndex("Address.City", "Address.Street")]
    [CompoundIndex("Name", "Age", "Phone.Number.Number")]
    public partial record Person
    (
        [property: Index] string Name,
        [property: Index] int Age,
        Address Address,
        Phone Phone,
        [property: Index(IsMultiEntry = true)] IEnumerable<string> Tags,
        [property: Index(IsUnique = true)] Guid Guid,
        string NotIndexed
    ) : ITestDB;

    public record Student
    (
        [property: Index] string Faculty,
        [property: Index] int PersonID,
        [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
    ) : ITestDB;

    [CompoundIndex("Date", "Include", IsPrimary = false)]
    public record FieldTest
    (
        [property: BoolIndex] bool Include,
        [property: BoolIndex(IsMultiEntry = true)] IEnumerable<bool> IncludeME,
        [property: Index] DateOnly Date,
        [property: Index] TimeOnly Time,
        [property: Index] TimeSpan TimeSpan,
        [property: Index] DateTime DateTime,
        [property: ByteIndex] byte[] Blob,
        [property: ByteIndex(IsMultiEntry = true)] IEnumerable<byte[]> BlobME,
        [property: Index] int[] Array,
        [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
    ) : ITestDB;

    [Schema(StoreName = "Logentries")]
    public record Logentry
   (
       [property: Index] string Message,
       [property: Index] DateTime DateTime,
       [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
   ) : ITestDB;

    public record AgeInfo1
    (
         [property: Index] int Age
    );

    public record AgeInfo2
    (
         [property: Index] DateTime? BirthDate
    );

    public record Friend
    (
        [property: Index] string Name,
        int ShoeSize,
         AgeInfo1 AgeInfo,
        [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
    ) : ITestDB;

    [Schema(StoreName = "Friends2", UpdateStore = typeof(Friend))]
    public record Friend2
    (
        [property: Index] string Name,
        [property: Index] int ShoeSize,
        AgeInfo1 AgeInfo,
        [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
    ) : ITestDB;

    [Schema(StoreName = "Friends3", UpdateStore = typeof(Friend))]
    public record Friend3
    (
        [property: Index] string FirstName,
        [property: Index] string LastName,
        [property: Index] AgeInfo2 AgeInfo,
        [property: Index(IsPrimary = true, IsAuto = true)] int? ID = null
    ) : ITestDB;

    [Schema(StoreName = nameof(PersonWithProperties), PrimaryKeyName = "PKey")]
    public partial class PersonWithProperties : ITestDB
    {
        [Index] public string FirstName { get; init; }
        [Index] public string LastName { get; init; }
        public PersonWithProperties(string firstName, string lastName)
          => (FirstName, LastName) = (firstName, lastName);
        public void Deconstruct(out string firstName, out string lastName, out ulong? pkey)
          => (firstName, lastName, pkey) = (FirstName, LastName, PKey);
    }

    public record FriendIBP
    (
        [property: Index] string Name,
        [property: Index(IsPrimary = true)] string ID
    ) : ITestDB;

    public record FriendIBBP
  (
      [property: Index] string Name,
      [property: ByteIndex(IsPrimary = true)] byte[] ID
  ) : ITestDB;

    [Schema(OutboundPrimaryKey = true)]
    public record FriendNP
    (
        [property: Index] string Name
    ) : ITestDB;

    [CompoundIndex("FirstName", "LastName", IsPrimary = true)]
    public record PersonCompound
    (
        string LastName,
        string FirstName
    ) : ITestDB;

    public class PersonComparer : IEqualityComparer<Person>
    {
        private readonly bool _ignoreID;

        public PersonComparer(bool IgnoreID = false)
        {
            _ignoreID = IgnoreID;
        }

        public bool Equals(Person? x, Person? y)
        {
            if (x is null || y is null)
            {
                return false;
            }

            var IDEquals = _ignoreID || x.ID == y.ID;

            return x.Name == y.Name && x.Age == y.Age && x.Address == y.Address && x.Phone == y.Phone &&
                x.Guid == y.Guid && IDEquals && Enumerable.SequenceEqual(x.Tags, y.Tags);
        }

        public int GetHashCode(Person obj)
        {
            HashCode hash = new();
            hash.Add(obj.Name);
            hash.Add(obj.Age);
            hash.Add(obj.Address);
            hash.Add(obj.Phone);
            hash.Add(obj.Guid);

            foreach (var tag in obj.Tags)
            {
                hash.Add(tag);
            }

            if (!_ignoreID)
            {
                hash.Add(obj.ID);
            }

            return hash.ToHashCode();
        }
    }

    public class StudentComparer : IEqualityComparer<Student>
    {
        private readonly bool _ignoreID;

        public StudentComparer(bool IgnoreID = false)
        {
            _ignoreID = IgnoreID;
        }

        public bool Equals(Student? x, Student? y)
        {
            if (x is null || y is null)
            {
                return false;
            }

            var IDEquals = _ignoreID || x.ID == y.ID;

            return x.Faculty == y.Faculty && IDEquals;
        }

        public int GetHashCode(Student obj)
        {
            HashCode hash = new();
            hash.Add(obj.Faculty);

            if (!_ignoreID)
            {
                hash.Add(obj.ID);
            }

            return hash.ToHashCode();
        }
    }
}
