﻿using DexieNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Buffers;
using System.Reactive.Linq;

namespace DexieNETCloudSample.Dexie.Services
{
    public sealed partial class ToDoListService : CrudService<ToDoDBList>
    {
        public IEnumerable<ToDoDBList> Lists => Items;
        public IEnumerable<Invite> Invites { get; private set; } = Enumerable.Empty<Invite>();

        public IEnumerable<ListOpenClose> ListOpenClose { get; private set; }

        // Commands
        public ICommandAsync<ToDoDBList> AddList => AddItem;
        public ICommandAsync<ToDoDBList> UpdateList => UpdateItem;
        public ICommandAsync<ToDoDBList> DeleteList => DeleteItem;
        public ICommandAsync ClearLists => ClearItems;
        public ICommandAsync<ToDoDBList> ToggleListItemsOpenClose => new ToggleListItemsOpenCloseCmd(this);
        public ICommandAsync<ToDoDBList> ToggleListShareOpenClose => new ToggleListShareOpenCloseCmd(this);
        public ICommand<Invite> AcceptInvite => new AcceptInviteCmd(this);
        public ICommand<Invite> RejectInvite => new RejectInviteCmd(this);

        private ToDoDB? _db;

        public ToDoListService(DexieCloudService databaseService) : base(databaseService)
        {
            ListOpenClose = Enumerable.Empty<ListOpenClose>();
        }

        public static ToDoDBList CreateList(string title, ToDoDBList? list = null)
        {
            return ToDoDBList.Create(title, list);
        }

        public bool IsListItemsOpen(ToDoDBList? list)
        {
            if (list is not null && ListOpenClose.Any(l => l.ListID == list.ID) &&
                 ListOpenClose.Where(l => l.ListID == list.ID).First().IsItemsOpen)
            {
                return true;
            }

            return false;
        }

        public bool IsListShareOpen(ToDoDBList? list)
        {
            if (list is not null && ListOpenClose.Any(l => l.ListID == list.ID) &&
                 ListOpenClose.Where(l => l.ListID == list.ID).First().IsShareOpen)
            {
                return true;
            }

            return false;
        }

        protected override async ValueTask<Table<ToDoDBList, string>> GetTable()
        {
            ArgumentNullException.ThrowIfNull(_db);
            return await _db.ToDoDBLists();
        }

        protected override async ValueTask<LiveQuery<IEnumerable<ToDoDBList>>> InitializeDB(ToDoDB db)
        {
            _db = db;

            var listOpenQuery = await _db.LiveQuery(GetListOpenCloseDo);
            DBDisposeBag.Add(listOpenQuery.Subscribe(i =>
            {
                ListOpenClose = i;
                StateHasChanged();
            }));

            if (DbService.Invites is not null)
            {
                Invites = DbService.Invites;
            }

            DBDisposeBag.Add(DbService.Subscribe(i =>
            {
                if (i is DBChangedMessage.Invites && DbService.Invites is not null)
                {
                    Invites = DbService.Invites;
                    StateHasChanged();
                }
            }));

            return await db.LiveQuery(async () => await GetTable().ToArray());
        }

        private async ValueTask<IEnumerable<ListOpenClose>> GetListOpenCloseDo()
        {
            ArgumentNullException.ThrowIfNull(_db);
            return await _db.ListOpenCloses().ToArray();
        }

        protected override async Task PostAddAction(string id)
        {
            ArgumentNullException.ThrowIfNull(_db);

            await _db.Transaction(async _ =>
            {
                var oc = new ListOpenClose(false, false, id);
                await _db.ListOpenCloses().Put(oc);
            });
        }

        protected override async Task PreClearAction(IEnumerable<ToDoDBList> lists)
        {
            ArgumentNullException.ThrowIfNull(_db);

            await _db.Transaction(async t =>
            {
                foreach (var list in lists)
                {
                    await PreDeleteAction(list);
                }
            });
        }

        protected override async Task PreDeleteAction(ToDoDBList list)
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(list.ID);

            await _db.Transaction(async _ =>
            {
                await _db.ListOpenCloses()
                    .Where(l => l.ListID, list.ID)
                    .Delete();

                // Delete todo items
                await _db.ToDoDBItems()
                    .Where(i => i.ListID, list.ID)
                    .Delete();

                // Delete any tied realm and related access.
                // If it wasn't shared, this is a no-op but do
                // it anyway to make this operation consistent
                // in case it was shared by other offline
                // client and then syncs.
                // No need to delete members - they will be deleted
                // automatically when the realm is deleted.
                var tiedRealmId = _db.GetTiedRealmID(list.ID);
                await _db.Realms().Delete(tiedRealmId);
            });
        }
    }
}
