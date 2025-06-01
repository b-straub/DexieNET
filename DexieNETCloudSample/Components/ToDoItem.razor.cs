using DexieNETCloudSample.Dialogs;
using DexieNETCloudSample.Logic;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using RxBlazorLightCore;
using RxMudBlazorLight.Extensions;

namespace DexieNETCloudSample.Components
{
    public partial class ToDoItem
    {
        [Inject]
        public required IDialogService DialogService { get; init; }

        [Inject]
        public required  ISnackbar Snackbar { get; init; }
        
        [Parameter, EditorRequired]
        public required ToDoDBList List { get; init; }

        private enum DeleteType
        {
            All,
            Completed,
            One
        }

        protected override void OnInitialized()
        {
            ArgumentNullException.ThrowIfNull(List);
            Service.CurrentList.Value = List;
            base.OnInitialized();
        }

        private async Task<bool> ConfirmDelete(DeleteType type)
        {
            var message = type switch
            {
                DeleteType.All => "Delete all items?",
                DeleteType.Completed => "Delete completed items?",
                DeleteType.One => "Delete item?",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            var parameters = new DialogParameters { ["Message"] = message };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>("ToDoItem", parameters);

            var res = await dialog.Result;

            return res.OK();
        }

        private static string ColorForItem(ToDoDBItem item)
        {
            return item.Completed ? $"color:{Colors.Gray.Default}" : "color:black";
        }

        private static string DateTimeForItem(ToDoDBItem item)
        {
            return item.DueDate == new DateTime(item.DueDate.Year, item.DueDate.Month, item.DueDate.Day)
                ? item.DueDate.ToShortDateString()
                : item.DueDate.ToShortDateString() + " - " + item.DueDate.ToShortTimeString();
        }

        private async Task HandleSharePayload()
        {
            ArgumentNullException.ThrowIfNull(Service.SharePayload?.Title);
            
            var itemExist = Service.ToDoItems.Any(i => i.Text == Service.SharePayload.Title && i.ListID == List.ID);
           
            if (!itemExist && Service.CanAddItem())
            {
                Snackbar.Add($"Push Notification, Title {Service.SharePayload.Title} added", Severity.Info,
                    config => { config.RequireInteraction = false; });
                
                var dueDate = DateTime.Now.AddDays(1);
                var item = ToDoDBItem.Create(Service.SharePayload.Title, dueDate, List, null);
                await Service.DoAddItem(item);
            }
            else
            {
                Snackbar.Add($"Push Notification, Title {Service.SharePayload.Title} skipped", Severity.Info,
                    config => { config.RequireInteraction = false; });
            }

            Service.SetSharePayload(null);
        }

        protected override async Task OnServiceStateHasChangedAsync(IList<ServiceChangeReason> crList, CancellationToken ct)
        {
            if (crList.Any(cr => cr.StateID == Service.ItemsState.StateID))
            {
                if (Service.SharePayload?.Title is not null)
                {
                    await HandleSharePayload();
                }
            }
        }
    }
}