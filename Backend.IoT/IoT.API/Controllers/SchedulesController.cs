using IoT.Application.Devices;
using IoT.Application.Schedules;
using IoT.Domain.Entities.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/schedules")]
[Authorize]
public class SchedulesController : ControllerBase
{
    private readonly IScheduleService _schedules;
    private readonly IDeviceService _devices;

    public SchedulesController(IScheduleService schedules, IDeviceService devices)
    {
        _schedules = schedules;
        _devices = devices;
    }

    public sealed class CreateScheduleRequest
    {
        public DeviceCommandType CommandType { get; init; }
        public int? TargetPercentage { get; init; }

        /// <summary>When to execute, as a UTC instant (e.g. 2026-12-25T20:30:00Z).</summary>
        public DateTime ExecuteAtUtc { get; init; }
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateScheduleRequest request, CancellationToken ct)
    {
        var device = await _devices.GetAsync(ct);

        var schedule = await _schedules.CreateAsync(
            device.Id,
            request.CommandType,
            request.TargetPercentage,
            request.ExecuteAtUtc,
            ct);

        return Ok(ToResponse(schedule));
    }

    [HttpGet]
    public async Task<IActionResult> GetSchedules(CancellationToken ct)
    {
        var device = await _devices.GetAsync(ct);
        var schedules = await _schedules.GetUpcomingAsync(device.Id, ct);

        return Ok(schedules.Select(ToResponse));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _schedules.DeleteAsync(id, ct);

        return NoContent();
    }

    /// <summary>Explicit projection so the API shape does not drift with the entity.</summary>
    private static object ToResponse(Schedule schedule) => new
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
