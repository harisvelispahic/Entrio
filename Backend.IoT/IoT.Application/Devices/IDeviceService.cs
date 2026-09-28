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

    /// <summary>
    /// Verifies an X-Device-Key, bumps LastSeenAtUtc and records what reported in.
    /// Null when the key does not match.
    /// </summary>
    /// <param name="clientHeader">
    /// The X-Device-Client header, if any. The firmware sends none, so null means hardware.
    /// </param>
    Task<Device?> AuthenticateAsync(
        string deviceKey,
        string? clientHeader = null,
        CancellationToken ct = default);
}
