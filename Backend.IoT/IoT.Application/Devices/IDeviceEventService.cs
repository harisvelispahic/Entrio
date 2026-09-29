using IoT.Domain.Entities.Devices;

namespace IoT.Application.Devices;

public interface IDeviceEventService
{
    /// <summary>
    /// Records something the CONTROLLER reported. Takes the raw request because the
    /// firmware sends enum names as strings; parsing them is the service's job, not the
    /// controller's.
    /// </summary>
    Task RecordAsync(Guid deviceId, DeviceEventRequest request, CancellationToken ct = default);

    /// <summary>
    /// Records an event the BACKEND itself raised, such as a schedule firing. Skips the
    /// request validator because the enums are already typed at the call site.
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
