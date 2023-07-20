using DexieNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace DexieNETCloudSample.Dexie.Services
{
    public abstract partial class CrudService<T> : RxBLServiceBase, IDisposable where T : IIDPrimaryIndex, IDBStore, IDBCloudEntity
    {
        public IEnumerable<T> Items { get; private set; }

        public bool IsDBOpen => DbService.DB is not null;

        // Commands
        public ICommandAsync<T> AddItem { get; }
        public ICommandAsync<T> UpdateItem => new UpdateItemCmd(this);
        public ICommandAsync<T> DeleteItem => new DeleteItemCmd(this);
        public ICommandAsync ClearItems { get; }

        protected CompositeDisposable DBDisposeBag { get; } = new();
        protected DexieCloudService DbService { get; }

        protected IUsePermissions<T>? Permissions { get; private set; }

        private IDisposable? _dbDisposable;

        public CrudService(DexieCloudService databaseService)
        {
            DbService = databaseService;
            Items = Enumerable.Empty<T>();

            AddItem = new AddItemCmd(this);
            ClearItems = new ClearItemsCmd(this);
        }

        public override void OnInitialized()
        {
            if (IsDBOpen)
            {
                InitDB();
            }

            DbService.OnDelete += () => Dispose(false);

            _dbDisposable = DbService
               .Select(c =>
               {
                   switch (c)
                   {
                       case DBChangedMessage.Cloud:
                           InitDB();
                           break;
                   };

                   return Unit.Default;
               }).Subscribe(_ => StateHasChanged());
        }


        public bool CanUpdate<Q>(T? item, Expression<Func<T, Q>> query)
        {
            return item is not null && (Permissions?.CanUpdate(item, query)).True();
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }


        protected virtual void Dispose(bool disposing)
        {
            DBDisposeBag.Clear();
            Permissions?.Dispose();
            Permissions = null;

            if (disposing)
            {
                _dbDisposable?.Dispose();
            }

            DbService.OnDelete -= () => Dispose(false);
        }

        protected abstract Table<T, string> GetTable();

        protected abstract LiveQuery<IEnumerable<T>> InitializeDB(ToDoDB db);

        private void InitDB()
        {
            ArgumentNullException.ThrowIfNull(DbService.DB);

            if (DBDisposeBag.Any())
            {
                throw new InvalidOperationException("DB not disposed");
            }

            var allItemsQuery = InitializeDB(DbService.DB);

            DBDisposeBag.Add(allItemsQuery.Subscribe(l =>
            {
#if DEBUG
                Console.WriteLine($"CRUD new items: {l.Aggregate(string.Empty, (p, n) => p += n.ToString())}");
#endif
                Items = l;
                StateHasChanged();
            }));

            Permissions = GetTable().CreateUsePermissions();
            DBDisposeBag.Add(Permissions.Subscribe(_ =>
            {
                StateHasChanged();
            }));
        }
        protected virtual Task PostAddAction(string id) { return Task.CompletedTask; }
        protected virtual Task PreDeleteAction(string id) { return Task.CompletedTask; }
        protected virtual Task PostDeleteAction(string id) { return Task.CompletedTask; }
    }
}
