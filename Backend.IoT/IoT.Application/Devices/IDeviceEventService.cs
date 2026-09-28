using IoT.Domain.Entities.Devices;

namespace IoT.Application.Devices;

public interface IDeviceEventService
{
    /// <summary>
    /// Records something the controller reported, and arms auto-close when the report is
    /// a genuine open.
    /// </summary>
    Task RecordAsync(
        Guid deviceId,
        DeviceEventType eventType,
        DeviceEventSource source,
        string? details = null,
        CancellationToken ct = default);

    /// <summary>Most recent events first, capped so the endpoint cannot dump the table.</summary>
    Task<IReadOnlyList<DeviceEvent>> GetRecentAsync(int limit = 200, CancellationToken ct = default);
}
