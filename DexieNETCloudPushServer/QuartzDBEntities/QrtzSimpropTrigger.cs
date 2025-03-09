// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DexieNETCloudPushServer.QuartzDBEntities;

public partial class QrtzSimpropTrigger
{
    public string SchedName { get; init; } = null!;

    public string TriggerName { get; init; } = null!;

    public string TriggerGroup { get; init; } = null!;

    public string? StrProp1 { get; init; }

    public string? StrProp2 { get; init; }

    public string? StrProp3 { get; init; }

    public int? IntProp1 { get; init; }

    public int? IntProp2 { get; init; }

    public long? LongProp1 { get; init; }

    public long? LongProp2 { get; init; }

    public decimal? DecProp1 { get; init; }

    public decimal? DecProp2 { get; init; }

    public bool? BoolProp1 { get; init; }

    public bool? BoolProp2 { get; init; }

    public string? TimeZoneId { get; init; }

    public virtual QrtzTrigger QrtzTrigger { get; init; } = null!;
}
