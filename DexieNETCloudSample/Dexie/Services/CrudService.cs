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
    public abstract partial class CrudService<T> : RxBLService, IDisposable where T : IIDPrimaryIndex, IDBStore, IDBCloudEntity
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

        protected override ValueTask ContextReadyAsync()
        {
            if (IsDBOpen)
            {
                InitDB();
            }

            _dbDisposable = DbService.Subscribe(s =>
            {
                if (s.ID == DbService.State.ID && DbService.State.Value is DBState.Cloud)
                {
                    InitDB();
                }
            });

            return ValueTask.CompletedTask;
        }


        public bool CanUpdate<Q>(IDBCloudEntity? entity, Expression<Func<T, Q>> query)
        {
            return entity is not null && (Permissions?.CanUpdate(entity, query)).True();
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
        }

        protected abstract Table<T, string> GetTable();

        protected abstract LiveQuery<IEnumerable<T>> InitializeDB(ToDoDB db);

        private void InitDB()
        {
            ArgumentNullException.ThrowIfNull(DbService.DB);

            if (DBDisposeBag.Count != 0)
            {
                throw new InvalidOperationException("DB not disposed");
            }

            var allItemsQuery = InitializeDB(DbService.DB);

            DBDisposeBag.Add(allItemsQuery.Subscribe(l =>
            {
#if DEBUG
                Console.WriteLine($"CRUD new items: {l.Aggregate(string.Empty, (p, n) => p += n.ToString())}");
#endif
                ItemsState.Value = l;
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
