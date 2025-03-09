// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DexieNETCloudPushServer.QuartzDBEntities;

public partial class QrtzFiredTrigger
{
    public string SchedName { get; init; } = null!;

    public string EntryId { get; init; } = null!;

    public string TriggerName { get; init; } = null!;

    public string TriggerGroup { get; init; } = null!;

    public string InstanceName { get; init; } = null!;

    public long FiredTime { get; init; }

    public long SchedTime { get; init; }

    public int Priority { get; init; }

    public string State { get; init; } = null!;

    public string? JobName { get; init; }

    public string? JobGroup { get; init; }

    public bool IsNonconcurrent { get; init; }

    public bool? RequestsRecovery { get; init; }
}
