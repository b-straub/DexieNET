using RxBlazorLightCore;

namespace DexieNETCloudSample.Aministration
{
    public partial class AdministrationService
    {
        private class GetUsersCmd(AdministrationService service) : CommandServiceAsync<AdministrationService, CloudKeyData>(service)
        {
            protected override async Task DoExecute(CloudKeyData parameter, CancellationToken cancellationToken)
            {
                await Service.DoGetUsers(parameter, cancellationToken);
            }

            public override bool CanCancel()
            {
                return true;
            }

            public override bool HasProgress()
            {
                return true;
            }
        }
    }
}
