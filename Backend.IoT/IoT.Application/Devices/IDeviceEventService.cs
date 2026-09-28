using IoT.Domain.Entities.Devices;

namespace IoT.Application.Devices;

public interface IDeviceEventService
{
    /// <summary>
    /// Records something the controller reported, and arms auto-close when the report is
    /// a genuine open. Takes the raw request because the firmware sends enum names as
    /// strings; parsing them is the service's job, not the controller's.
    /// </summary>
    Task RecordAsync(Guid deviceId, DeviceEventRequest request, CancellationToken ct = default);

    /// <summary>Most recent events first, capped so the endpoint cannot dump the table.</summary>
    Task<IReadOnlyList<DeviceEvent>> GetRecentAsync(int limit = 200, CancellationToken ct = default);
}
