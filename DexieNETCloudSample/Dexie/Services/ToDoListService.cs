using System.Reactive;
using System.Reactive.Linq;
using DexieNET;
using DexieCloudNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public sealed partial class ToDoListService : CrudService<ToDoDBList>
    {
        public IEnumerable<ToDoDBList> ToDoLists => ItemsState.Value;
        public IEnumerable<Invite> Invites => DbService.Invites.Value ?? [];

        private readonly IState<IEnumerable<ListOpenClose>> _listOpenClose;

        private ToDoDB? _db;

        public ToDoListService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _listOpenClose = this.CreateState(Enumerable.Empty<ListOpenClose>());
        }

        public static ToDoDBList CreateList(string title, ToDoDBList? list = null)
        {
            return ToDoDBList.Create(title, list);
        }
        
        public bool IsListItemsOpen(ToDoDBList? list)
        {
            if (!_listOpenClose.HasValue() || list is null) return false;
            
            var open = _listOpenClose.Value.Any(l => l.ListID == list.ID) &&
                   _listOpenClose.Value.First(l => l.ListID == list.ID).IsItemsOpen;
            return open;
        }

        public bool IsListShareOpen(ToDoDBList? list)
        {
            if (!_listOpenClose.HasValue() || list is null) return false;
            
            return _listOpenClose.Value.Any(l => l.ListID == list.ID) &&
                   _listOpenClose.Value.First(l => l.ListID == list.ID).IsShareOpen;
        }

        protected override Table<ToDoDBList, string> GetTable()
        {
            ArgumentNullException.ThrowIfNull(_db);
            return _db.ToDoDBLists;
        }

        protected override Task<LiveQuery<IEnumerable<ToDoDBList>>> InitializeDB(ToDoDB db)
        {
            _db = db;

            var listOpenQuery = _db.LiveQuery(GetListOpenCloseDo);
            DBDisposeBag.Add(listOpenQuery.Subscribe(i => { _listOpenClose.Value = i; }));

            DBDisposeBag.Add(DbService.Where(cr => cr.ID == DbService.Invites.ID)
                .Select(_ => Unit.Default)
                .Subscribe(this));
            
            return Task.FromResult(db.LiveQuery(async () => await GetTable().ToArray()));
        }

        public Func<IStateCommandAsync, Task> DeleteMember(string? memberID) => async _ =>
        {
            ArgumentNullException.ThrowIfNull(_db);

            if (memberID is not null)
            {
                await _db.Members.Delete(memberID);
            }
        };

        private async ValueTask<IEnumerable<ListOpenClose>> GetListOpenCloseDo()
        {
            ArgumentNullException.ThrowIfNull(_db);
            return await _db.ListOpenCloses.ToArray();
        }

        protected override async Task PostAddAction(string listID)
        {
            ArgumentNullException.ThrowIfNull(_db);

            if (_db.ListOpenCloses.TransactionCollecting)
            {
                await _db.ListOpenCloses.Put(null);
                return;
            }

            var oc = new ListOpenClose(false, true, listID);
            await _db.ListOpenCloses.Put(oc);
        }

        protected override async Task PreDeleteAction(string? listID)
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(listID);

            await _db.ListOpenCloses
                .Where(l => l.ListID, listID)
                .Delete();
            
            var toDoItems = await _db.ToDoDBItems
                .Where(i => i.ListID, listID).ToArray();
            
            await _db.PushNotifications.ExpireNotifications(toDoItems.Select(i => i.ID));
            
            // Delete todo items
            await _db.ToDoDBItems
                .Where(i => i.ListID, listID)
                .Delete();
        }

        protected override async Task PostDeleteAction(string listID)
        {
            ArgumentNullException.ThrowIfNull(_db);

            if (_db.Realms.TransactionCollecting)
            {
                await _db.Realms.Put(null);
                return;
            }

            // Delete any tied realm and related access.
            // If it wasn't shared, this is a no-op but do
            // it anyway to make this operation consistent
            // in case it was shared by other offline
            // client and then syncs.
            // No need to delete members - they will be deleted
            // automatically when the realm is deleted.
            var tiedRealmId = _db.GetTiedRealmID(listID);
            await _db.Realms.Delete(tiedRealmId);
        }
    }
}