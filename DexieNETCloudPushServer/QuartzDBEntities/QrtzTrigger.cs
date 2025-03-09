// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DexieNETCloudPushServer.QuartzDBEntities;

public partial class QrtzTrigger
{
    public string SchedName { get; init; } = null!;

    public string TriggerName { get; init; } = null!;

    public string TriggerGroup { get; init; } = null!;

    public string JobName { get; init; } = null!;

    public string JobGroup { get; init; } = null!;

    public string? Description { get; init; }

    public long? NextFireTime { get; init; }

    public long? PrevFireTime { get; init; }

    public int? Priority { get; init; }

    public string TriggerState { get; init; } = null!;

    public string TriggerType { get; init; } = null!;

    public long StartTime { get; init; }

    public long? EndTime { get; init; }

    public string? CalendarName { get; init; }

    public short? MisfireInstr { get; init; }

    public byte[]? JobData { get; init; }

    public virtual QrtzBlobTrigger? QrtzBlobTrigger { get; init; }

    public virtual QrtzCronTrigger? QrtzCronTrigger { get; init; }

    public virtual QrtzJobDetail QrtzJobDetail { get; init; } = null!;

    public virtual QrtzSimpleTrigger? QrtzSimpleTrigger { get; init; }

    public virtual QrtzSimpropTrigger? QrtzSimpropTrigger { get; init; }
}
