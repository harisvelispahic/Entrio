using IoT.Domain.Entities.Devices;

namespace IoT.Application.Devices;

public interface IDeviceService
{
    /// <summary>
    /// The one device in the system. Throws NotFoundException when nothing is registered,
    /// which callers deliberately do not catch — an unseeded database is a setup error,
    /// not a request the caller can recover from.
    /// </summary>
    Task<Device> GetAsync(CancellationToken ct = default);

    /// <summary>Verifies an X-Device-Key and bumps LastSeenAtUtc. Null when it does not match.</summary>
    Task<Device?> AuthenticateAsync(string deviceKey, CancellationToken ct = default);
}
