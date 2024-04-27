using DexieNET;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class ToDoItemService
    {

        public Func<IStateCommandAsync, Task> ToggledItemCompleted(ToDoDBItem item) => async _ =>
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(item);

            var completedItem = item with { Completed = !item.Completed };
            await _db.ToDoDBItems.Put(completedItem);
        };

        public Func<bool> CanToggledItemCompleted(ToDoDBItem? item) => () =>
        {
            return CanUpdate(item, i => i.Completed);
        };

        public Func<IStateCommandAsync, Task> DeleteCompletedItems => async _ =>
        {
            ArgumentNullException.ThrowIfNull(_db);

            var itemsToDelete = (await _db.ToDoDBItems
                .Where(i => i.Completed)
                .Equal(true)
                .ToArray())
                .Select(i => i.ID!);

            await _db.ToDoDBItems.BulkDelete(itemsToDelete);
        };

        public Func<bool> CanDeleteCompletedItems => () =>
        {
            var item = ItemsState.Value.Where(i => i.Completed).FirstOrDefault();
            return CanDelete(item);
        };


        public Func<bool> CanProvide() => () =>
        {
            if (ItemsState is not null)
            {
                var item = ItemsState.Value.Where(i => i.Completed).FirstOrDefault();
                return CanDelete(item);
            }

            return false;
        };
    }
}
