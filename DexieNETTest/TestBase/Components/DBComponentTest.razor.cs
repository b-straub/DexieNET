using DexieNET;
using DexieNETTest.TestBase.Test;
using System.ComponentModel.DataAnnotations;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace DexieNETTest.TestBase.Components
{
    public partial record struct Friend
    (
        [property: Index] string Name,
        [property: Index] int Age
    ) : IDBStore;

    public partial class DBComponentTest
    {
        private IEnumerable<Friend> _friends = Enumerable.Empty<Friend>();
        private IEnumerable<Friend> _friendsSecond = Enumerable.Empty<Friend>();
        private IEnumerable<Friend> _searchedFriends = Enumerable.Empty<Friend>();

        [Required]
        [StringLength(30, ErrorMessage = "Name is too long.")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [Range(0, 100, ErrorMessage = "Invalid Age.")]
        public int Age { get; set; } = 0;

        public bool CreateByTransaction { get; set; } = false;

        private readonly CompositeDisposable _disposeBag = new();
        private string _queryName = string.Empty;
        private readonly Subject<Unit> _queryChanged = new();
        private LiveQuery<IEnumerable<Friend>>? _friendsQuery = null;
        private UseLiveQuery<IEnumerable<Friend>>? _searchFriendsQuery = null;
        private bool _hasData = false;
        private IDisposable? _hasDataDisposable = null;

        public DBComponentTest() : base() { }

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();

            if (Dexie is not null)
            {
                Dexie.Version(2).Stores();
                if (await Dexie.Friends.Count() == 0)
                {
                    await Dexie.Friends.BulkAdd(DataGenerator.GetFriends());
                }

                _friendsQuery = Dexie.LiveQuery(async () =>
                {
                    var f = await Dexie.Friends.ToArray();
                    return f;
                });

                var lq = Dexie.LiveQuery(async () =>
                {
                    var sf = await Dexie.Friends.Where(f => f.Name).StartsWithIgnoreCase(_queryName.ToLowerInvariant()).ToArray();
                    return sf;
                });

                _searchFriendsQuery = lq.UseLiveQuery(_queryChanged);

                var hasDataQuery = Dexie.LiveQuery(async () =>
                {
                    return await Dexie.Friends.Count();
                });

                _hasDataDisposable = hasDataQuery.Subscribe(c =>
                {
                    _hasData = c > 0;
                    InvokeAsync(StateHasChanged);
                });

                Subscribe();
            }
        }

        private async Task HandleValidSubmit()
        {
            var friend = new Friend(Name, Age);
            await Dexie.Friends.Add(friend);
        }

        private void QueryChanged()
        {
            _queryChanged.OnNext(Unit.Default);
        }

        private async Task ClearDatabase()
        {
            await Dexie.Friends.Clear();
        }

        private void Subscribe()
        {
            var disposable = _friendsQuery?.Select(async values =>
            {
                if (CreateByTransaction)
                {
                    await Dexie.Transaction(async ta =>
                    {
                        await Dexie.Friends.Add(new Components.Friend("TA1", 55));

                        await Dexie.Transaction(async _ =>
                        {
                            await Dexie.Friends.Add(new Components.Friend("TA2", 57));
                        }, TAType.TopLevel);
                    });

                    CreateByTransaction = false; // caution reset to prevent endless recursion
                }

                _friends = values;
                await InvokeAsync(StateHasChanged);
            }).Subscribe();

            if (disposable is not null)
            {
                _disposeBag.Add(disposable);
            }

            disposable = _searchFriendsQuery?.Subscribe(values =>
            {
                _searchedFriends = values;
                InvokeAsync(StateHasChanged);
            });

            if (disposable is not null)
            {
                _disposeBag.Add(disposable);
            }
        }

        private void SubscribeSecond()
        {
            var disposable = _friendsQuery?.Subscribe(values =>
            {
                _friendsSecond = values;
                InvokeAsync(StateHasChanged);
            });

            if (disposable is not null)
            {
                _disposeBag.Add(disposable);
            }
        }

        private void Unsubscribe()
        {
            foreach (var disposable in _disposeBag)
            {
                disposable.Dispose();
            }
            _disposeBag.Clear();
            _friends = Enumerable.Empty<Friend>();
            _friendsSecond = Enumerable.Empty<Friend>();
            _searchedFriends = Enumerable.Empty<Friend>();
            _queryName = string.Empty;
            InvokeAsync(StateHasChanged);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!_disposeBag.IsDisposed)
                {
                    _disposeBag.Dispose();
                }

                _hasDataDisposable?.Dispose();
                _hasDataDisposable = null;
            }

            base.Dispose(disposing);
        }
    }
}
