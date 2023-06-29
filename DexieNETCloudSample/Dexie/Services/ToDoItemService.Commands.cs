using DexieNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoItemService
    {
        private class SetListCmd : CommandService<ToDoItemService, ToDoDBList>
        {
            public SetListCmd(ToDoItemService service) : base(service) { }

            protected override void DoExecute(ToDoDBList parameter)
            {
                Service.List = parameter;
            }
        }

        private class ToggledItemCompletedCmd : CommandServiceAsync<ToDoItemService, ToDoDBItem>
        {
            public ToggledItemCompletedCmd(ToDoItemService service) : base(service) { }

            protected override async Task DoExecute(ToDoDBItem parameter, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service._db);
                ArgumentNullException.ThrowIfNull(parameter);

                var completedItem = parameter with { Completed = !parameter.Completed };
                await Service._db.ToDoDBItems().Put(completedItem);
            }

            public override bool CanExecute(ToDoDBItem? parameter)
            {
                return Service.CanUpdate(parameter, i => i.Completed);
            }
        }

        private class DeleteCompletedItemsCmd : CommandServiceAsync<ToDoItemService>
        {
            public DeleteCompletedItemsCmd(ToDoItemService service) : base(service) { }

            protected override async Task DoExecute(CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service._db);

                var itemsToDelete = (await Service._db.ToDoDBItems()
                    .Where(i => i.Completed)
                    .Equal(true)
                    .ToArray())
                    .Select(i => i.ID!);

                await Service._db.ToDoDBItems().BulkDelete(itemsToDelete);
            }

            public override bool CanExecute()
            {
                var item = Service.Items.Where(i => i.Completed).FirstOrDefault();
                return Service.CanDeleteItemDo(item);
            }
        }
    }
}
