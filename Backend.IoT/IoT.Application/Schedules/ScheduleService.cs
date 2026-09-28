using IoT.Application.Common;
using IoT.Application.Common.Exceptions;
using IoT.Application.Devices;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Schedules;

public class ScheduleService : IScheduleService
{
    private readonly IAppDbContext _db;
    private readonly IDeviceCommandService _commands;

    public ScheduleService(IAppDbContext db, IDeviceCommandService commands)
    {
        _db = db;
        _commands = commands;
    }

    public async Task<IReadOnlyList<Schedule>> GetUpcomingAsync(Guid deviceId, CancellationToken ct = default) =>
        await _db.Schedules
            .AsNoTracking()
            .Where(s => s.DeviceId == deviceId && s.IsActive && !s.WasTriggered)
            .OrderBy(s => s.ExecuteAtUtc)
            .ToListAsync(ct);

    public async Task<Schedule> CreateAsync(
        Guid deviceId,
        DeviceCommandType commandType,
        int? targetPercentage,
        DateTime executeAtUtc,
        CancellationToken ct = default)
    {
        if (commandType == DeviceCommandType.Vent && targetPercentage is null or < 1 or > 99)
            throw new BusinessRuleException("Vent requires a target percentage between 1 and 99.");

        if (executeAtUtc <= DateTime.UtcNow)
            throw new BusinessRuleException("Scheduled time must be in the future.");

        var schedule = new Schedule
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            CommandType = commandType,
            TargetPercentage = commandType == DeviceCommandType.Vent ? targetPercentage : null,
            ExecuteAtUtc = executeAtUtc,
            IsActive = true,
            WasTriggered = false
        };

        _db.Schedules.Add(schedule);
        await _db.SaveChangesAsync(ct);

        return schedule;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var schedule = await _db.Schedules.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("Schedule not found.");

        _db.Schedules.Remove(schedule);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<int> TriggerDueAsync(CancellationToken ct = default)
    {
        var nowUtc = DateTime.UtcNow;

        var due = await _db.Schedules
            .Where(s => s.IsActive && !s.WasTriggered && s.ExecuteAtUtc <= nowUtc)
            .ToListAsync(ct);

        if (due.Count == 0)
            return 0;

        foreach (var schedule in due)
        {
            // suppressAutoClose: a scheduled action must not arm another auto-close, or an
            // auto-close would schedule the next one and the door would cycle forever.
            await _commands.QueueAsync(
                schedule.DeviceId,
                schedule.CommandType,
                schedule.TargetPercentage,
                suppressAutoClose: true,
                ct);

            schedule.WasTriggered = true;
        }

        await _db.SaveChangesAsync(ct);

        return due.Count;
    }
}
