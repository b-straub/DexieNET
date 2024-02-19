using DexieNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Buffers;
using System.Reactive.Linq;

namespace DexieNETCloudSample.Dexie.Services
{
    public sealed partial class ToDoItemService : CrudService<ToDoDBItem>
    {
        public partial class Scope(ToDoItemService service) : CrudServiceScope(service)
        {
            public IServiceStateTransformer<ToDoDBItem> ToggledItemCompleted { get; } = new ToggledItemCompletedSST(service);
            public IServiceStateProvider DeleteCompletedItems { get; } = new DeleteCompletedItemsSSP(service);
        }

        public IEnumerable<ToDoDBItem> ToDoItems => Items.Value ?? Enumerable.Empty<ToDoDBItem>();
        public IState<ToDoDBList?> CurrentList { get; }

        private ToDoDB? _db;

        public ToDoItemService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            CurrentList = this.CreateState((ToDoDBList?)null);
        }

        public override IRxBLScope CreateScope()
        {
            return new Scope(this);
        }

        public static ToDoDBItem CreateItem(string text, DateTime dueDate, ToDoDBList list, ToDoDBItem? item = null)
        {
            return ToDoDBItem.Create(text, dueDate, list, item);
        }

        protected override Table<ToDoDBItem, string> GetTable()
        {
            ArgumentNullException.ThrowIfNull(_db);
            return _db.ToDoDBItems;
        }

        protected override LiveQuery<IEnumerable<ToDoDBItem>> InitializeDB(ToDoDB db)
        {
            ArgumentNullException.ThrowIfNull(CurrentList.Value?.ID);
            _db = db;
            return db.LiveQuery(async () => await GetTable().Where(i => i.ListID).Equal(CurrentList.Value.ID).ToArray());
        }

        protected override bool CanAdd()
        {
            return (Permissions?.CanAdd(CurrentList.Value)).True();
        }

        protected override bool CanUpdate(ToDoDBItem item)
        {
            return CanUpdate(item, i => i.Text) || CanUpdate(item, i => i.DueDate);
        }
    }
}
