using System.Text.Json;
using DexieCloudNET;
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
            
            await _db.Transaction(async t =>
            {
                var id = await _db.ToDoDBItems.Put(completedItem);
                
                if (_db.PushNotifications.TransactionCollecting)
                {
                    await _db.PushNotifications.Put(null);
                    return;
                }
                await AddPushNotification(id, completedItem.Completed ? PnReason.COMPLETED : PnReason.REMINDER);
            });
        };

        public Func<bool> CanToggledItemCompleted(ToDoDBItem? item) => () =>
        {
            return CanUpdate(item, i => i.Completed);
        };

        public Func<IStateCommandAsync, Task> DeleteItems(bool completed) => async _ =>
        {
            ArgumentNullException.ThrowIfNull(_db);
            ArgumentNullException.ThrowIfNull(CurrentList.Value);
            
            var itemsToDelete = (completed ? await _db.ToDoDBItems
                .Where(i => i.ListID, CurrentList.Value.ID, i => i.Completed, true)
                .ToArray() : await _db.ToDoDBItems
                .Where(i => i.ListID, CurrentList.Value.ID)
                .ToArray()).ToArray();
            
            foreach (var item in itemsToDelete)
            {
                ArgumentNullException.ThrowIfNull(item.ID);

                await _db.Transaction(async t =>
                {
                    await PreDeleteAction(item.ID);
                    await GetTable().Delete(item.ID);
                    await PostDeleteAction(item.ID);
                });
            };
        };

        public Func<bool> CanDeleteCompletedItems => () =>
        {
            var item = ItemsState.Value.Where(i => i.Completed).FirstOrDefault();
            return CanDelete(item);
        };
    }
}
