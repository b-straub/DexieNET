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
        public IEnumerable<ToDoDBItem> ToDoItems => Items;

        public ToDoDBList? CurrentList { get; private set; }

        // Commands
        public ICommandAsync<ToDoDBItem> ToggledItemCompleted => new ToggledItemCompletedCmd(this);
        public ICommandAsync DeleteCompletedItems;

        public ICommand<ToDoDBList> SetCurrentList;

        private ToDoDB? _db;

        public ToDoItemService(DexieCloudService databaseService) : base(databaseService)
        {
            SetCurrentList = new SetListCmd(this);
            DeleteCompletedItems = new DeleteCompletedItemsCmd(this);
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
            ArgumentNullException.ThrowIfNull(CurrentList?.ID);
            _db = db;
            return db.LiveQuery(async () => await GetTable().Where(i => i.ListID).Equal(CurrentList.ID).ToArray());
        }

        protected override bool CanAdd()
        {
            return (Permissions?.CanAdd(CurrentList)).True();
        }

        protected override bool CanUpdate(ToDoDBItem? item)
        {
            return CanUpdate(item, i => i.Text) || CanUpdate(item, i => i.DueDate);
        }
    }
}
