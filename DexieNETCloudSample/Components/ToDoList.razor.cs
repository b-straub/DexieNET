using DexieNETCloudSample.Dexie.Services;
using DexieNETCloudSample.Dialogs;
using DexieNETCloudSample.Logic;
using DexieCloudNET;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using RxBlazorLightCore;
using RxMudBlazorLight.Extensions;
using System.Text;
using System.Text.Json;

namespace DexieNETCloudSample.Components
{
    public partial class ToDoList
    {
        [Inject]
        public required IDialogService DialogService { get; init; }

        [Parameter]
        [SupplyParameterFromQuery(Name = $"{PushConstants.PushPayloadJsonBase64}")]
        public string? PushPayloadJsonBase64 { get; set; }

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

        protected override void OnParametersSet()
        {
            if (PushPayloadJsonBase64 is not null)
            {
                var pushPayloadJson = Encoding.UTF8.GetString(Convert.FromBase64String(PushPayloadJsonBase64));
                var pushPayload = JsonSerializer.Deserialize(pushPayloadJson,
                    PushPayloadConfigContext.Default.PushPayload);
                
                ArgumentNullException.ThrowIfNull(pushPayload);
                Service.SetPushPayloadEvent(pushPayload);
            }
        }
    }
}