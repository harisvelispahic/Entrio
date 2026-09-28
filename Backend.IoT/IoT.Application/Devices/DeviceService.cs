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

    public async Task<Device?> AuthenticateAsync(string deviceKey, CancellationToken ct = default)
    {
        // Single-device system, so there is exactly one candidate to check against.
        var device = await _db.Devices.FirstOrDefaultAsync(ct);

        if (device is null)
            return null;

        if (!_hasher.Verify(deviceKey, device.DeviceKeyHash, device.DeviceKeySalt))
            return null;

        device.LastSeenAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return device;
    }
}
