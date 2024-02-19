using DexieNET;
using DexieNETCloudSample.Extensions;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class CrudService<T>
    {
        private class AddItemSST(CrudService<T> service) : ServiceStateTransformerAsync<CrudService<T>, T>(service)
        {
            protected override async Task TransformStateAsync(T value, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service.DbService.DB);

                await Service.DbService.DB.Transaction(async _ =>
                {
                    var id = await Service.GetTable().Add(value);
                    await Service.PostAddAction(id);
                });
            }

            public override bool CanTransform(T? _)
            {
                return Service.CanAdd();
            }
        }

        protected virtual bool CanAdd()
        {
            return (Permissions?.CanAdd()).True();
        }

        private class UpdateItemSST(CrudService<T> service) : ServiceStateTransformerAsync<CrudService<T>, T>(service)
        {
            protected override async Task TransformStateAsync(T value, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service.DbService.DB);

                await Service.GetTable().Put(value);
            }

            public override bool CanTransform(T? value)
            {
                return Service.CanUpdate(value);
            }
        }

        protected virtual bool CanUpdate(T? item)
        {
            return true;
        }

        private class DeleteItemSST(CrudService<T> service) : ServiceStateTransformerAsync<CrudService<T>, T>(service)
        {
            protected override async Task TransformStateAsync(T value, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service.DbService.DB);
                ArgumentNullException.ThrowIfNull(value.ID);

                await Service.DbService.DB.Transaction(async _ =>
                {
                    await Service.PreDeleteAction(value.ID);
                    await Service.GetTable().Delete(value.ID);
                    await Service.PostDeleteAction(value.ID);
                });
            }

            public override bool CanTransform(T? parameter)
            {
                return Service.CanDeleteItemDo(parameter);
            }
        }

        protected bool CanDeleteItemDo(T? item)
        {
            return item is not null && (Permissions?.CanDelete(item)).True();
        }

        private class ClearItemsSSP(CrudService<T> service) : ServiceStateProviderAsync<CrudService<T>>(service)
        {
            protected override async Task ProvideStateAsync(CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service.DbService.DB);
                var itemsToClear = await Service.GetTable().ToArray();

                foreach (var item in itemsToClear)
                {
                    ArgumentNullException.ThrowIfNull(item.ID);

                    await Service.DbService.DB.Transaction(async _ =>
                    {
                        await Service.PreDeleteAction(item.ID);
                        await Service.GetTable().Delete(item.ID);
                        await Service.PostDeleteAction(item.ID);
                    });
                }
            }

            protected override bool CanProvide()
            {
                return Service.CanClearItemsDo();
            }
        }

        private bool CanClearItemsDo()
        {
            return Items.HasValue() && Items.Value.Any(CanDeleteItemDo);
        }
    }
}
