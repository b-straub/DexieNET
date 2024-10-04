using System.Reactive;
using DexieNET;
using DexieCloudNET;
//using DexieNETCloudSample.Aministration;
using RxBlazorLightCore;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using DexieNETCloudSample.Dexie.Services;
using DexieNETCloudSample.Extensions;

namespace DexieNETCloudSample.Logic
{
    [DBName("ToDoDB")]
    [DBAddPushSupport]
    public interface IToDoDBItem : IDBStore
    {
    }

    public interface IIDPrimaryIndex
    {
        string? ID { get; }
    }

    [Schema(CloudSync = true)]
    [CompoundIndex("ListID", "RealmId")]
    [CompoundIndex("ListID", "Completed")]
    public partial record ToDoDBItem(
        [property: Index] string Text,
        [property: Index] DateTime DueDate,
        [property: BoolIndex] bool Completed,
        [property: Index] string ListID,
        [property: Index(IsPrimary = true, IsAuto = true)]
        string? ID
    ) : IToDoDBItem, IIDPrimaryIndex
    {
        public static ToDoDBItem Create(string text, DateTime dueDate, ToDoDBList list, ToDoDBItem? item)
        {
            ArgumentNullException.ThrowIfNull(list.ID);

            var newItem = new ToDoDBItem(text, dueDate, item is not null && item.Completed, list.ID, item?.ID);

            if (item is not null)
            {
                newItem = newItem with { Owner = item.Owner, RealmId = item.RealmId };
            }
            else
            {
                newItem = newItem with { RealmId = list.RealmId };
            }

            return newItem;
        }
    }

    [Schema(CloudSync = true)]
    public partial record ToDoDBList(
        [property: Index] string Title,
        [property: Index(IsPrimary = true, IsAuto = true)]
        string? ID
    ) : IToDoDBItem, IIDPrimaryIndex
    {
        public static ToDoDBList Create(string title, ToDoDBList? list = null)
        {
            var newList = new ToDoDBList(title, list?.ID);

            if (list is not null)
            {
                newList = newList with { Owner = list.Owner, RealmId = list.RealmId };
            }

            return newList;
        }
    }

    public record ListOpenClose(
        bool IsShareOpen,
        bool IsItemsOpen,
        [property: Index(IsPrimary = true)] string? ListID
    ) : IToDoDBItem
    {
        public static ListOpenClose Create(string listID)
        {
            return new(false, false, listID);
        }
    }

    public record ColorTheme(bool Light);

    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(ColorTheme))]
    public partial class ColorThemeConfigContext : JsonSerializerContext
    {
    }
    
    [Schema(CloudSync = true)]
    public partial record Settings(
        [property: Index(IsPrimary = true)] string ID,
        string ValueJson
    ) : IToDoDBItem
    {
        public const string ColorThemeKey = "#colorTheme";

        public static Settings CreateColorTheme(bool light)
        {
            var theme = new ColorTheme(light);
            var themeJson = JsonSerializer.Serialize(theme,
                ColorThemeConfigContext.Default.ColorTheme);

            return new Settings(ColorThemeKey, themeJson);
        }
        
        public ColorTheme? GetColorTheme()
        {
            var theme = JsonSerializer.Deserialize(ValueJson,
                ColorThemeConfigContext.Default.ColorTheme);

            return theme;
        }
    }

    public enum DBState
    {
        Closed,
        Opened,
        Cloud
    }

    public sealed partial class DexieCloudService : RxBLService
    {
        public ToDoDB? DB { get; private set; }
        public bool IsDBOpen => DB is not null;

        public IState<DBState> State { get; }
        public IState<SyncState?> SyncState { get; }
        public IState<ServiceWorkerNotifications?> ServiceWorkerNotificationState { get; }
        public IState<UserLogin?> UserLogin { get; }
        public IState<UIInteraction?> UIInteraction { get; }
        public IState<IEnumerable<Invite>?> Invites { get; }
        public IState<Dictionary<string, Role>?> Roles { get; }
        public IState<bool> LightMode { get; }
        public IState<NotificationState> NotificationsState { get; }
        public IStateObserverAsync LogoutObserver { get; }
        public string? CloudURL { get; private set; }
        private ILogger Logger { get; }

        private readonly IDexieNETFactory<ToDoDB> _dexieFactory;
        private readonly CompositeDisposable _DBServicesDisposeBag = [];

        public DexieCloudService(IServiceProvider serviceProvider)
        {
            Logger = serviceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger<DexieCloudService>();

            var dexieService = serviceProvider.GetRequiredService<IDexieNETService<ToDoDB>>();
            _dexieFactory = dexieService.DexieNETFactory;

            State = this.CreateState(DBState.Closed);
            SyncState = this.CreateState((SyncState?)null);
            ServiceWorkerNotificationState = this.CreateState((ServiceWorkerNotifications?)null);
            UserLogin = this.CreateState((UserLogin?)null);
            UIInteraction = this.CreateState((UIInteraction?)null);
            Invites = this.CreateState((IEnumerable<Invite>?)null);
            Roles = this.CreateState((Dictionary<string, Role>?)null);
            LightMode = this.CreateState(true);
            NotificationsState = this.CreateState(NotificationState.None);
            LogoutObserver = this.CreateStateObserverAsync();
        }

        public async ValueTask OpenDB()
        {
            if (DB is null)
            {
                Logger.LogDebug("OpenDB");

                await _dexieFactory.Delete();
                DB = await _dexieFactory.Create();
                DB.Version(17).Stores();

                ArgumentNullException.ThrowIfNull(DB);
                State.Value = DBState.Opened;
            }
        }

        public async Task ConfigureCloud(string cloudURL, string? applicationServerKey = null)
        {
            if (CloudURL == cloudURL)
            {
                return;
            }

            Logger.LogDebug("ConfigureCloud with for {URL} and applicationServerKey {KEY}", cloudURL,
                applicationServerKey);

            ArgumentNullException.ThrowIfNull(DB);

            CloudURL = cloudURL;

            var options = new DexieCloudOptions(CloudURL)
                    .WithCustomLoginGui(true)
                    .WithRequireAuth(false)
#if !DEBUG
                    .WithTryUseServiceWorker(true)
#endif
            ;
            //.WithFetchTokens(FetchTokens);

            // call before configure cloud to have login UI ready when needed
            _DBServicesDisposeBag.Add(DB.UserInteractionObservable().Subscribe(ui => { UIInteraction.Value = ui; }));

            await DB.ConfigureCloud(options, applicationServerKey);

            _DBServicesDisposeBag.Add(DB.SyncStateObservable().Subscribe(ss => { SyncState.Value = ss; }));

            _DBServicesDisposeBag.Add(DB.PersistedSyncStateStateObservable().Subscribe(pss =>
            {
                Logger.LogDebug("PersistedSyncStateStateObservable {VALUE}", pss);
            }));

            _DBServicesDisposeBag.Add(DB.WebSocketStatusObservable().Subscribe(wss =>
            {
                Logger.LogDebug("WebSocketStatusObservable {VALUE}", wss);
            }));

            _DBServicesDisposeBag.Add(DB.SyncCompleteObservable().Subscribe(c =>
            {
                Logger.LogDebug("SyncCompleteObservable {VALUE}", c);
            }));
            
            /*var colorThemeSettings = await DB.Settings.Get(Settings.ColorThemeKey);
            if (colorThemeSettings is null)
            {
                var settings = Settings.CreateColorTheme(LightMode.Value);
                await DB.Settings.Add(settings);
            }*/
            
            var settingsQuery = DB.LiveQuery(async () =>
            {
                var settings = await DB.Settings.Where(s => s.ID, Settings.ColorThemeKey).ToArray();
                return settings.FirstOrDefault();
            });

            _DBServicesDisposeBag.Add(settingsQuery.Subscribe(s =>
            {
                if (s is not null)
                {
                    var theme = s.GetColorTheme();
                    LightMode.Value = (theme?.Light).True();
                }
                else
                {
                    LightMode.Value = true;
                }
            }));
            
            _DBServicesDisposeBag.Add(DB.UserLoginObservable().Subscribe(ul => { UserLogin.Value = ul; }));

            _DBServicesDisposeBag.Add(DB.RoleObservable().Subscribe(r => { Roles.Value = r; }));

            _DBServicesDisposeBag.Add(DB.InvitesObservable().Subscribe(i => { Invites.Value = i; }));

            _DBServicesDisposeBag.Add(
                DB.NotificationStateObservable().Subscribe(n => { NotificationsState.Value = n; }));

            _DBServicesDisposeBag.Add(
                DB.ServiceWorkerNotificationObservable().Subscribe(swn =>
                {
                    ServiceWorkerNotificationState.Value = swn;
                }));

            _DBServicesDisposeBag.Add(this.AsChangedObservable(State)
                .Where(s => s is DBState.Cloud)
                .Select(async s => await InitDB())
                .Subscribe());
            
#if DEBUG
            Logger.LogDebug("We're using dexie-cloud-addon {VALUE}", DB.AddOnVersion());
            var cloudOptions = DB.Options();
            var schema = DB.Schema();
            var usingServiceWorker = DB.UsingServiceWorker();
#endif
            State.Value = DBState.Cloud;
        }

        public async ValueTask<string?> Login(LoginInformation loginInformation)
        {
            ArgumentNullException.ThrowIfNull(DB);
            return await DB.UserLogin(loginInformation);
        }

        public async ValueTask Sync(SyncOptions syncOptions)
        {
            ArgumentNullException.ThrowIfNull(DB);
            await DB.Sync(syncOptions);
        }

        /*
        private async Task<TokenFinalResponse?> FetchTokens(TokenParams tokenParams)
        {
            var adminstrationService = _serviceProvider.GetRequiredService<AdministrationService>();

            var cloudKeyData = new CloudKeyData
            {
                ClientId = "xx",
                ClientSecret = "xxx"
            };
            var tokenFinalResponse = await adminstrationService.GetUserCredentials(cloudKeyData, tokenParams, CancellationToken.None);
            return tokenFinalResponse;
        }
        */

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_DBServicesDisposeBag.IsDisposed)
                {
                    _DBServicesDisposeBag.Dispose(); 
                }
                _dexieFactory.Dispose();
            }

            base.Dispose(disposing);
        }

        private async Task InitDB()
        {
            ArgumentNullException.ThrowIfNull(DB);
            
            var colorThemeSettings = await DB.Settings.Get(Settings.ColorThemeKey);
            if (colorThemeSettings is null)
            {
                var settings = Settings.CreateColorTheme(LightMode.Value);
                await DB.Settings.Add(settings);
            }
       
            var lq = DB.LiveQuery(async () =>
            {
                return await DB.ToDoDBItems.Where(i => i.Completed, false).ToArray();
            });

            _DBServicesDisposeBag.Add(lq.SubscribeAsyncConcat(async items =>
            {
                await ClearBadgeItems(items);
            }));
        }
        
        private async Task ClearBadgeItems(IEnumerable<ToDoDBItem> items)
        {
            ArgumentNullException.ThrowIfNull(DB);

            var badgeEvents = await DB.GetBadgeEvents();

            if (badgeEvents.Length == 0)
            {
                return;
            }

            var badgeEventKeys = badgeEvents.Select(pushEvent =>
            {
                if (pushEvent.PayloadJson == null)
                {
                    return null;
                }

                var pushPayload = JsonSerializer.Deserialize(pushEvent.PayloadJson,
                    PushPayloadConfigContext.Default.PushPayload);

                return pushPayload == null ? null : Tuple.Create(pushEvent.ID, pushPayload.ItemID);
            }).Where(p => p is not null).Select(p => p!).ToArray();

            var itemsKeys = items.Select(i => i.ID).ToArray();
            List<long> keysToDelete = [];
            keysToDelete.AddRange(badgeEventKeys.Where(be => !itemsKeys.Contains(be.Item2)).Select(be => be.Item1));

            if (keysToDelete.Count == 0)
            {
                return;
            }

            await DB.DeleteBadgeEvents(keysToDelete.ToArray());
        }
    }
}