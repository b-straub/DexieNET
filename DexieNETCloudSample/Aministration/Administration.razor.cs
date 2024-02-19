using Microsoft.AspNetCore.Components;
using MudBlazor;
using DexieNETCloudSample.Dialogs;
using RxBlazorLightCore;

namespace DexieNETCloudSample.Aministration
{
    public partial class Administration
    {
        [CascadingParameter]
        public required AdministrationService Service { get; init; }

        [Inject]
        public required IDialogService DialogService { get; init; }

        private async Task GetCloudKeyData(IStateTransformer<CloudKeyData> st)
        {
            CloudKeyData data = new("clientId", "clientSecret");

            var parameters = new DialogParameters { ["Item"] = data };
            var dialog = DialogService.Show<GetClientKeys>("Cloud Client Keys", parameters);

            var result = await dialog.Result;
            if (!result.Canceled)
            {
                st.Transform((CloudKeyData)result.Data);
            }
        }

        private string GetExceptions()
        {
            return Service.Exceptions.Aggregate("", (p, n) => p + n.Exception.Message + ", ").TrimEnd([' ', ',']);
        }
    }
}
