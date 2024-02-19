using DexieNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoListService
    {
        private class ToggleListItemsOpenCloseSST(ToDoListService service) : ServiceStateTransformerAsync<ToDoListService, ToDoDBList>(service)
        {
            protected override async Task TransformStateAsync(ToDoDBList parameter, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service._db);
                ArgumentNullException.ThrowIfNull(parameter?.ID);

                var oc = await Service._db.ListOpenCloses.Get(parameter.ID);
                oc ??= new ListOpenClose(true, false, parameter.ID);

                oc = oc with { IsItemsOpen = !oc.IsItemsOpen };

                await Service._db.ListOpenCloses.Put(oc);
            }
        }

        private class ToggleListShareOpenCloseSST(ToDoListService service) : ServiceStateTransformerAsync<ToDoListService, ToDoDBList>(service)
        {
            protected override async Task TransformStateAsync(ToDoDBList value, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service._db);
                ArgumentNullException.ThrowIfNull(value?.ID);

                var oc = await Service._db.ListOpenCloses.Get(value.ID);
                ArgumentNullException.ThrowIfNull(oc);

                oc = oc with { IsShareOpen = !oc.IsShareOpen };

                await Service._db.ListOpenCloses.Put(oc);
            }

            public override bool CanTransform(ToDoDBList? value)
            {
                return Service.IsListItemsOpen(value);
            }
        }

        private class AcceptInviteSST(ToDoListService service) : ServiceStateTransformer<ToDoListService, Invite>(service)
        {
            protected override void TransformState(Invite value)
            {
                ArgumentNullException.ThrowIfNull(Service._db);
                ArgumentNullException.ThrowIfNull(value.Id);

                Service._db.AcceptInvite(value);
            }

            public override bool CanTransform(Invite? value)
            {
                return value?.Accepted is null;
            }
        }

        private class RejectInviteSST(ToDoListService service) : ServiceStateTransformer<ToDoListService, Invite>(service)
        {
            protected override void TransformState(Invite value)
            {
                ArgumentNullException.ThrowIfNull(Service._db);
                ArgumentNullException.ThrowIfNull(value.Id);

                Service._db.RejectInvite(value);
            }

            public override bool CanTransform(Invite? value)
            {
                return value?.Rejected is null;
            }
        }
    }
}
