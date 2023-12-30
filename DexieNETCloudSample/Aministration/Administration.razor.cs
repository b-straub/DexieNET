using Microsoft.AspNetCore.Components;
using MudBlazor;
using DexieNETCloudSample.Dialogs;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Aministration
{
    public partial class Administration
    {
        [Inject]
        private IDialogService? DialogService { get; set; }


        private readonly string[] _headers = ["UserId", "LastLogin", "LicenseType", "EvalDaysLeft"];

        private async Task<bool> GetCloudKeyData(ICommandAsync<CloudKeyData> cmd, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(DialogService);

            CloudKeyData data = new("clientId", "clientSecret");

            var parameters = new DialogParameters { ["Item"] = data };
            var dialog = DialogService.Show<GetClientKeys>("Cloud Client Keys", parameters);

            var result = await dialog.Result;
            if (!result.Canceled)
            {
                cmd.SetParameter((CloudKeyData)result.Data);
                return true;
            }

            return false;
        }

        private string GetExceptions()
        {
            return Service.CommandExceptions.Aggregate("", (p, n) => p + n.Message + ", ").TrimEnd([' ', ',']);
        }
    }
}
