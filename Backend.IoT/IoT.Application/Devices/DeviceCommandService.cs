using IoT.Application.Common;
using IoT.Application.Common.Exceptions;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Devices;

public class DeviceCommandService : IDeviceCommandService
{
    private readonly IAppDbContext _db;

    public DeviceCommandService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<DeviceCommand> QueueAsync(
        Guid deviceId,
        DeviceCommandType commandType,
        int? targetPercentage,
        bool suppressAutoClose = false,
        CancellationToken ct = default)
    {
        // A new command supersedes anything still queued, so the device never acts on a
        // stale instruction it had not got around to polling.
        var pending = await _db.DeviceCommands
            .Where(c => c.DeviceId == deviceId && c.Status == DeviceCommandStatus.Pending)
            .ToListAsync(ct);

        foreach (var stale in pending)
            stale.Status = DeviceCommandStatus.Cancelled;

        var command = new DeviceCommand
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            CommandType = commandType,
            // Only Vent carries a percentage; the others ignore it.
            TargetPercentage = commandType == DeviceCommandType.Vent ? targetPercentage : null,
            Status = DeviceCommandStatus.Pending,
            CreatedAtUtc = DateTime.UtcNow,
            SuppressAutoClose = suppressAutoClose
        };

        _db.DeviceCommands.Add(command);
        await _db.SaveChangesAsync(ct);

        return command;
    }

    public Task<DeviceCommand?> GetPendingAsync(Guid deviceId, CancellationToken ct = default) =>
        _db.DeviceCommands
            .Where(c => c.DeviceId == deviceId && c.Status == DeviceCommandStatus.Pending)
            .OrderBy(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);

    public async Task AcknowledgeAsync(Guid deviceId, Guid commandId, CancellationToken ct = default)
    {
        var command = await _db.DeviceCommands
            .FirstOrDefaultAsync(c => c.Id == commandId && c.DeviceId == deviceId, ct)
            ?? throw new NotFoundException("Command not found.");

        // The device retries an ack it is unsure about, so a repeat must not be an error.
        if (command.Status != DeviceCommandStatus.Pending)
            return;

        command.Status = DeviceCommandStatus.Acknowledged;
        command.AcknowledgedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
    }
}
