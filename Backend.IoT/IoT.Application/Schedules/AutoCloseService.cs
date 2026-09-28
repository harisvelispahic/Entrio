using IoT.Application.Common;
using IoT.Application.Common.Exceptions;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Schedules;

public class AutoCloseService : IAutoCloseService
{
    public const int MinAfterSeconds = 5;
    public const int MaxAfterSeconds = 3600;

    private readonly IAppDbContext _db;

    public AutoCloseService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<AutoCloseSettings> GetSettingsAsync(Guid deviceId, CancellationToken ct = default) =>
        await _db.AutoCloseSettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId, ct)
        ?? throw new NotFoundException("Auto-close settings have not been initialised for this device.");

    public async Task<AutoCloseSettings> UpdateSettingsAsync(
        Guid deviceId,
        bool enabled,
        int afterSeconds,
        CancellationToken ct = default)
    {
        if (afterSeconds is < MinAfterSeconds or > MaxAfterSeconds)
        {
            throw new BusinessRuleException(
                $"Auto-close delay must be between {MinAfterSeconds} and {MaxAfterSeconds} seconds.");
        }

        var settings = await GetSettingsAsync(deviceId, ct);

        settings.Enabled = enabled;
        settings.AfterSeconds = afterSeconds;

        await _db.SaveChangesAsync(ct);

        return settings;
    }

    public async Task ArmAsync(Guid deviceId, CancellationToken ct = default)
    {
        var settings = await _db.AutoCloseSettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId, ct);

        if (settings is null || !settings.Enabled)
            return;

        // Supersede any pending auto-close, so re-opening restarts the countdown rather
        // than leaving an older, earlier close still armed.
        var pending = await _db.Schedules
            .Where(s => s.DeviceId == deviceId && s.IsActive && !s.WasTriggered)
            .ToListAsync(ct);

        foreach (var schedule in pending)
            schedule.IsActive = false;

        _db.Schedules.Add(new Schedule
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            CommandType = DeviceCommandType.Close,
            TargetPercentage = null,
            // Measured from now, which is when the door finished opening: the event that
            // triggers this is raised on travel completion, not on acknowledgement.
            ExecuteAtUtc = DateTime.UtcNow.AddSeconds(settings.AfterSeconds),
            IsActive = true,
            WasTriggered = false
        });

        await _db.SaveChangesAsync(ct);
    }
}
