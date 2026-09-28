using FluentValidation;
using IoT.Application.Common;
using IoT.Application.Schedules;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Devices;

public class DeviceStatusService : IDeviceStatusService
{
    private readonly IAppDbContext _db;
    private readonly IValidator<DeviceStatusRequest> _validator;
    private readonly IAutoCloseService _autoClose;

    public DeviceStatusService(
        IAppDbContext db,
        IValidator<DeviceStatusRequest> validator,
        IAutoCloseService autoClose)
    {
        _db = db;
        _validator = validator;
        _autoClose = autoClose;
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

        // Captured before the update: auto-close reacts to the TRANSITION, not to the
        // state itself. The controller re-reports its current status every 10s, so
        // acting on the state alone would reset the countdown forever and it would
        // never fire.
        var previousState = status.DoorState;

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

        await ReactToTransitionAsync(deviceId, previousState, request, ct);
    }

    /// <summary>
    /// Auto-close normally arms on a DoorOpened event, which the controller raises only
    /// when travel COMPLETES. Stopping the door part-way therefore raises no event, and
    /// the door would sit half-open indefinitely. Arming here covers that: a door that
    /// stopped anywhere other than closed is open as far as the user is concerned.
    /// </summary>
    private async Task ReactToTransitionAsync(
        Guid deviceId,
        DoorState previousState,
        DeviceStatusRequest request,
        CancellationToken ct)
    {
        if (request.DoorState == previousState)
            return;

        if (request.DoorState == DoorState.Stopped && request.PositionPercent > 0)
        {
            await _autoClose.ArmAsync(deviceId, ct);
            return;
        }

        // Reached closed by any route -- auto-close, a schedule, the obstacle resume, or
        // by hand. A pending auto-close is now pointless, and firing it later would queue
        // a Close against an already-closed door.
        if (request.DoorState == DoorState.Closed)
            await _autoClose.CancelPendingAsync(deviceId, ct);
    }

    private static DeviceStatus NewClosedStatus(Guid deviceId) => new()
    {
        DeviceId = deviceId,
        DoorState = DoorState.Closed,
        PositionPercent = 0,
        UpdatedAtUtc = DateTime.UtcNow
    };
}
