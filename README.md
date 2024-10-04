DexieNET
========

DexieCloudNET is a .NET wrapper for dexie.js minimalist wrapper for IndexedDB see https://dexie.org with cloud support see https://dexie.org/cloud/ .

*'DexieNET' used with permission of David Fahlander*

*and made with*
<img src="https://resources.jetbrains.com/storage/products/company/brand/logos/Rider.png" alt="Rider logo." style="width:100px;"> [*Support for Open-Source Projects*](https://www.jetbrains.com/community/opensource/#support) !

##  News

- Released [DexieCloud](https://dexie.org/cloud/)
- Added Push support. Please check the [ReadMe](DexieNETCloudPushServer/README.md)
  - Clicking on notifications currently does not work reliable for iOS [notificationclick events in serviceworkers not firing](https://bugs.webkit.org/show_bug.cgi?id=268797)
  - Clicking on notifications currently does not work reliable for Chrome on MacOS 15
- Please register with **DexieCloud** and test the [ToDoSample](DexieNETCloudSample). The configuration script can be found here [configure-app.ps1](DexieNETCloudSample/Dexie/configure-app.ps1) (Windows) or here [configure-app.sh](DexieNETCloudSample/Dexie/configure-app.sh) (Nix - jq required).
- Published a new helper library [RxBlazorLight](https://github.com/b-straub/RxBlazorLight)  

---

##  Basic

**DexieNET** aims to be a feature complete .NET wrapper for **Dexie.js** the famous Javascript IndexedDB wrapper from David Fahlander including support for **cloud sync**.

I consists of two parts, a source generator converting  a C# record, class, struct to a DB store and a set of wrappers around the well known Dexie.js API constructs such as *Table, WhereClause, Collection*, ...

It's designed to work within a Blazor Webassembly application with minimal effort. 

#### Hello World

- create a Blazor WebAssembly
- Add the **DexieNET** Nuget
- Add the HelloWorld Component and update the index

*Program.cs*

```c#
using DexieNET;
using YourNamspace.Pages;
....

builder.Services.AddDexieNET<FriendsDB>();
```

*HelloWorld.razor*

```razor
@page "/helloWorld"
@using DexieNET.Component
@inherits DexieNET<FriendsDB>

Friends:
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

<hr />

Logs:
@if (_logs is null)
{
    <p>Loading...</p>
}
else if (_logs.Count() == 0)
{
    <p>No items...</p>
}
else
{
    <ul style="list-style: square inside;">
        @foreach (var logEntry in _logs)
        {
            <li>
                Message: @logEntry.Message, TimeStamp: @logEntry.TimeStamp.ToLongTimeString();
            </li>
        }
    </ul>
}

<hr />

<div style="display: flex; column-gap: 50px">
    <button class="btn btn-primary" style="flex: 0 1 auto" @onclick="PopulateDatabase">
        PopulateDatabase
    </button>

    <button class="btn btn-secondary" style="flex: 0 1 auto" @onclick="GoodTransaction">
        GoodTransaction
    </button>

    <button class="btn btn-secondary" style="flex: 0 1 auto" @onclick="FailedTransaction">
        FailedTransaction
    </button>

    <button class="btn btn-secondary" style="flex: 0 1 auto" @onclick="ClearDatabase">
        ClearDatabase
    </button>
</div>
```

*HelloWorld.razor.cs*

```c#
using DexieNET;
using System.ComponentModel.DataAnnotations;
using System.Runtime.InteropServices;
using System.Xml.Linq;

namespace DexieNETHelloWorld.Pages
{
    public interface IFriendsDB : IDBStore { };

    public partial record Friend
    (
        [property: Index] string Name,
        [property: Index] int Age
    ) : IFriendsDB;

    public partial record LogEntry
    (
        [property: Index] string? Message,
        [property: Index] DateTime TimeStamp
    ) : IFriendsDB;

    public partial class HelloWorld
    {
        private IEnumerable<Friend>? _friends;
        private IEnumerable<LogEntry>? _logs;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await Dexie.Version(1).Stores();
            await FillTables();
        }

        private async Task FillTables()
        {
            _friends = await Dexie.Friends().ToArray();
            _logs = await Dexie.LogEntries().OrderBy(l => l.TimeStamp).Reverse().ToArray();
            await InvokeAsync(StateHasChanged);
        }

        private async Task LogMessage(string? message)
        {
            await Dexie.Transaction(async _ =>
            {
                await Dexie.LogEntries().Add(new LogEntry(message, DateTime.Now));
            }, TAType.TopLevel);
        }
        private async Task ClearDatabase()
        {
            await Dexie.Friends().Clear();
            await Dexie.LogEntries().Clear();

            await FillTables();
        }

        private async Task PopulateDatabase()
        {
            await LogMessage("PopulateDatabase");

            Random rand = new();
            await Dexie.Friends().Add(new Friend("Jane Doe", rand.Next(1, 99)));
            await Dexie.Friends().Add(new Friend("John Doe", rand.Next(1, 99)));

            await FillTables();
        }

        private async Task GoodTransaction()
        {
            await LogMessage("GoodTransaction");

            await Dexie.Transaction(async ta =>
            {
                Random rand = new();
                var key = await Dexie.Friends().Add(new Friend("Luke", rand.Next(1, 99)));
                var friend = await Dexie.Friends().Get(key);

                if (friend?.Name == "Luke" || ta.Collecting)
                // ta.Collecting, this means the first pass of the transaction, in which the table names are collected
                // if a second table is hidden behind a conditional statement, it must also be visited in the first pass
                {
                    await Dexie.Friends().Add(new Friend("John", rand.Next(1, 99)));
                    await Dexie.LogEntries().Add(new LogEntry("TA executed", DateTime.Now));
                }
            });

            await FillTables();
        }

        private async Task FailedTransaction()
        {
            await LogMessage("ProvokeFail");

            try
            {
                await Dexie.Transaction(async ta =>
                {
                    await Dexie.Friends().Clear();
                    var key = await Dexie.Friends().Add(new Friend("Test", 33));
                    var friend = await Dexie.Friends().Get(key);

                    if (friend?.Name == "Test" || ta.Collecting)
                    {
                        await LogMessage("TA will fail");
                    }
                    await Dexie.Friends().Add(friend); // this will fail
                });
            }
            catch (Exception ex)
            {
                var firstDot = ex.Message.IndexOf('.');
                var message = firstDot <= 0 ? ex.Message : ex.Message[..firstDot];
                await LogMessage($"TA failed: {message}");
            }

            await FillTables();
        }
    }
}
```

## Advanced

### Naming

- the Source Generator will create the following classes from an *IDBStore* derived class, struct, record:
	- **PIndentifier** -> Plural of *Identifier* provided by *Humanizer.Core* (English only), be aware the plural form might not be always obvious e.g. Person -> People
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
	[DBName("TestDB")] // optional -> default name = interface name without leading 'I' -> PersonsDB
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
    public partial record Address
    (
        [property: Index] string Street,
        [property: Index] string Housenumber,
        [property: Index] string City,
        [property: Index] string ZIP,
        [property: Index] string Country

    ) : IPersonsDB;

	......
	// Service
	using DexieNET;
	.......
	builder.Services.AddDexieNET<TestDB>();
	
	// Component
	[Inject]
	public IDexieNETService<TestDB>? TestDB { get; set; }

	......
	// Table
	var persons = await TestDB.Persons();
	var addresses = await TestDB.Addresses();
	```

### Transactions

- transactions capture the table names in two passes
- if a second table is hidden behind a conditional statement, it must also be visited in the first pass
- nested transactions contain all required table names
- top level transactions must use *TAType.TopLevel*, the root transaction is implictly a top level transaction
- parallel transactions are composed by a root transaction with *TAType.TopLevel* and top level child transactions

### Samples

The tests from [TestCases](DexieNETTest/TestBase/Test/TestCases) will cover all possible *DexieNET Api* calls. Those calls are as close as possible modelled after the original *Dexie.js* API.