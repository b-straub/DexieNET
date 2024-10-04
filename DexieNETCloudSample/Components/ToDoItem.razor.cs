using DexieNETCloudSample.Dialogs;
using DexieNETCloudSample.Logic;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using RxMudBlazorLight.Extensions;

namespace DexieNETCloudSample.Components
{
    public partial class ToDoItem
    {
        [Inject]
        public required IDialogService DialogService { get; init; }

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
            return item.DueDate == new DateTime(item.DueDate.Year, item.DueDate.Month, item.DueDate.Day) ?
                item.DueDate.ToShortDateString() : item.DueDate.ToShortDateString() + " - " + item.DueDate.ToShortTimeString();
        }
    }
}