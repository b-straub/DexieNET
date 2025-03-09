// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DexieNETCloudPushServer.QuartzDBEntities;

public class QrtzBlobTrigger
{
    public string SchedName { get; init; } = null!;

    public string TriggerName { get; init; } = null!;

    public string TriggerGroup { get; init; } = null!;

    public byte[]? BlobData { get; init; }

    public QrtzTrigger QrtzTrigger { get; init; } = null!;
}