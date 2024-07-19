using DexieCloudNET;
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

        public bool IsLoggedIn()
        {
            return DBService.UserLogin.Value?.AccessToken is not null;
        }
        
        public async Task ExpireAllPushSubscriptions()
        {
            ArgumentNullException.ThrowIfNull(DBService.DB);
            await DBService.DB.ExpireAllPushSubscriptions();
        }
    }
}
