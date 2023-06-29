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

        public override async Task OnInitializedAsync()
        {
            if (IsDBOpen)
            {
                await InitDB();
            }

            DbService.OnDelete += () => Dispose(false);

            _dbDisposable = DbService
               .Select(async c =>
               {
                   switch (c)
                   {
                       case DBChangedMessage.Cloud:
                           await InitDB();
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

        protected abstract ValueTask<Table<T, string>> GetTable();

        protected abstract ValueTask<LiveQuery<IEnumerable<T>>> InitializeDB(ToDoDB db);

        private async Task InitDB()
        {
            ArgumentNullException.ThrowIfNull(DbService.DB);

            if (DBDisposeBag.Any())
            {
                throw new InvalidOperationException("DB not disposed");
            }

            var allItemsQuery = await InitializeDB(DbService.DB);

            DBDisposeBag.Add(allItemsQuery.Subscribe(l =>
            {
#if DEBUG
                Console.WriteLine($"CRUD new items: {l.Aggregate(string.Empty, (p, n) => p += n.ToString())}");
#endif
                Items = l;
                StateHasChanged();
            }));

            Permissions = await GetTable().CreateUsePermissions();
            DBDisposeBag.Add(Permissions.Subscribe(_ =>
            {
                StateHasChanged();
            }));
        }

        protected virtual Task PostAddAction(string id) { return Task.CompletedTask; }
        protected virtual Task PreDeleteAction(T item) { return Task.CompletedTask; }
        protected virtual Task PreClearAction(IEnumerable<T> items) { return Task.CompletedTask; }
    }
}
