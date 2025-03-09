// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DexieNETCloudPushServer.QuartzDBEntities;

public partial class QrtzJobDetail
{
    public string SchedName { get; init; } = null!;

    public string JobName { get; init; } = null!;

    public string JobGroup { get; init; } = null!;

    public string? Description { get; init; }

    public string JobClassName { get; init; } = null!;

    public bool IsDurable { get; init; }

    public bool IsNonconcurrent { get; init; }

    public bool IsUpdateData { get; init; }

    public bool RequestsRecovery { get; init; }

    public byte[]? JobData { get; init; }

    public virtual ICollection<QrtzTrigger> QrtzTriggers { get; init; } = new List<QrtzTrigger>();
}
