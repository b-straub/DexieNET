using DexieNETCloudSample.Dexie.Services;
using DexieNETCloudSample.Dialogs;
using DexieNETCloudSample.Logic;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Components
{
    public partial class ToDoList
    {
        [Inject]
        public required IDialogService DialogService { get; init; }

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
            var dialog = DialogService.Show<AddToDoList>(item is null ? "Add ToDoList" : "Edit ToDoList", parameters);

            var result = await dialog.Result;

            if (!result.Canceled)
            {
                data = (ToDoListData)result.Data;
                ToDoDBList list = item is null ?
                    ToDoListService.CreateList(data.Title) :
                    ToDoListService.CreateList(data.Title, item);

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
            var dialog = DialogService.Show<ConfirmDialog>("ToDoList", parameters);

            var res = await dialog.Result;

            if (res.Canceled)
            {
                return false;
            }

            return true;
        }
    }
}