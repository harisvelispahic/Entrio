using FluentValidation;
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
    private readonly IDeviceEventService _events;
    private readonly IValidator<CreateScheduleRequest> _createValidator;

    public ScheduleService(
        IAppDbContext db,
        IDeviceCommandService commands,
        IDeviceEventService events,
        IValidator<CreateScheduleRequest> createValidator)
    {
        _db = db;
        _commands = commands;
        _events = events;
        _createValidator = createValidator;
    }

    public async Task<IReadOnlyList<ScheduleEntry>> GetUpcomingAsync(
        Guid deviceId,
        CancellationToken ct = default)
    {
        var pending = await _db.Schedules
            .AsNoTracking()
            .Where(s => s.DeviceId == deviceId && s.IsActive && !s.WasTriggered)
            .ToListAsync(ct);

        // A user period is two rows sharing a group id; collapse it into one entry so the
        // list shows "Open 14:00 until 15:00" rather than two unrelated-looking lines.
        var periods = pending
            .Where(s => s.ScheduleGroupId.HasValue)
            .GroupBy(s => s.ScheduleGroupId!.Value)
            .Select(group => BuildPeriod(group.Key, group.ToList()))
            .Where(entry => entry is not null)
            .Select(entry => entry!);

        // Rows with no group are system-raised; currently that means auto-close only.
        var autoCloses = pending
            .Where(s => !s.ScheduleGroupId.HasValue)
            .Select(s => new ScheduleEntry(
                s.Id,
                ScheduleEntryKind.AutoClose,
                s.CommandType,
                s.TargetPercentage,
                OpensAtUtc: null,
                s.ExecuteAtUtc));

        return periods.Concat(autoCloses)
            .OrderBy(entry => entry.OpensAtUtc ?? entry.ClosesAtUtc)
            .ToList();
    }

    public async Task<ScheduleEntry> CreateAsync(
        Guid deviceId,
        CreateScheduleRequest request,
        CancellationToken ct = default)
    {
        await _createValidator.ValidateAndThrowAsync(request, ct);

        var groupId = Guid.NewGuid();

        var open = new Schedule
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            ScheduleGroupId = groupId,
            CommandType = request.CommandType,
            TargetPercentage = request.CommandType == DeviceCommandType.Vent
                ? request.TargetPercentage
                : null,
            ExecuteAtUtc = request.OpensAtUtc,
            IsActive = true,
            WasTriggered = false
        };

        var close = new Schedule
        {
            Id = Guid.NewGuid(),
            DeviceId = deviceId,
            ScheduleGroupId = groupId,
            CommandType = DeviceCommandType.Close,
            TargetPercentage = null,
            ExecuteAtUtc = request.ClosesAtUtc,
            IsActive = true,
            WasTriggered = false
        };

        _db.Schedules.AddRange(open, close);
        await _db.SaveChangesAsync(ct);

        return new ScheduleEntry(
            groupId,
            ScheduleEntryKind.Period,
            open.CommandType,
            open.TargetPercentage,
            open.ExecuteAtUtc,
            close.ExecuteAtUtc);
    }

    public async Task DeleteAsync(Guid groupId, CancellationToken ct = default)
    {
        var rows = await _db.Schedules
            .Where(s => s.ScheduleGroupId == groupId)
            .ToListAsync(ct);

        if (rows.Count == 0)
            throw new NotFoundException("Schedule not found.");

        // Both halves go together: a period whose close was deleted would leave the door
        // open indefinitely, which is the thing the pairing exists to prevent.
        _db.Schedules.RemoveRange(rows);
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

            // Record WHY the door is about to move. Without this the event log only ever
            // showed the controller's own DoorOpened/DoorClosed, so the analytics source
            // breakdown could never report Schedule or AutoClose -- two of its five
            // categories were unreachable.
            var isAutoClose = schedule.ScheduleGroupId is null;

            await _events.RecordAsync(
                schedule.DeviceId,
                isAutoClose ? DeviceEventType.AutoCloseTriggered : DeviceEventType.ScheduleTriggered,
                isAutoClose ? DeviceEventSource.AutoClose : DeviceEventSource.Schedule,
                details: DescribeCommand(schedule),
                ct);
        }

        await _db.SaveChangesAsync(ct);

        return due.Count;
    }

    /// <summary>Fills the Details column in the event log, which was always empty before.</summary>
    private static string DescribeCommand(Schedule schedule) =>
        schedule.CommandType == DeviceCommandType.Vent
            ? $"Vent to {schedule.TargetPercentage}%"
            : schedule.CommandType.ToString();

    /// <summary>
    /// Turns the rows of one group into a single entry. Returns null once the opening half
    /// has already fired, because a period whose close is all that remains is no longer
    /// something the user can meaningfully act on.
    /// </summary>
    private static ScheduleEntry? BuildPeriod(Guid groupId, List<Schedule> rows)
    {
        var open = rows.FirstOrDefault(s => s.CommandType != DeviceCommandType.Close);
        var close = rows.FirstOrDefault(s => s.CommandType == DeviceCommandType.Close);

        if (open is null || close is null)
            return null;

        return new ScheduleEntry(
            groupId,
            ScheduleEntryKind.Period,
            open.CommandType,
            open.TargetPercentage,
            open.ExecuteAtUtc,
            close.ExecuteAtUtc);
    }
}
