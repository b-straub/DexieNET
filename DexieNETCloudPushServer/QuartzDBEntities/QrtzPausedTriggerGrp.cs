// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DexieNETCloudPushServer.QuartzDBEntities;

public partial class QrtzPausedTriggerGrp
{
    public string SchedName { get; init; } = null!;

    public string TriggerGroup { get; init; } = null!;
}
