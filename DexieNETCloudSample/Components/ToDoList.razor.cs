using DexieNETCloudSample.Dexie.Services;
using DexieNETCloudSample.Dialogs;
using DexieNETCloudSample.Logic;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using RxBlazorLightCore;
using RxMudBlazorLight.Extensions;

namespace DexieNETCloudSample.Components
{
    public partial class ToDoList
    {
        [Inject]
        public required IDialogService DialogService { get; init; }
        
        [Inject]
        public required  ISnackbar Snackbar { get; init; }
        
        private enum DeleteType
        {
            All,
            One
        }

        private Func<IStateCommandAsync, Task> AddOrUpdate(ToDoDBList? item) => async commandAsync =>
        {
            ToDoListData data = item is null ? new ToDoListData(string.Empty) : new ToDoListData(item.Title);

            bool canUpdateTitle = item is null || Service.CanUpdate(item, i => i.Title);

            var parameters = new DialogParameters { ["List"] = data, ["CanUpdateTitle"] = canUpdateTitle };
            var dialog =
                await DialogService.ShowAsync<AddToDoList>(item is null ? "Add ToDoList" : "Edit ToDoList", parameters);

            var result = await dialog.Result;

            if (result.TryGet<ToDoListData>(out var newData))
            {
                var list = item is null
                    ? ToDoListService.CreateList(newData.Title)
                    : ToDoListService.CreateList(newData.Title, item);

                if (item is null)
                {
                    await commandAsync.ExecuteAsync(Service.AddItem(list));
                }
                else
                {
                    await commandAsync.ExecuteAsync(Service.UpdateItem(list));
                }
            }
        };

        private async Task<bool> ConfirmDelete(DeleteType type)
        {
            var message = type switch
            {
                DeleteType.All => "Delete all lists?",
                DeleteType.One => "Delete list?",
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

            var parameters = new DialogParameters { ["Message"] = message };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>("ToDoList", parameters);

            var res = await dialog.Result;

            return res.OK();
        }

        private string GetListOpenCloseIcon(ToDoDBList list)
        {
            return Service.IsListItemsOpen(list)
                ? Icons.Material.Filled.KeyboardArrowDown
                : Icons.Material.Filled.KeyboardArrowRight;
        }

        private string GetShareOpenCloseIcon(ToDoDBList list)
        {
            return Service.IsListShareOpen(list) ? Icons.Material.TwoTone.Share : Icons.Material.Filled.Share;
        }
        
        private async Task HandlePushPayload()
        {
            ArgumentNullException.ThrowIfNull(Service.PushPayload?.ListID);
        
            Snackbar.Add($"Push Notification, ListID {Service.PushPayload.ListID}, ItemID {Service.PushPayload.ItemID}", Severity.Info, config => { config.RequireInteraction = false; });
            await Service.OpenList(Service.PushPayload.ListID);
            Service.SetPushPayload(null);
        }
        
        private async Task HandleSharePayload()
        {
            ArgumentNullException.ThrowIfNull(Service.SharePayload);

            if (Service.SharePayload.List is not null)
            {
                Snackbar.Add($"Push Notification, List {Service.SharePayload.List} opened", Severity.Info,
                    config => { config.RequireInteraction = false; });

                var lists = Service.ToDoLists.ToArray();
                var list = lists.FirstOrDefault(l => l.Title == Service.SharePayload.List, lists.First());

                if (list.ID is not null)
                {
                    await Service.OpenList(list.ID);
                }

                Service.SetSharePayload(Service.SharePayload with { List = null });
            }
            else
            {
                var list = Service.ToDoLists.FirstOrDefault(l => l.Title == PushConstants.ShareListTitle);

                if (Service.CanAdd() && list is null)
                {
                    Snackbar.Add($"Push Notification, List {PushConstants.ShareListTitle} added", Severity.Info,
                        config => { config.RequireInteraction = false; });

                    var newList = ToDoListService.CreateList(PushConstants.ShareListTitle);
                    await Service.DoAddItem(newList);
                    
                    Service.SetSharePayload(Service.SharePayload with { List = PushConstants.ShareListTitle });
                }
                else
                {
                    Snackbar.Add($"Push Notification, List {PushConstants.ShareListTitle} skipped", Severity.Info,
                        config => { config.RequireInteraction = false; });

                    if (list?.ID is not null)
                    {
                        await Service.OpenList(list.ID);
                    }
                }
            }
        }

        protected override async Task OnServiceStateHasChangedAsync(IList<ServiceChangeReason> crList)
        {
            if (crList.Any(cr => cr.ID == Service.ItemsState.ID))
            {
                if (Service.PushPayload?.ListID is not null)
                {
                    await HandlePushPayload();
                }
                
                if (Service.SharePayload is not null)
                {
                    await HandleSharePayload();
                }
            }
        }
    }
}