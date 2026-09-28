using IoT.Application.Common;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Devices.Services;

/// <summary>
/// Queues an automatic close after the door is opened, when the feature is enabled for
/// the device. Implemented as a schedule rather than a timer so it survives a restart:
/// ScheduleWorker picks it up the same way it picks up user-created schedules.
/// </summary>
public class AutoCloseService
{
    private readonly IAppDbContext _db;

    public AutoCloseService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task ScheduleAutoCloseAsync(Guid deviceId, CancellationToken ct = default)
    {
        var settings = await _db.AutoCloseSettings
            .FirstOrDefaultAsync(x => x.DeviceId == deviceId, ct);

        if (settings is null || !settings.Enabled)
            return;

        // Supersede any pending auto-close, so re-opening the door restarts the countdown
        // instead of leaving an older, earlier close still armed.
        var pending = await _db.Schedules
            .Where(s => s.DeviceId == deviceId && s.IsActive && !s.WasTriggered)
            .ToListAsync(ct);

        foreach (var schedule in pending)
            schedule.Deactivate();

        _db.Schedules.Add(new ScheduleEntity(
            deviceId: deviceId,
            commandType: DeviceCommandType.Close,
            targetPercentage: null,
            executeAtUtc: DateTime.UtcNow.AddSeconds(settings.AfterSeconds)));

        await _db.SaveChangesAsync(ct);
    }
}
