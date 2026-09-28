using FluentValidation;
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
    private readonly IValidator<AutoCloseSettingsRequest> _settingsValidator;

    public AutoCloseService(IAppDbContext db, IValidator<AutoCloseSettingsRequest> settingsValidator)
    {
        _db = db;
        _settingsValidator = settingsValidator;
    }

    public async Task<AutoCloseSettings> GetSettingsAsync(Guid deviceId, CancellationToken ct = default) =>
        await _db.AutoCloseSettings.FirstOrDefaultAsync(s => s.DeviceId == deviceId, ct)
        ?? throw new NotFoundException("Auto-close settings have not been initialised for this device.");

    public async Task<AutoCloseSettings> UpdateSettingsAsync(
        Guid deviceId,
        AutoCloseSettingsRequest request,
        CancellationToken ct = default)
    {
        await _settingsValidator.ValidateAndThrowAsync(request, ct);

        var settings = await GetSettingsAsync(deviceId, ct);

        settings.Enabled = request.Enabled;
        settings.AfterSeconds = request.AfterSeconds;

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
        await DeactivatePendingAutoCloseAsync(deviceId, ct);

        _db.Schedules.Add(new Schedule
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            // No group: this is system-raised, not half of a user period. That is what
            // keeps it out of the user's schedule list and out of their deletes.
            ScheduleGroupId = null,
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

    public async Task CancelPendingAsync(Guid deviceId, CancellationToken ct = default)
    {
        await DeactivatePendingAutoCloseAsync(deviceId, ct);
        await _db.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Deactivates pending AUTO-CLOSE rows only, identified by having no schedule group.
    ///
    /// This used to deactivate every pending row for the device, so arming auto-close
    /// silently cancelled the user's own schedules: create an Open for tomorrow, open the
    /// door once with auto-close enabled, and tomorrow's schedule was gone.
    /// </summary>
    private async Task DeactivatePendingAutoCloseAsync(Guid deviceId, CancellationToken ct)
    {
        var pending = await _db.Schedules
            .Where(s => s.DeviceId == deviceId
                        && s.ScheduleGroupId == null
                        && s.IsActive
                        && !s.WasTriggered)
            .ToListAsync(ct);

        foreach (var schedule in pending)
            schedule.IsActive = false;
    }
}
