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
    public abstract partial class CrudService<T> : RxBLService, IDisposable where T : IIDPrimaryIndex, IDBStore, IDBCloudEntity
    {
        public class CrudServiceScope(CrudService<T> service) : IRxBLScope
        {
            public IServiceStateTransformer<T> AddItem { get; } = new AddItemSST(service);
            public IServiceStateTransformer<T> UpdateItem { get; } = new UpdateItemSST(service);
            public IServiceStateTransformer<T> DeleteItem { get; } = new DeleteItemSST(service);
            public IServiceStateProvider ClearItems { get; } = new ClearItemsSSP(service);
        }

        public IState<IEnumerable<T>> Items { get; }

        public bool IsDBOpen => DbService.DB is not null;

        // Transformers
        protected CompositeDisposable DBDisposeBag { get; } = [];
        protected DexieCloudService DbService { get; }

        protected IUsePermissions<T>? Permissions { get; private set; }

        private IDisposable? _dbDisposable;
        private readonly IState<Unit> _permissionsChanged;

        public CrudService(IServiceProvider serviceProvider)
        {
            DbService = serviceProvider.GetRequiredService<DexieCloudService>();
            Items = this.CreateState(Enumerable.Empty<T>());
            _permissionsChanged = this.CreateState(Unit.Default);
        }

        protected override ValueTask ContextReadyAsync()
        {
            if (IsDBOpen)
            {
                InitDB();
            }

            DbService.OnDelete += () => Dispose(false);

            _dbDisposable = DbService.Subscribe(s =>
            {
                if (s == (ChangeReason.STATE, DbService.State.ID) && DbService.State.Value is DBState.Cloud)
                {
                    InitDB();
                }
            });

            return ValueTask.CompletedTask;
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
                Items.Transform(l);
            }));

            Permissions = GetTable().CreateUsePermissions();
            DBDisposeBag.Add(Permissions.Subscribe(_ =>
            {
                _permissionsChanged.Transform(Unit.Default);
            }));
        }
        protected virtual Task PostAddAction(string id) { return Task.CompletedTask; }
        protected virtual Task PreDeleteAction(string id) { return Task.CompletedTask; }
        protected virtual Task PostDeleteAction(string id) { return Task.CompletedTask; }
    }
}
