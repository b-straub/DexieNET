// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DexieNETCloudPushServer.QuartzDBEntities;

public partial class QrtzSchedulerState
{
    public string SchedName { get; init; } = null!;

    public string InstanceName { get; init; } = null!;

    public long LastCheckinTime { get; init; }

    public long CheckinInterval { get; init; }
}
