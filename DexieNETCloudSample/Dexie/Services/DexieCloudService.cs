using DexieNET;
using DexieCloudNET;
//using DexieNETCloudSample.Aministration;
using RxBlazorLightCore;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace DexieNETCloudSample.Logic
{
    [DBName("ToDoDB")]
    public interface IToDoDBItem : IDBStore
    {
    }

    public interface IIDPrimaryIndex
    {
        string? ID { get; }
    }

    [Schema(CloudSync = true)]
    [CompoundIndex("ListID", "RealmId")]
    public partial record ToDoDBItem
    (
        [property: Index] string Text,
        [property: Index] DateTime DueDate,
        [property: BoolIndex] bool Completed,
        [property: Index] string ListID,
        [property: Index(IsPrimary = true, IsAuto = true)] string? ID
    ) : IToDoDBItem, IIDPrimaryIndex
    {
        public static ToDoDBItem Create(string text, DateTime dueDate, ToDoDBList list, ToDoDBItem? item)
        {
            ArgumentNullException.ThrowIfNull(list.ID);

            var newItem = new ToDoDBItem(text, dueDate, item is not null && item.Completed, list.ID, item?.ID);

            if (item is not null)
            {
                newItem = newItem with { Owner = item.Owner, RealmId = item.RealmId };
            }
            else
            {
                newItem = newItem with { RealmId = list.RealmId };
            }

            return newItem;
        }
    }

    [Schema(CloudSync = true)]
    public partial record ToDoDBList
    (
        [property: Index] string Title,
        [property: Index(IsPrimary = true, IsAuto = true)] string? ID
    ) : IToDoDBItem, IIDPrimaryIndex
    {
        public static ToDoDBList Create(string title, ToDoDBList? list = null)
        {
            var newList = new ToDoDBList(title, list?.ID);

            if (list is not null)
            {
                newList = newList with { Owner = list.Owner, RealmId = list.RealmId };
            }

            return newList;
        }
    }

    public record ListOpenClose
    (
        bool IsShareOpen,
        bool IsItemsOpen,
        [property: Index(IsPrimary = true)] string? ListID
    ) : IToDoDBItem
    {
        public static ListOpenClose Create(string listID)
        {
            return new(false, false, listID);
        }
    }

    public enum DBState
    {
        Closed,
        Opened,
        Cloud
    }

    public sealed class DexieCloudService : RxBLService
    {
        public ToDoDB? DB { get; private set; }
        public bool IsDBOpen => DB is not null;

        public IState<DBState> State { get; }
        public IState<SyncState?> SyncState { get; }
        public IState<UserLogin?> UserLogin { get; }
        public IState<UIInteraction?> UIInteraction { get; }
        public IState<IEnumerable<Invite>?> Invites { get; }
        public IState<Dictionary<string, Role>?> Roles { get; }

        public string? CloudURL { get; private set; }

        private readonly IDexieNETFactory<ToDoDB> _dexieFactory;
        private readonly CompositeDisposable _DBServicesDisposeBag = [];

        public DexieCloudService(IServiceProvider serviceProvider)
        {
            var dexieService = serviceProvider.GetRequiredService<IDexieNETService<ToDoDB>>();
            _dexieFactory = dexieService.DexieNETFactory;

            State = this.CreateState(DBState.Closed);
            SyncState = this.CreateState((SyncState?)null);
            UserLogin = this.CreateState((UserLogin?)null);
            UIInteraction = this.CreateState((UIInteraction?)null);
            Invites = this.CreateState((IEnumerable<Invite>?)null);
            Roles = this.CreateState((Dictionary<string, Role>?)null);
        }

        public async ValueTask OpenDB()
        {
            if (DB is null)
            {
#if DEBUG
                Console.WriteLine("OpenDB");
#endif
                await _dexieFactory.Delete();
                DB = await _dexieFactory.Create();
                DB.Version(1).Stores();

                ArgumentNullException.ThrowIfNull(DB);
                State.Value = DBState.Opened;
            }
        }

        public async Task Logout(bool force = false)
        {
            ArgumentNullException.ThrowIfNull(DB);
            await DB.Logout(force);
        }

        public void ConfigureCloud(string cloudURL)
        {
            if (CloudURL == cloudURL)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(DB);

            CloudURL = cloudURL;

            var options = new DexieCloudOptions(CloudURL)
                .WithCustomLoginGui(true)
                .WithRequireAuth(false);
                //.WithFetchTokens(FetchTokens);

            // call before configure cloud to have login UI ready when needed
            _DBServicesDisposeBag.Add(DB.UserInteractionObservable().Subscribe(ui =>
            {
                UIInteraction.Value = ui;
            }));

            DB.ConfigureCloud(options);

            _DBServicesDisposeBag.Add(DB.SyncStateObservable().Subscribe(ss =>
            {
                SyncState.Value = ss;
            }));

            _DBServicesDisposeBag.Add(DB.PersistedSyncStateStateObservable().Subscribe(pss =>
            {
                Console.WriteLine(pss);
            }));

            _DBServicesDisposeBag.Add(DB.WebSocketStatusObservable().Subscribe(wss =>
            {
                Console.WriteLine(wss);
            }));

            _DBServicesDisposeBag.Add(DB.SyncCompleteObservable().Subscribe(pss =>
            {
                Console.WriteLine("SyncComplete");
            }));

            _DBServicesDisposeBag.Add(DB.UserLoginObservable().Subscribe(ul =>
            {
                UserLogin.Value = ul;
            }));

            _DBServicesDisposeBag.Add(DB.RoleObservable().Subscribe(r =>
            {
                Roles.Value = r;
            }));

            _DBServicesDisposeBag.Add(DB.InvitesObservable().Subscribe(i =>
            {
                Invites.Value = i;
            }));

            State.Value = DBState.Cloud;

#if DEBUG
            Console.WriteLine($"We're using dexie-cloud-addon {DB.AddOnVersion()}");
            var cloudOptions = DB.Options();
            var schema = DB.Schema();
            var usingServiceWorker = DB.UsingServiceWorker();
#endif
        }

        public async ValueTask<string?> Login(LoginInformation loginInformation)
        {
            ArgumentNullException.ThrowIfNull(DB);

            return await DB.UserLogin(loginInformation);
        }

        public async ValueTask Sync(SyncOptions syncOptions)
        {
            ArgumentNullException.ThrowIfNull(DB);
            await DB.Sync(syncOptions);
        }

        /*
        private async Task<TokenFinalResponse?> FetchTokens(TokenParams tokenParams)
        {
            var adminstrationService = _serviceProvider.GetRequiredService<AdministrationService>();
    
            var cloudKeyData = new CloudKeyData
            {
                ClientId = "xx",
                ClientSecret = "xxx"
            };
            var tokenFinalResponse = await adminstrationService.GetUserCredentials(cloudKeyData, tokenParams, CancellationToken.None);
            return tokenFinalResponse;
        }
        */
    }
}
