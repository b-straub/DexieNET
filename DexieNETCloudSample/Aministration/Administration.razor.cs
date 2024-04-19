using Microsoft.AspNetCore.Components;
using MudBlazor;
using DexieNETCloudSample.Dialogs;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Aministration
{
    public partial class Administration
    {
        [Inject]
        public required IDialogService DialogService { get; init; }

        private Func<IStateCommandAsync, Task> GetUsers => async stateCommandAsync =>
        {
            CloudKeyData data = new("clientId", "clientSecret");

            var parameters = new DialogParameters { ["Item"] = data };
            var dialog = DialogService.Show<GetClientKeys>("Cloud Client Keys", parameters);

            var result = await dialog.Result;
            if (!result.Canceled)
            {
                await stateCommandAsync.ExecuteAsync(Service.GetUsers((CloudKeyData)result.Data));
            }
        };

        private string GetExceptions()
        {
            return Service.Exceptions.Aggregate("", (p, n) => p + n.Exception.Message + ", ").TrimEnd([' ', ',']);
        }
    }
}
