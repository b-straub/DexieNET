using DexieNET;
using DexieCloudNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Linq.Expressions;
using R3;

namespace DexieNETCloudSample.Dexie.Services
{
    public abstract partial class CrudService<T> : RxBLService where T : IIDPrimaryIndex, IDBStore, IDBCloudEntity
    {
        public IState<IEnumerable<T>> ItemsState { get; }
        public bool IsDBOpen => DbService.DB is not null;
        public PushPayloadToDo? PushPayload => DbService.PushPayload;
        public SharePayload? SharePayload => DbService.SharePayload;
        
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
            if (DbService.State.Value is DBState.Cloud)
            {
                await InitDB();
            }

            _dbDisposable = DbService.AsChangedObservable(DbService.State)
                .Where(s => s is DBState.Cloud)
                .SubscribeAwait(async (_,_) => await InitDB());
        }


        public bool CanUpdate<Q>(IDBCloudEntity? entity, Expression<Func<T, Q>> query)
        {
            return entity is not null && (Permissions?.CanUpdate(entity, query)).True();
        }

        public void SetPushPayload(PushPayloadToDo? pushPayload)
        {
            DbService.SetPushPayload(pushPayload);
        }
    
        public void SetSharePayload(SharePayload? sharePayload)
        {
            DbService.SetSharePayload(sharePayload);
        }
        
        protected override void Dispose(bool disposing)
        {
            DBDisposeBag.Clear();
        
            if (disposing)
            {
                _dbDisposable?.Dispose();
            }
        }

        protected abstract Table<T, string> GetTable();

        protected abstract Task<ILiveQuery<IEnumerable<T>>> InitializeDB(ToDoDB db);

        private async Task InitDB()
        {
            ArgumentNullException.ThrowIfNull(DbService.DB);

            if (DBDisposeBag.Count != 0)
            {
                throw new InvalidOperationException("DB not disposed");
            }

            var allItemsQuery = await InitializeDB(DbService.DB);

            DBDisposeBag.Add(allItemsQuery.AsObservable
                .Subscribe(l =>
            {
#if DEBUG
                Console.WriteLine($"CRUD new items: {l.Aggregate(string.Empty, (p, n) => p += n.ToString())}");
#endif
                ItemsState.Value = l;
            }));
            
            Permissions = GetTable().CreateUsePermissions();
            DBDisposeBag.Add(Permissions.AsObservable.Subscribe(this));
        }

        protected virtual Task PostAddAction(string id) { return Task.CompletedTask; }
        protected virtual Task PostUpdateAction(string id) { return Task.CompletedTask; }
        protected virtual Task PreDeleteAction(string id) { return Task.CompletedTask; }
        protected virtual Task PostDeleteAction(string id) { return Task.CompletedTask; }
    }
}
