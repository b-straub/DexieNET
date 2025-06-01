using DexieNET;
using DexieNETCloudSample.Extensions;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class CrudService<T>
    {
        
        public Func<IStateCommandAsync, Task> AddItem(T value)
        {
            return async _ => await DoAddItem(value);
        }
        
        public async Task DoAddItem(T value)
        {
            ArgumentNullException.ThrowIfNull(DbService.DB);
            
            await DbService.DB.Transaction(async t =>
            {
                var id = await GetTable().Add(value);
                await PostAddAction(id);
            });
        }

        public Func<bool> CanAddItem => () => CanAdd();

        public virtual bool CanAdd()
        {
            return (Permissions?.CanAdd()).True();
        }

        public Func<IStateCommandAsync, Task> UpdateItem(T value)
        {
            ArgumentNullException.ThrowIfNull(DbService.DB);

            return async _ =>
            {
                await DbService.DB.Transaction(async t =>
                {
                    ArgumentNullException.ThrowIfNull(DbService.DB);
                    var id = await GetTable().Put(value);
                    await PostUpdateAction(id);
                });
            };
        }

        public Func<bool> CanUpdateItem(T value) => () => CanUpdate(value);

        protected virtual bool CanUpdate(T value)
        {
            return true;
        }

        public Func<IStateCommandAsync, Task> DeleteItem(T value)
        {
            ArgumentNullException.ThrowIfNull(DbService.DB);
            ArgumentNullException.ThrowIfNull(value.ID);

            return async _ =>
            {
                await DbService.DB.Transaction(async t =>
                {
                    await PreDeleteAction(value.ID);
                    await GetTable().Delete(value.ID);
                    await PostDeleteAction(value.ID);
                });
            };
        }

        public Func<bool> CanDeleteItem(T value) => () => CanDelete(value);

        protected bool CanDelete(T? item)
        {
            return item is not null && (Permissions?.CanDelete(item)).True();
        }

        public Func<IStateCommandAsync, Task> ClearItems => async _ =>
        {
            ArgumentNullException.ThrowIfNull(DbService.DB);
            var itemsToClear = (await GetTable().ToArray()).ToArray();

            foreach (var item in itemsToClear)
            {
                ArgumentNullException.ThrowIfNull(item.ID);

                await DbService.DB.Transaction(async t =>
                {
                    await PreDeleteAction(item.ID);
                    await GetTable().Delete(item.ID);
                    await PostDeleteAction(item.ID);
                });
            }

            ;
        };

        public Func<bool> CanClearItems => () => CanClearItemsDo();

        private bool CanClearItemsDo()
        {
            return ItemsState.Value.Any(CanDelete);
        }

        public async Task<bool> OpenList(string listID)
        {
            return await DbService.OpenList(listID);
        }
    }
}
