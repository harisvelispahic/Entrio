using FluentValidation;
using IoT.Application.Common;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Devices;

public class DeviceStatusService : IDeviceStatusService
{
    private readonly IAppDbContext _db;
    private readonly IValidator<DeviceStatusRequest> _validator;

    public DeviceStatusService(IAppDbContext db, IValidator<DeviceStatusRequest> validator)
    {
        _db = db;
        _validator = validator;
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
        DeviceStatusRequest request,
        CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        var status = await GetOrCreateAsync(deviceId, ct);

        status.DoorState = request.DoorState;
        status.PositionPercent = request.PositionPercent;
        status.ObstacleDetected = request.ObstacleDetected;

        // OpenedAtUtc tracks the current open period, so it is set on the way open and
        // cleared on the way closed rather than being a running timestamp.
        if (request.DoorState == DoorState.Open && status.OpenedAtUtc is null)
            status.OpenedAtUtc = DateTime.UtcNow;

        if (request.DoorState == DoorState.Closed)
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
