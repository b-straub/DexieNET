using DexieNET;
using DexieNETCloudSample.Extensions;
using DexieNETCloudSample.Logic;
using RxBlazorLightCore;
using System.Text.Json;
using DexieCloudNET;

namespace DexieNETCloudSample.Dexie.Services
{
    public sealed partial class ToDoItemService : CrudService<ToDoDBItem>
    {
        public sealed class ToDoItemItemInput(ToDoItemService service, ToDoDBItem? item)
        {
            public IState<string> Text { get; } = service.CreateState(item is null ? string.Empty : item.Text);

            public IState<DateTime> DueDateDate { get; } =
                service.CreateState(item is null ? DateTime.Now.Date : item.DueDate.Date);

            public IState<TimeSpan> DueDateTime { get; } =
                service.CreateState(item is null ? DateTime.Now.TimeOfDay : item.DueDate.TimeOfDay);

            public async Task SubmitAsync()
            {
                ArgumentNullException.ThrowIfNull(service.CurrentList.Value);
                var newItem = ToDoDBItem.Create(Text.Value, DueDateDate.Value + DueDateTime.Value,
                    service.CurrentList.Value, item);
                if (item is null)
                {
                    await service.CommandAsync.ExecuteAsync(service.AddItem(newItem));
                }
                else
                {
                    await service.CommandAsync.ExecuteAsync(service.UpdateItem(newItem));
                }
            }

            public bool CanSubmit()
            {
                var dateItem = NoSeconds(item?.DueDate);
                var dateNew = NoSeconds(DueDateDate.Value.Date + DueDateTime.Value);
                var dateNow = NoSeconds(DateTime.Now);

                return Text.Value != string.Empty && dateNew >= dateNow &&
                       (Text.Value != item?.Text || dateNew != dateItem);
            }

            public Func<bool> CanUpdateText => () =>
            {
                return service.CanUpdate(service.CurrentList.Value, i => i.Text);
            };

            public Func<bool> CanUpdateDueDate => () =>
            {
                return service.CanUpdate(service.CurrentList.Value, i => i.DueDate);
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
                var dateNew = DueDateDate.Value.Date + v;
                var dateNow = NoSeconds(DateTime.Now);
                return new("DueDate can not be in the past!", dateNew < dateNow);
            };

            public Func<bool> CanUpdateTime => () =>
            {
                return service.CanUpdate(service.CurrentList.Value, i => i.DueDate);
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

        public IEnumerable<ToDoDBItem> ToDoItems => ItemsState.Value;
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

        protected override Task<LiveQuery<IEnumerable<ToDoDBItem>>> InitializeDB(ToDoDB db)
        {
            ArgumentNullException.ThrowIfNull(CurrentList.Value?.ID);
            _db = db;
            
            return Task.FromResult(db.LiveQuery(
                async () => await GetTable()
                    .Where(i => i.ListID, CurrentList.Value.ID)
                    .ToArray())
            );
        }

        public override bool CanAdd()
        {
            return (Permissions?.CanAdd(CurrentList.Value)).True();
        }

        protected override bool CanUpdate(ToDoDBItem item)
        {
            return !item.Completed && (CanUpdate(item, i => i.Text) || CanUpdate(item, i => i.DueDate));
        }
        
        protected override async Task PostAddAction(string id)
        {
            ArgumentNullException.ThrowIfNull(_db);
            
            if (_db.PushNotifications.TransactionCollecting)
            {
                await _db.PushNotifications.Put(null);
                return;
            }

            await AddPushNotification(id, PnReason.ADD);
        }
        
        protected override async Task PostUpdateAction(string id)
        {
            ArgumentNullException.ThrowIfNull(_db);

            if (_db.PushNotifications.TransactionCollecting)
            {
                await _db.PushNotifications.Put(null);
                return;
            }

            await AddPushNotification(id, PnReason.REMINDER);
        }
        
        protected override async Task PreDeleteAction(string id)
        {
            ArgumentNullException.ThrowIfNull(_db);
            await _db.PushNotifications.ExpireNotification(id);
        }

        private enum PnReason
        {
            ADD,
            REMINDER,
            COMPLETED
        }
        
        private async Task AddPushNotification(string id, PnReason reason)
        {
            ArgumentNullException.ThrowIfNull(_db);

            if (DbService.NotificationsState.Value is not NotificationState.Subscribed)
            {
                return;
            }

            var item = await GetTable().Get(id);
            var notCompletedCount = (long)await GetTable().Where(i => i.Completed, false).Count();
            
            ArgumentNullException.ThrowIfNull(item);
            ArgumentNullException.ThrowIfNull(item.ID);
            ArgumentNullException.ThrowIfNull(item.RealmId);

            var pushPayload = new PushPayloadToDo(item.ListID, item.ID);
            var pushPayloadJson = JsonSerializer.Serialize(pushPayload,
                PushPayloadToDoConfigContext.Default.PushPayloadToDo);

            var pushPayloadEnvelope = new PushPayloadEnvelope(PushPayloadType.TODO, pushPayloadJson);
            var pushPayloadEnvelopeJson = JsonSerializer.Serialize(pushPayloadEnvelope,
                PushPayloadEnvelopeConfigContext.Default.PushPayloadEnvelope);
            
            var pushPayloadBase64 = pushPayloadEnvelopeJson.ToBase64();
            
            var firstReminderDateTime = item.DueDate - TimeSpan.FromMinutes(5);

            if (firstReminderDateTime <= DateTime.Now)
            {
                firstReminderDateTime = DateTime.Now + TimeSpan.FromMinutes(1);
            }

            var messageReminder =
                $"Reminder for {item.Text} at {item.DueDate:G}";

            var reminderTrigger = new PushTrigger(messageReminder, pushPayloadBase64, PushConstants.PushIconToDo, firstReminderDateTime.ToUniversalTime(),
                false, 2, 2);

            long? appBadge = notCompletedCount > 0 ? notCompletedCount : null;
            
            switch (reason)
            {
                case PnReason.ADD:
                {
                    var messageAdd =
                        $"Added {item.Text} at {DateTime.Now:G}";
                    var addTrigger = new PushTrigger(messageAdd, pushPayloadBase64, PushConstants.PushIconToDo);
                    var pushNotification =
                        new PushNotification(item.ID, "ToDo", item.RealmId, [addTrigger, reminderTrigger], pushPayload.Tag, appBadge);
                    await _db.PushNotifications.Put(pushNotification);
                }
                    break;
                case PnReason.REMINDER:
                {
                    var pushNotification =
                        new PushNotification(item.ID, "ToDo", item.RealmId, [reminderTrigger], pushPayload.Tag, appBadge);
                    await _db.PushNotifications.Put(pushNotification);
                }
                    break;
                case PnReason.COMPLETED:
                {
                    var messageCompleted =
                        $"{item.Text} completed!";
                    var completedTrigger = new PushTrigger(messageCompleted, pushPayloadBase64, PushConstants.PushIconToDo);
                    var pushNotification =
                        new PushNotification(item.ID, "ToDo", item.RealmId, [completedTrigger], pushPayload.Tag, appBadge);
                    await _db.PushNotifications.Put(pushNotification);
                }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(reason), reason, null);
            }
        }
    }
}