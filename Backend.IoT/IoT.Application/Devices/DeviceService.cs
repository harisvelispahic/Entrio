using IoT.Application.Common;
using IoT.Application.Common.Exceptions;
using IoT.Application.Identity;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Devices;

public class DeviceService : IDeviceService
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;

    public DeviceService(IAppDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<Device> GetAsync(CancellationToken ct = default) =>
        await _db.Devices.FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("No device is registered in the system.");

    /// <summary>Value the simulator sends in X-Device-Client to identify itself.</summary>
    private const string SimulatorClient = "simulator";

    public async Task<Device?> AuthenticateAsync(
        string deviceKey,
        string? clientHeader = null,
        CancellationToken ct = default)
    {
        // Single-device system, so there is exactly one candidate to check against.
        var device = await _db.Devices.FirstOrDefaultAsync(ct);

        if (device is null)
            return null;

        if (!_hasher.Verify(deviceKey, device.DeviceKeyHash, device.DeviceKeySalt))
            return null;

        device.LastSeenAtUtc = DateTime.UtcNow;

        // No header means the ESP32 firmware, which sends none. Only the simulator
        // identifies itself, so hardware needs no firmware change to be reported
        // correctly.
        device.LastClientKind = string.Equals(clientHeader, SimulatorClient, StringComparison.OrdinalIgnoreCase)
            ? DeviceClientKind.Simulator
            : DeviceClientKind.Hardware;

        await _db.SaveChangesAsync(ct);

        return device;
    }
}
