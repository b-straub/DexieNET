// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DexieNETCloudPushServer.QuartzDBEntities;

public partial class QrtzSimpleTrigger
{
    public string SchedName { get; init; } = null!;

    public string TriggerName { get; init; } = null!;

    public string TriggerGroup { get; init; } = null!;

    public long RepeatCount { get; init; }

    public long RepeatInterval { get; init; }

    public long TimesTriggered { get; init; }

    public virtual QrtzTrigger QrtzTrigger { get; init; } = null!;
}
