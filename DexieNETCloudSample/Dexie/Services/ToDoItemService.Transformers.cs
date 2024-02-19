using DexieNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoItemService
    {
        private class ToggledItemCompletedSST(ToDoItemService service) : ServiceStateTransformerAsync<ToDoItemService, ToDoDBItem>(service)
        {
            protected override async Task TransformStateAsync(ToDoDBItem value, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service._db);
                ArgumentNullException.ThrowIfNull(value);

                var completedItem = value with { Completed = !value.Completed };
                await Service._db.ToDoDBItems.Put(completedItem);
            }

            public override bool CanTransform(ToDoDBItem? value)
            {
                return Service.CanUpdate(value, i => i.Completed);
            }
        }

        private class DeleteCompletedItemsSSP(ToDoItemService service) : ServiceStateProviderAsync<ToDoItemService>(service)
        {
            protected override async Task ProvideStateAsync(CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service._db);

                var itemsToDelete = (await Service._db.ToDoDBItems
                    .Where(i => i.Completed)
                    .Equal(true)
                    .ToArray())
                    .Select(i => i.ID!);

                await Service._db.ToDoDBItems.BulkDelete(itemsToDelete);
            }

            protected override bool CanProvide()
            {
                if (Service.Items.HasValue())
                {
                    var item = Service.Items.Value.Where(i => i.Completed).FirstOrDefault();
                    return Service.CanDeleteItemDo(item);
                }

                return false;
            }
        }
    }
}
