DexieNET
========

DexieNET is a .NET wrapper for dexie.js minimalistic wrapper for IndexedDB see https://dexie.org

*'DexieNET' used with permission of David Fahlander*

##  Basic

**DexieNET** aims to be a feature complete .NET wrapper for **Dexie.js** the famous Javascript IndexedDB wrapper from David Fahlander.

I consists of two parts, a source generator converting  a C# record, class, struct to a DB store and a set of wrappers around the well known Dexie.js API constructs such as *Table, WhereClause, Collection*, ...

It's designed to work within a Blazor Webassembly application with minimal effort. 

#### Hello World

*Program.cs*

```c#
using DexieNET;

....

builder.Services.AddDexieNET<FriendsDB>();
```

*HelloWorld.razor*

```razor
@inherits DexieNET<FriendsDB>

@if (_friends is null)
{
	<p>Loading...</p>
}
else if (_friends.Count() == 0)
{
	<p>No items...</p>
}
else
{
	<ul style="list-style: square inside;">
		@foreach (var friend in _friends)
		{
			<li>
				Name: @friend.Name, Age: @friend.Age
			</li>
		}
	</ul>
}
```

*HelloWorld.razor.cs*

```c#
using DexieNET;

namespace YourNamspace.HelloWorld 
{
	public partial record Friend
	(
		[property: Index] string Name,
		[property: Index] int Age
	) : IDBStore;

	public partial class DBComponentTest
	{
		private var IEnumerable<Friend>? _friends;

		protected override async Task OnInitializedAsync()
		{
			await base.OnInitializedAsync();
			
			if (Dexie is not null)
			{
				await Dexie.Version(1).Stores();
				await Dexie.Friends().Add(new Friend("Jane Doe", 44));
				_friends = await Dexie.Friends().ToArray();
			}
		}
	}
}

```

## Advanced

###Naming###

- the Source Generator will create the following classes from an *IDBStore* derived class, struct, record:
	- **PIndentifier** -> Plural of *Identifier* provided by *PluralizeService.Core* (English only)
	- service: *PIndentifier***DB**
	- table: *PIndentifier*
	
		```c#
		// Record
		public partial record Friend
		(
			[property: Index] string Name,
			[property: Index] int Age
		) : IDBStore;

		......
		// Service
		builder.Services.AddDexieNET<FriendsDB>();
		......
		[Inject]
		public IDexieNETService<FriendsDB>? DB { get; set; }

		......
		// Table
		var table = await DB.Friends();
		```
		
	- You can have multiple stores in one database
	
	```c#
	[DBName("TestDB") // optional -> default name = interface name 
	without leading 'I' -> PersonsDB
	public interface IPersonsDB : IDBStore
	{
	}

	// Records
	[CompoundIndex("FirstName", "LastName")]
	public partial record Person
	(
		[property: Index] string FirstName,
		[property: Index] string LastName,
		Guid? AddressKey
	) : IPersonsDB;

	[CompoundIndex("City", "Street")]
	[CompoundIndex("Zip", "Street")]
	public record Address
	(
		[property: Index] string Street,
		[property: Index] string Housenumber,
		[property: Index] string City,
		[property: Index] string ZIP,
		[property: Index] string Country,
		[property: Index(IsPrimary = true, IsAuto = true) Guid? Key
	) IPersonsDB;

	......
	// Service
	builder.Services.AddDexieNET<TestDB>();
	......
	[Inject]
	public IDexieNETService<TestDB>? TestDB { get; set; }

	......
	// Table
	var persons = await TestDB.Persons();
	var addresses = await TestDB.Addresses();
	```

###Transactions###

```c#
void async Task LogName(string name)
{
	await DB.Transaction(async _ =>
	{
		try
		{
			await DB.LogEntries.Add(new LogEntry(name, DateTime.Now());
		}
		catch (Exception ex) // this will prevent outer transaction to abort even when 		nested transaction failed
		{
			Console.WriteLine($"Can not add {name} to Log));
		}
	});
}

try
{
	await DB.Transaction(async tx =>
	{
		await db.Friends().Clear();
		var key = await db.Friends().Add(new Friend("Test", 33));
		var friend = await db.Friends().Get(key);
		
		if (friend.Name == "Test" || tx is null) // tx is null means first pass of transaction 	where table names are collected
		{
			await LogName(friend.Name);
		}
		await table.Add(person); // this will fail
	});
}
catch (Exception ex)
{
	if (ex.GetType() != typeof(TransactionException))
	{
		Console.WriteLine("Transaction failed unexpectedly");
	}
}
```

###Samples##

The tests from [TestCases](DexieNETTest/TestBase/TestCases) will cover all possible *DexieNET Api* calls. Those calls are as close as possible modelled after the original *Dexie.js* API.