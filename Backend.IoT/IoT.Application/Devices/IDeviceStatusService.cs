using IoT.Domain.Entities.Devices;

namespace IoT.Application.Devices;

public interface IDeviceStatusService
{
    /// <summary>
    /// The device's status row, created as closed if it has never reported. Returning a
    /// safe default beats 404ing a dashboard that is simply waiting for a first report.
    /// </summary>
    Task<DeviceStatus> GetOrCreateAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>Applies a status report from the controller.</summary>
    Task UpdateAsync(Guid deviceId, DeviceStatusRequest request, CancellationToken ct = default);
}
