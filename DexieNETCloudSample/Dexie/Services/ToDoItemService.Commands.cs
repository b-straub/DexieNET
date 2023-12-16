using DexieNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoItemService
    {
        private class SetListCmd(ToDoItemService service) : CommandService<ToDoItemService, ToDoDBList>(service)
        {
            protected override void DoExecute(ToDoDBList parameter)
            {
                Service.CurrentList = parameter;
            }
        }

        private class ToggledItemCompletedCmd(ToDoItemService service) : CommandServiceAsync<ToDoItemService, ToDoDBItem>(service)
        {
            protected override async Task DoExecute(ToDoDBItem parameter, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service._db);
                ArgumentNullException.ThrowIfNull(parameter);

                var completedItem = parameter with { Completed = !parameter.Completed };
                await Service._db.ToDoDBItems.Put(completedItem);
            }

            public override bool CanExecute(ToDoDBItem? parameter)
            {
                return Service.CanUpdate(parameter, i => i.Completed);
            }
        }

        private class DeleteCompletedItemsCmd(ToDoItemService service) : CommandServiceAsync<ToDoItemService>(service)
        {
            protected override async Task DoExecute(CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service._db);

                var itemsToDelete = (await Service._db.ToDoDBItems
                    .Where(i => i.Completed)
                    .Equal(true)
                    .ToArray())
                    .Select(i => i.ID!);

                await Service._db.ToDoDBItems.BulkDelete(itemsToDelete);
            }

            public override bool CanExecute()
            {
                var item = Service.Items.Where(i => i.Completed).FirstOrDefault();
                return Service.CanDeleteItemDo(item);
            }
        }
    }
}
