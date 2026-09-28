using IoT.Application.Common;
using IoT.Application.Identity;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Devices;

public class DeviceAuthenticator : IDeviceAuthenticator
{
    private readonly IAppDbContext _db;
    private readonly IPasswordHasher _hasher;

    public DeviceAuthenticator(IAppDbContext db, IPasswordHasher hasher)
    {
        _db = db;
        _hasher = hasher;
    }

    public async Task<DeviceEntity?> AuthenticateAsync(string deviceKey)
    {
        var device = await _db.Devices.SingleOrDefaultAsync();
        if (device == null)
            return null;

        var isValid = _hasher.Verify(
            deviceKey,
            device.DeviceKeyHash,
            device.DeviceKeySalt
        );

        if (!isValid)
            return null;

        device.UpdateLastSeen();
        await _db.SaveChangesAsync();

        return device;
    }
}
