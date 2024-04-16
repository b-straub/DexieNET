using DexieNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoListService
    {
        public Func<IStateCommandAsync, Task> ToggleListItemsOpenClose(ToDoDBList list) => async _ =>
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(list?.ID);

            var oc = await _db.ListOpenCloses.Get(list.ID);
            oc ??= new ListOpenClose(true, false, list.ID);

            oc = oc with { IsItemsOpen = !oc.IsItemsOpen };

            await _db.ListOpenCloses.Put(oc);
        };

        public Func<IStateCommandAsync, Task> ToggleListShareOpenClose(ToDoDBList list) => async _ =>
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(list?.ID);

            var oc = await _db.ListOpenCloses.Get(list.ID);
            ArgumentNullException.ThrowIfNull(oc);

            oc = oc with { IsShareOpen = !oc.IsShareOpen };

            await _db.ListOpenCloses.Put(oc);
        };

        public Func<bool> CanToggleListShareOpenClose(ToDoDBList? list) => () =>
        {
            return IsListItemsOpen(list);
        };

        public Action AcceptInvite(Invite invite) => () =>
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(invite.Id);

            _db.AcceptInvite(invite);
        };

        public static Func<bool> CanAcceptInvite(Invite? invite) => () =>
        {
            return invite?.Accepted is null;
        };

        public Action RejectInvite(Invite invite) => () =>
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(invite.Id);

            _db.RejectInvite(invite);
        };

        public static Func<bool> CanRejectInvite(Invite? invite) => () =>
        {
            return invite?.Rejected is null;
        };
    }
}
