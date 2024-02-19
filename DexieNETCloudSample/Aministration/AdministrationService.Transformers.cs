using RxBlazorLightCore;

namespace DexieNETCloudSample.Aministration
{
    public partial class AdministrationService
    {
        private class GetUsersST(AdministrationService service, IState<IEnumerable<UserResponse>, List<UserResponse>> state) : 
            StateRefTransformerAsync<AdministrationService, CloudKeyData, IEnumerable<UserResponse>, List<UserResponse>>(service, state)
        {
            protected override async Task TransformStateAsync(CloudKeyData value, List<UserResponse> stateRef, CancellationToken cancellationToken)
            {
                await Service.DoGetUsers(value, stateRef, cancellationToken);
            }

            public override bool CanCancel => true;
            public override bool LongRunning => true;
        }

        private class DeleteUserSSP(AdministrationService service) : ServiceStateProviderAsync<AdministrationService>(service)
        {
            protected override async Task ProvideStateAsync(CancellationToken cancellationToken)
            {
                await Service.DoDeleteUser(cancellationToken);
            }

            public override bool CanRun => Service.DBService.UserLogin.HasValue();
            public override bool CanCancel => true;
            public override bool LongRunning => true;
        }
    }
}
