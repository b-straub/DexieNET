// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DexieNETCloudPushServer.QuartzDBEntities;

public partial class QrtzCronTrigger
{
    public string SchedName { get; init; } = null!;

    public string TriggerName { get; init; } = null!;

    public string TriggerGroup { get; init; } = null!;

    public string CronExpression { get; init; } = null!;

    public string? TimeZoneId { get; init; }

    public virtual QrtzTrigger QrtzTrigger { get; init; } = null!;
}
