using DexieNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoListService
    {
        private class ToggleListItemsOpenCloseCmd : CommandServiceAsync<ToDoListService, ToDoDBList>
        {
            public ToggleListItemsOpenCloseCmd(ToDoListService service) : base(service) { }

            protected override async Task DoExecute(ToDoDBList parameter, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service._db);
                ArgumentNullException.ThrowIfNull(parameter?.ID);

                var oc = await Service._db.ListOpenCloses.Get(parameter.ID);
                oc ??= new ListOpenClose(true, false, parameter.ID);

                oc = oc with { IsItemsOpen = !oc.IsItemsOpen };

                await Service._db.ListOpenCloses.Put(oc);
            }
        }

        private class ToggleListShareOpenCloseCmd : CommandServiceAsync<ToDoListService, ToDoDBList>
        {
            public ToggleListShareOpenCloseCmd(ToDoListService service) : base(service) { }

            protected override async Task DoExecute(ToDoDBList parameter, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service._db);
                ArgumentNullException.ThrowIfNull(parameter?.ID);

                var oc = await Service._db.ListOpenCloses.Get(parameter.ID);
                ArgumentNullException.ThrowIfNull(oc);

                oc = oc with { IsShareOpen = !oc.IsShareOpen };

                await Service._db.ListOpenCloses.Put(oc);
            }

            public override bool CanExecute(ToDoDBList? parameter)
            {
                return Service.IsListItemsOpen(parameter);
            }
        }

        private class AcceptInviteCmd : CommandService<ToDoListService, Invite>
        {
            public AcceptInviteCmd(ToDoListService service) : base(service) { }

            protected override void DoExecute(Invite parameter)
            {
                ArgumentNullException.ThrowIfNull(Service._db);
                ArgumentNullException.ThrowIfNull(parameter.Id);

                Service._db.AcceptInvite(parameter);
            }

            public override bool CanExecute(Invite? parameter)
            {
                return parameter?.Accepted is null;
            }
        }

        private class RejectInviteCmd : CommandService<ToDoListService, Invite>
        {
            public RejectInviteCmd(ToDoListService service) : base(service) { }

            protected override void DoExecute(Invite parameter)
            {
                ArgumentNullException.ThrowIfNull(Service._db);
                ArgumentNullException.ThrowIfNull(parameter.Id);

                Service._db.RejectInvite(parameter);
            }

            public override bool CanExecute(Invite? parameter)
            {
                return parameter?.Rejected is null;
            }
        }
    }
}
