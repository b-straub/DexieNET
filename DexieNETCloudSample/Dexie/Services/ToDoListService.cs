﻿using DexieNET;
using DexieCloudNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Buffers;
using System.Reactive.Linq;

namespace DexieNETCloudSample.Dexie.Services
{
    public sealed partial class ToDoListService : CrudService<ToDoDBList>
    {
        public IEnumerable<ToDoDBList> ToDoLists => ItemsState.Value;
        public IEnumerable<Invite> Invites => DbService.Invites.Value ?? [];
        public IState<IEnumerable<ListOpenClose>> ListOpenClose { get; }

        private ToDoDB? _db;

        public ToDoListService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            ListOpenClose = this.CreateState(Enumerable.Empty<ListOpenClose>());
        }

        public static ToDoDBList CreateList(string title, ToDoDBList? list = null)
        {
            return ToDoDBList.Create(title, list);
        }

        public bool IsListItemsOpen(ToDoDBList? list)
        {
            if (ListOpenClose.HasValue())
            {
                if (list is not null && ListOpenClose.Value.Any(l => l.ListID == list.ID) &&
                     ListOpenClose.Value.Where(l => l.ListID == list.ID).First().IsItemsOpen)
                {
                    return true;
                }
            }

            return false;
        }

        public bool IsListShareOpen(ToDoDBList? list)
        {
            if (ListOpenClose.HasValue())
            {
                if (list is not null && ListOpenClose.Value.Any(l => l.ListID == list.ID) &&
                 ListOpenClose.Value.Where(l => l.ListID == list.ID).First().IsShareOpen)
                {
                    return true;
                }
            }

            return false;
        }

        protected override Table<ToDoDBList, string> GetTable()
        {
            ArgumentNullException.ThrowIfNull(_db);
            return _db.ToDoDBLists;
        }

        protected override LiveQuery<IEnumerable<ToDoDBList>> InitializeDB(ToDoDB db)
        {
            _db = db;

            var listOpenQuery = _db.LiveQuery(GetListOpenCloseDo);
            DBDisposeBag.Add(listOpenQuery.Subscribe(i =>
            {
                ListOpenClose.Value = i;
            }));

            DBDisposeBag.Add(DbService.Subscribe(cr =>
            {
                if (cr.ID == DbService.Invites.ID)
                {
                    StateHasChanged();
                }
            }));

            return db.LiveQuery(async () => await GetTable().ToArray());
        }

        public Func<IStateCommandAsync, Task> DeleteMember(string? memberID) => async _ =>
        {
            ArgumentNullException.ThrowIfNull(_db);
            var currentUserId = _db.CurrentUserId();

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

            var oc = new ListOpenClose(false, true, listID);
            await _db.ListOpenCloses.Put(oc);
        }

        protected override async Task PreDeleteAction(string listID)
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(listID);

            await _db.ListOpenCloses
                .Where(l => l.ListID, listID)
                .Delete();

            // Delete todo items
            await _db.ToDoDBItems
                .Where(i => i.ListID, listID)
                .Delete();
        }

        protected override async Task PostDeleteAction(string listID)
        {
            ArgumentNullException.ThrowIfNull(_db);

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
