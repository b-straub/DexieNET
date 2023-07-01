using DexieNET;
using DexieNETCloudSample.Extensions;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Dexie.Services
{
    public partial class CrudService<T>
    {
        private class AddItemCmd : CommandServiceAsync<CrudService<T>, T>
        {
            public AddItemCmd(CrudService<T> service) : base(service) { }

            protected override async Task DoExecute(T parameter, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service.DbService.DB);

                await Service.DbService.DB.Transaction(async _ =>
                {
                    var id = await Service.GetTable().Add(parameter);
                    await Service.PostAddAction(id);
                });
            }

            public override bool CanExecute(T? parameter)
            {
                return Service.CanAdd();
            }
        }

        protected virtual bool CanAdd()
        {
            return (Permissions?.CanAdd()).True();
        }

        private class UpdateItemCmd : CommandServiceAsync<CrudService<T>, T>
        {
            public UpdateItemCmd(CrudService<T> service) : base(service) { }

            protected override async Task DoExecute(T parameter, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service.DbService.DB);

                await Service.GetTable().Put(parameter);
            }

            public override bool CanExecute(T? parameter)
            {
                return Service.CanUpdate(parameter);
            }
        }

        protected virtual bool CanUpdate(T? item)
        {
            return item is not null;
        }

        private class DeleteItemCmd : CommandServiceAsync<CrudService<T>, T>
        {
            public DeleteItemCmd(CrudService<T> service) : base(service) { }

            protected override async Task DoExecute(T parameter, CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service.DbService.DB);
                ArgumentNullException.ThrowIfNull(parameter.ID);

                await Service.DbService.DB.Transaction(async _ =>
                {
                    await Service.PreDeleteAction(parameter.ID);
                    await Service.GetTable().Delete(parameter.ID);
                    await Service.PostDeleteAction(parameter.ID);
                });
            }

            public override bool CanExecute(T? parameter)
            {
                return Service.CanDeleteItemDo(parameter);
            }
        }

        protected bool CanDeleteItemDo(T? item)
        {
            return item is not null && (Permissions?.CanDelete(item)).True();
        }

        private class ClearItemsCmd : CommandServiceAsync<CrudService<T>>
        {
            public ClearItemsCmd(CrudService<T> service) : base(service) { }

            protected override async Task DoExecute(CancellationToken cancellationToken)
            {
                ArgumentNullException.ThrowIfNull(Service.DbService.DB);
                var itemsToClear = await Service.GetTable().ToArray();

                foreach (var item in itemsToClear)
                {
                    await Service.DeleteItem.Execute(item);
                }
            }

            public override bool CanExecute()
            {
                return Service.CanClearItemsDo();
            }
        }

        private bool CanClearItemsDo()
        {
            var item = Items.FirstOrDefault();
            return CanDeleteItemDo(item);
        }
    }
}
