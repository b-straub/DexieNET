using DexieNET;
using DexieCloudNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoListService
    {
        protected override bool CanUpdate(ToDoDBList? item)
        {
            return CanUpdate(item, i => i.Title);
        }

        public Func<IStateCommandAsync, Task> ToggleListItemsOpenClose(ToDoDBList list) => async _ =>
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(list.ID);
            
            await _db.Transaction(async t =>
            {
                var oc = await _db.ListOpenCloses.Get(list.ID);
                if (!t.Collecting)
                {
                    oc ??= new ListOpenClose(false, false, list.ID);
                    oc = oc with { IsItemsOpen = !oc.IsItemsOpen };
                }
                await _db.ListOpenCloses.Put(oc);
            });
        };

        public Func<IStateCommandAsync, Task> ToggleListShareOpenClose(ToDoDBList list) => async _ =>
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(list?.ID);

            await _db.Transaction(async t =>
            {
                var oc = await _db.ListOpenCloses.Get(list.ID);
                if (!t.Collecting)
                {
                    ArgumentNullException.ThrowIfNull(oc);
                    oc = oc with { IsShareOpen = !oc.IsShareOpen };
                }
                await _db.ListOpenCloses.Put(oc);
            });
        };

        public Func<bool> CanToggleListShareOpenClose(ToDoDBList? list) => () => { return IsListItemsOpen(list); };

        public Action AcceptInvite(Invite invite) => () =>
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(invite.Id);

            _db.AcceptInvite(invite);
        };

        public static Func<bool> CanAcceptInvite(Invite? invite) => () => { return invite?.Accepted is null; };

        public Action RejectInvite(Invite invite) => () =>
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(invite.Id);

            _db.RejectInvite(invite);
        };

        public static Func<bool> CanRejectInvite(Invite? invite) => () => { return invite?.Rejected is null; };
        
        public void SetPushPayloadEvent(PushPayload pushPayload)
        {
            DbService.SetPushPayloadEvent(pushPayload);
        }
    }
}