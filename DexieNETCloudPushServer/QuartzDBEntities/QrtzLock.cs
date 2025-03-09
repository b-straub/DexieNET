// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DexieNETCloudPushServer.QuartzDBEntities;

public partial class QrtzLock
{
    public string SchedName { get; init; } = null!;

    public string LockName { get; init; } = null!;
}
