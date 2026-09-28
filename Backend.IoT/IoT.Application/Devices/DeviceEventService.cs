using FluentValidation;
using IoT.Application.Common;
using IoT.Application.Schedules;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Devices;

public class DeviceEventService : IDeviceEventService
{
    private readonly IAppDbContext _db;
    private readonly IAutoCloseService _autoClose;
    private readonly IValidator<DeviceEventRequest> _validator;

    public DeviceEventService(
        IAppDbContext db,
        IAutoCloseService autoClose,
        IValidator<DeviceEventRequest> validator)
    {
        _db = db;
        _autoClose = autoClose;
        _validator = validator;
    }

    public async Task RecordAsync(
        Guid deviceId,
        DeviceEventRequest request,
        CancellationToken ct = default)
    {
        await _validator.ValidateAndThrowAsync(request, ct);

        // Safe to parse unchecked: the validator has already rejected anything unknown.
        var eventType = Enum.Parse<DeviceEventType>(request.Type, ignoreCase: true);

        // Source is optional; the firmware omits it for system-raised events.
        var source = string.IsNullOrWhiteSpace(request.Source)
            ? DeviceEventSource.System
            : Enum.Parse<DeviceEventSource>(request.Source, ignoreCase: true);

        _db.DeviceEvents.Add(new DeviceEvent
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            EventType = eventType,
            Source = source,
            Details = null,
            OccurredAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(ct);

        if (eventType != DeviceEventType.DoorOpened)
            return;

        var lastCommand = await _db.DeviceCommands
            .Where(c => c.DeviceId == deviceId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

        // Arm auto-close unless the open was caused by a command that suppressed it.
        //
        // The CommandType == Close clause is load-bearing (added in 62c035d, "Fixed RFID
        // opening logic"): an RFID open happens entirely on the device and creates no
        // command row, so lastCommand is whatever ran previously -- often a scheduled
        // Close carrying SuppressAutoClose. Without this clause that stale row would
        // suppress auto-close for a local open it had nothing to do with. A last command
        // of Close means this DoorOpened cannot have come from it.
        var causedBySuppressingCommand =
            lastCommand is not null
            && lastCommand.SuppressAutoClose
            && lastCommand.CommandType != DeviceCommandType.Close;

        if (!causedBySuppressingCommand)
            await _autoClose.ArmAsync(deviceId, ct);
    }

    public async Task<IReadOnlyList<DeviceEvent>> GetRecentAsync(int limit = 200, CancellationToken ct = default) =>
        await _db.DeviceEvents
            .AsNoTracking()
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(limit)
            .ToListAsync(ct);
}
