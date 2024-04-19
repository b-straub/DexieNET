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
        public sealed partial class ToDoItemItemInput(ToDoItemService service, ToDoDBItem? item)
        {
            public IState<string> Text { get; } = service.CreateState(item is null ? string.Empty : item.Text);
            public IState<DateTime> DueDateDate { get; } = service.CreateState(item is null ? DateTime.Now.Date : item.DueDate.Date);
            public IState<TimeSpan> DueDateTime { get; } = service.CreateState(item is null ? DateTime.Now.TimeOfDay : item.DueDate.TimeOfDay);

            public async Task SubmitAsync()
            {
                ArgumentNullException.ThrowIfNull(service.CurrentList.Value);
                var newItem = ToDoDBItem.Create(Text.Value, DueDateDate.Value + DueDateTime.Value, service.CurrentList.Value, item);
                await service.CommandAsync.ExecuteAsync(service.AddItem(newItem));
            }
            public bool CanSubmit()
            {
                var dateItem = NoSeconds(item?.DueDate);
                var dateNew = NoSeconds(DueDateDate.Value.Date + DueDateTime.Value);

                return Text.Value != string.Empty && DueDateDate.Value.Date >= DateTime.Now.Date &&
                    (Text.Value != item?.Text || dateNew != dateItem);
            }

            public Func<string, bool> CanUpdateText => _ =>
            {
                return service.CanUpdate(item, i => i.Text);
            };

            public Func<DateTime, bool> CanUpdateDueDate => _ =>
            {
                return service.CanUpdate(item, i => i.DueDate);
            };

            public static Func<string, StateValidation> ValidateText => v =>
            {
                return new("Text can not be empty!", v.Length == 0);
            };

            public static Func<DateTime, StateValidation> ValidateDueDate => v =>
            {
                return new("DueDate can not be in the past!", v.Date < DateTime.Now.Date);
            };

            public Func<TimeSpan, StateValidation> ValidateDueDateTime => v =>
            {
                var dateNowNS = NoSeconds(DateTime.Now);
                var dateNew = item is null ? NoSeconds(DueDateDate.Value.Date + v) : dateNowNS;

                return new("DueDate can not be in the past!", dateNew < dateNowNS);
            };

            public Func<TimeSpan, bool> CanUpdateTime => _ =>
            {
                return service.CanUpdate(item, i => i.DueDate);
            };

            private static DateTime? NoSeconds(DateTime? dateTime)
            {
                if (dateTime.HasValue)
                {
                    return new(dateTime.Value.Year, dateTime.Value.Month, dateTime.Value.Day, dateTime.Value.Hour,
                        dateTime.Value.Minute, 0, dateTime.Value.Kind);
                }

                return null;
            }
        }

        public IEnumerable<ToDoDBItem> ToDoItems => Items ?? [];
        public IState<ToDoDBList?> CurrentList { get; }

        private ToDoDB? _db;

        public ToDoItemService(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            CurrentList = this.CreateState((ToDoDBList?)null);
        }

        public ToDoItemItemInput CreateItemInput(ToDoDBItem? item = null)
        {
            return new ToDoItemItemInput(this, item);
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

        public override bool CanAdd()
        {
            return (Permissions?.CanAdd(CurrentList.Value)).True();
        }

        public override bool CanUpdate(ToDoDBItem? item)
        {
            return CanUpdate(item, i => i.Text) || CanUpdate(item, i => i.DueDate);
        }
    }
}
