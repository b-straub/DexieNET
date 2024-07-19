using Microsoft.AspNetCore.Components;
using MudBlazor;
using DexieNETCloudSample.Dialogs;
using RxBlazorLightCore;
using RxMudBlazorLight.Extensions;

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
            if (result.OK())
            {
                stateCommandAsync.NotifyChanging();
                await Task.Delay(2000, stateCommandAsync.CancellationToken);
                var cloudKeyData = (CloudKeyData?)result.Data;
                if (cloudKeyData is not null)
                {
                    await stateCommandAsync.ExecuteAsync(Service1.GetUsers(data));
                }
            }
        };

        private string GetExceptions()
        {
            return Service1.Exceptions.Aggregate("", (p, n) => p + n.Exception.Message + ", ").TrimEnd([' ', ',']);
        }
        
        private async Task ExpirePushSubscriptions()
        {
            var parameters = new DialogParameters
                { ["Message"] = $"Expire PushSubscriptions?", ["ConfirmButton"] = "Expire", ["SuccessOnConfirm"] = false };
            var dialog = await DialogService.ShowAsync<ConfirmDialog>("PushSubscriptions", parameters);

            var res = await dialog.Result;

            if (!res.OK())
            {
                return;
            }

            await Service1.ExpireAllPushSubscriptions();
        }
    }
}
