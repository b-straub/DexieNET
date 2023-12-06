using DexieNET;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

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

    public enum DBChangedMessage
    {
        Closed,
        Opened,
        Cloud,
        SyncState,
        UIInteraction,
        UserLogin,
        Roles,
        Invites
    }

    public sealed class DexieCloudService : IObservable<DBChangedMessage>
    {
        public ToDoDB? DB { get; private set; }
        public bool IsDBOpen => DB is not null;

        public SyncState? SyncState { get; private set; }
        public UserLogin? UserLogin { get; private set; }
        public UIInteraction? UIInteraction { get; private set; }
        public IEnumerable<Invite>? Invites { get; private set; }
        public Dictionary<string, Role> Roles { get; private set; } = [];

        public event Action? OnDelete;

        private readonly DexieNETFactory<ToDoDB> _dexieFactory;
        private readonly BehaviorSubject<DBChangedMessage> _dbChangedSubject = new(DBChangedMessage.Closed);
        private string? _cloudURL;
        private readonly CompositeDisposable _DBServicesDisposeBag = [];
        private readonly IObservable<DBChangedMessage> _changedObservable;

        public DexieCloudService(IDexieNETService<ToDoDB> dexieService)
        {
            _dexieFactory = dexieService.DexieNETFactory;
            _changedObservable = _dbChangedSubject.Publish().RefCount();
        }

        public async ValueTask OpenDB()
        {
            if (DB is null)
            {
#if DEBUG
                Console.WriteLine("OpenDB");
#endif
                await _dexieFactory.Delete();
                DB = await _dexieFactory.Create(true);
                DB.Version(1).Stores();

                ArgumentNullException.ThrowIfNull(DB);
                _dbChangedSubject.OnNext(DBChangedMessage.Opened);
            }
        }

        public async Task DeleteDB()
        {
            OnDelete?.Invoke();

            if (DB is not null)
            {
                _DBServicesDisposeBag.Clear();
                SyncState = null;
                _dbChangedSubject.OnNext(DBChangedMessage.SyncState);
                UIInteraction = null;
                _dbChangedSubject.OnNext(DBChangedMessage.UIInteraction);
                UserLogin = null;
                _dbChangedSubject.OnNext(DBChangedMessage.UserLogin);
                Invites = null;
                _dbChangedSubject.OnNext(DBChangedMessage.Invites);
                Roles.Clear();
                _dbChangedSubject.OnNext(DBChangedMessage.Roles);

                await DB.Delete();
                DB = null;
                _cloudURL = null;
#if DEBUG
                Console.WriteLine("DeleteDB");
#endif
                await Task.Delay(1000);

                _dbChangedSubject.OnNext(DBChangedMessage.Closed);
            }
        }

        public void ConfigureCloud(string cloudURL)
        {
            if (_cloudURL == cloudURL)
            {
                return;
            }

            ArgumentNullException.ThrowIfNull(DB);

            _cloudURL = cloudURL;

            var options = new DexieCloudOptions(_cloudURL)
                .WithCustomLoginGui(true)
                .WithRequireAuth(false);

            // call before configure cloud to have login UI ready when needed
            _DBServicesDisposeBag.Add(DB.UserInteractionObservable().Subscribe(ui =>
            {
                UIInteraction = ui;
                _dbChangedSubject.OnNext(DBChangedMessage.UIInteraction);
            }));

            DB.ConfigureCloud(options);

            _DBServicesDisposeBag.Add(DB.SyncStateObservable().Subscribe(ss =>
            {
                SyncState = ss;
                _dbChangedSubject.OnNext(DBChangedMessage.SyncState);
            }));

            _DBServicesDisposeBag.Add(DB.UserLoginObservable().Subscribe(ul =>
            {
                UserLogin = ul;
                _dbChangedSubject.OnNext(DBChangedMessage.UserLogin);
            }));

            _DBServicesDisposeBag.Add(DB.RoleObservable().Subscribe(r =>
            {
                Roles = r;
                _dbChangedSubject.OnNext(DBChangedMessage.Roles);
            }));

            _DBServicesDisposeBag.Add(DB.InvitesObservable().Subscribe(i =>
            {
                Invites = i;
                _dbChangedSubject.OnNext(DBChangedMessage.Invites);
            }));

            _dbChangedSubject.OnNext(DBChangedMessage.Cloud);
        }

        public void Login(LoginInformation loginInformation)
        {
            DB?.UserLogin(loginInformation);
        }

        public IDisposable Subscribe(IObserver<DBChangedMessage> observer)
        {
            return _changedObservable.Subscribe(observer);
        }
    }
}
