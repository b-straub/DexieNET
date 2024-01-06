using RxBlazorLightCore;

namespace DexieNETCloudSample.Aministration
{
    public partial class AdministrationService
    {
        private class GetUsersCmd(AdministrationService service) : CommandLongRunningServiceAsync<AdministrationService, CloudKeyData>(service)
        {
            protected override async Task DoExecute(CloudKeyData parameter, CancellationToken cancellationToken)
            {
                await Service.DoGetUsers(parameter, cancellationToken);
            }
        }

        private class DeleteUserCmd(AdministrationService service) : CommandLongRunningServiceAsync<AdministrationService>(service)
        {
            protected override async Task DoExecute(CancellationToken cancellationToken)
            {
                await Service.DoDeleteUser(cancellationToken);
            }

            public override bool CanExecute()
            {
                return Service.DBService.UserLogin is not null;
            }
        }
    }
}
