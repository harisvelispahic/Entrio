using IoT.Application.Common;
using IoT.Application.Devices.Services;
using IoT.Domain.Entities.Devices;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Devices.Events;

public sealed class CreateDeviceEventCommandHandler
    : IRequestHandler<CreateDeviceEventCommand>
{
    private readonly IAppDbContext _db;
    private readonly AutoCloseService _autoClose;

    public CreateDeviceEventCommandHandler(
        IAppDbContext db,
        AutoCloseService autoClose)
    {
        _db = db;
        _autoClose = autoClose;
    }

    public async Task Handle(
        CreateDeviceEventCommand request,
        CancellationToken cancellationToken)
    {
        var ev = new DeviceEventEntity(
            request.DeviceId,
            request.Type,
            request.Source
        );

        _db.DeviceEvents.Add(ev);
        await _db.SaveChangesAsync(cancellationToken);

        // Auto-close only follows a genuine open, and only when the command that caused
        // it did not explicitly suppress it -- a scheduled close must not re-arm one.
        if (request.Type != DeviceEventType.DoorOpened)
            return;

        var lastCommand = await _db.DeviceCommands
            .Where(c => c.DeviceId == request.DeviceId)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        // Arm auto-close unless the open was caused by a command that suppressed it.
        //
        // The CommandType == Close clause is load-bearing (added in 62c035d, "Fixed RFID
        // opening logic"): an RFID open happens entirely on the device and creates no
        // command row, so `lastCommand` is whatever ran previously -- often a scheduled
        // Close carrying SuppressAutoClose. Without this clause that stale row would
        // suppress auto-close for a local open it had nothing to do with. A last command
        // of Close means this DoorOpened cannot have come from it.
        var causedBySuppressingCommand =
            lastCommand is not null
            && lastCommand.SuppressAutoClose
            && lastCommand.CommandType != DeviceCommandType.Close;

        if (!causedBySuppressingCommand)
            await _autoClose.ScheduleAutoCloseAsync(request.DeviceId, cancellationToken);
    }
}
