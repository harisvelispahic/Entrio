using IoT.Application.Common;
using IoT.Application.Common.Exceptions;
using IoT.Domain.Entities.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/schedules")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly IAppDbContext _db;

    public SchedulesController(IAppDbContext db)
    {
        _db = db;
    }

    public sealed class CreateScheduleRequest
    {
        public DeviceCommandType CommandType { get; init; }
        public int? TargetPercentage { get; init; }

        /// <summary>When to execute, in UTC (e.g. 2026-12-25T20:30:00Z).</summary>
        public DateTime ExecuteAtUtc { get; init; }
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateScheduleRequest request,
        CancellationToken ct)
    {
        if (request.CommandType == DeviceCommandType.Vent
            && request.TargetPercentage is null or < 1 or > 99)
        {
            throw new BusinessRuleException("Vent requires a target percentage between 1 and 99.");
        }

        var executeAtUtc = request.ExecuteAtUtc.ToUniversalTime();

        if (executeAtUtc <= DateTime.UtcNow)
            throw new BusinessRuleException("Scheduled time must be in the future.");

        // Single-device system: the device is resolved here rather than taken from the
        // request, so a client cannot schedule against an arbitrary id.
        var device = await GetDeviceAsync(ct);

        var schedule = new ScheduleEntity(
            device.Id,
            request.CommandType,
            // Only Vent carries a percentage; the others ignore it.
            request.CommandType == DeviceCommandType.Vent ? request.TargetPercentage : null,
            executeAtUtc);

        _db.Schedules.Add(schedule);
        await _db.SaveChangesAsync(ct);

        return Ok(ToResponse(schedule));
    }

    /// <summary>Upcoming schedules: active and not yet triggered, soonest first.</summary>
    [HttpGet]
    public async Task<IActionResult> GetSchedules(CancellationToken ct)
    {
        // This used to take deviceId from the query string. The frontend never sent one,
        // so it filtered on Guid.Empty and the list was always empty.
        var device = await GetDeviceAsync(ct);

        var schedules = await _db.Schedules
            .Where(s => s.DeviceId == device.Id && s.IsActive && !s.WasTriggered)
            .OrderBy(s => s.ExecuteAtUtc)
            .ToListAsync(ct);

        return Ok(schedules.Select(ToResponse));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteSchedule(Guid id, CancellationToken ct)
    {
        var schedule = await _db.Schedules.FirstOrDefaultAsync(s => s.Id == id, ct)
            ?? throw new NotFoundException("Schedule not found.");

        _db.Schedules.Remove(schedule);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    private async Task<DeviceEntity> GetDeviceAsync(CancellationToken ct) =>
        await _db.Devices.AsNoTracking().FirstOrDefaultAsync(ct)
        ?? throw new NotFoundException("No device is registered in the system.");

    /// <summary>Explicit projection so the API shape does not drift with the entity.</summary>
    private static object ToResponse(ScheduleEntity schedule) => new
    {
        id = schedule.Id,
        deviceId = schedule.DeviceId,
        commandType = (int)schedule.CommandType,
        targetPercentage = schedule.TargetPercentage,
        executeAtUtc = schedule.ExecuteAtUtc,
        isActive = schedule.IsActive,
        wasTriggered = schedule.WasTriggered
    };
}
