using RxBlazorLightCore;
using DexieNET;

namespace DexieNETCloudSample.Aministration
{
    public partial class AdministrationService
    {
        public Func<IStateCommandAsync, Task> GetUsers(CloudKeyData value) => async c =>
        {
            await DoGetUsers(value, c.CancellationToken);
        };

        public Func<IStateCommandAsync, Task> DeleteUser => async c =>
        {
            await DoDeleteUser(c.CancellationToken);
        };

        public bool CanDeleteUser()
        {
            return DBService.UserLogin.HasValue();
        }
    }
}
