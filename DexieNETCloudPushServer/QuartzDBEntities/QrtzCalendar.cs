// ReSharper disable EntityFramework.ModelValidation.UnlimitedStringLength

namespace DexieNETCloudPushServer.QuartzDBEntities;

public partial class QrtzCalendar
{
    public string SchedName { get; init; } = null!;

    public string CalendarName { get; init; } = null!;

    public byte[] Calendar { get; init; } = null!;
}
