using DexieNET;
using DexieCloudNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Linq.Expressions;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace DexieNETCloudSample.Dexie.Services
{
    public abstract partial class CrudService<T> : RxBLService where T : IIDPrimaryIndex, IDBStore, IDBCloudEntity
    {
        public IState<IEnumerable<T>> ItemsState { get; }
        public bool IsDBOpen => DbService.DB is not null;

        // Transformers
        protected CompositeDisposable DBDisposeBag { get; } = [];
        protected DexieCloudService DbService { get; }

        protected IUsePermissions<T>? Permissions { get; private set; }

        private IDisposable? _dbDisposable;

        public CrudService(IServiceProvider serviceProvider)
        {
            ItemsState = this.CreateState(Enumerable.Empty<T>());

            DbService = serviceProvider.GetRequiredService<DexieCloudService>();
        }

        protected override async ValueTask ContextReadyAsync()
        {
            if (IsDBOpen)
            {
                await InitDB();
            }

            _dbDisposable = DbService
                .Where(s => s.ID == DbService.State.ID && DbService.State.Value is DBState.Cloud)
                .Select(async s => await InitDB())
                .Subscribe();
        }


        public bool CanUpdate<Q>(IDBCloudEntity? entity, Expression<Func<T, Q>> query)
        {
            return entity is not null && (Permissions?.CanUpdate(entity, query)).True();
        }

        protected override void Dispose(bool disposing)
        {
            DBDisposeBag.Clear();
            Permissions?.Dispose();
            Permissions = null;

            if (disposing)
            {
                _dbDisposable?.Dispose();
            }
        }

        protected abstract Table<T, string> GetTable();

        protected abstract Task<LiveQuery<IEnumerable<T>>> InitializeDB(ToDoDB db);

        private async Task InitDB()
        {
            ArgumentNullException.ThrowIfNull(DbService.DB);

            if (DBDisposeBag.Count != 0)
            {
                throw new InvalidOperationException("DB not disposed");
            }

            var allItemsQuery = await InitializeDB(DbService.DB);

            DBDisposeBag.Add(allItemsQuery
                .Subscribe(l =>
            {
#if DEBUG
                Console.WriteLine($"CRUD new items: {l.Aggregate(string.Empty, (p, n) => p += n.ToString())}");
#endif
                ItemsState.Value = l;
                ItemsChanged();
            }));
            
            Permissions = GetTable().CreateUsePermissions();
            DBDisposeBag.Add(Permissions.Subscribe(this));
        }

        protected virtual Task PostAddAction(string id) { return Task.CompletedTask; }
        protected virtual Task PostUpdateAction(string id) { return Task.CompletedTask; }
        protected virtual Task PreDeleteAction(string id) { return Task.CompletedTask; }
        protected virtual Task PostDeleteAction(string id) { return Task.CompletedTask; }
        protected virtual void ItemsChanged() { }
    }
}
