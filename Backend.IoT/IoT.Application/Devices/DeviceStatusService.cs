using IoT.Application.Common;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Devices;

public class DeviceStatusService : IDeviceStatusService
{
    private readonly IAppDbContext _db;

    public DeviceStatusService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<DeviceStatus> GetOrCreateAsync(Guid deviceId, CancellationToken ct = default)
    {
        var status = await _db.DeviceStatuses.FirstOrDefaultAsync(s => s.DeviceId == deviceId, ct);

        if (status is not null)
            return status;

        status = NewClosedStatus(deviceId);

        _db.DeviceStatuses.Add(status);
        await _db.SaveChangesAsync(ct);

        return status;
    }

    public async Task UpdateAsync(
        Guid deviceId,
        DoorState doorState,
        int positionPercent,
        bool obstacleDetected,
        CancellationToken ct = default)
    {
        var status = await GetOrCreateAsync(deviceId, ct);

        status.DoorState = doorState;
        status.PositionPercent = positionPercent;
        status.ObstacleDetected = obstacleDetected;

        // OpenedAtUtc tracks the current open period, so it is set on the way open and
        // cleared on the way closed rather than being a running timestamp.
        if (doorState == DoorState.Open && status.OpenedAtUtc is null)
            status.OpenedAtUtc = DateTime.UtcNow;

        if (doorState == DoorState.Closed)
            status.OpenedAtUtc = null;

        status.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }

    private static DeviceStatus NewClosedStatus(Guid deviceId) => new()
    {
        DeviceId = deviceId,
        DoorState = DoorState.Closed,
        PositionPercent = 0,
        UpdatedAtUtc = DateTime.UtcNow
    };
}
