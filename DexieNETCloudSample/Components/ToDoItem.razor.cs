using DexieNETCloudSample.Dexie.Services;
using DexieNETCloudSample.Dialogs;
using DexieNETCloudSample.Logic;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace DexieNETCloudSample.Components
{
    public partial class ToDoItem
    {
        [CascadingParameter]
        public required ToDoItemService Service { get; init; }

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

        private SortDirection _sortDirection = SortDirection.Ascending;

        protected override Task OnParametersSetAsync()
        {
            ArgumentNullException.ThrowIfNull(List);

            if (Service.CurrentList.Value != List)
            {
                Service.CurrentList.Value = List;
            }

            return base.OnParametersSetAsync();
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
            var dialog = DialogService.Show<ConfirmDialog>("ToDoItem", parameters);

            var res = await dialog.Result;

            if (res.Canceled)
            {
                return false;
            }

            return true;
        }

        private static string ColorForItem(ToDoDBItem item)
        {
            return item.Completed ? $"color:{Colors.Grey.Default}" : "color:black";
        }

        private static string DateTimeForItem(ToDoDBItem item)
        {
            return item.DueDate == new DateTime(item.DueDate.Year, item.DueDate.Month, item.DueDate.Day) ?
                item.DueDate.ToShortDateString() : item.DueDate.ToShortDateString() + " - " + item.DueDate.ToShortTimeString();
        }

        private Func<ToDoDBItem, object> Sort()
        {
            return new Func<ToDoDBItem, object>(x =>
            {
                var completed = x.Completed;

                if (_sortDirection is SortDirection.Descending)
                {
                    completed = !completed;
                }

                return x.Completed ? x.DueDate.Ticks : -(long.MaxValue - x.DueDate.Ticks);
            });
        }
    }
}