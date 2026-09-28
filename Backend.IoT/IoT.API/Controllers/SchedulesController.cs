using IoT.Application.Devices;
using IoT.Application.Schedules;
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

    [HttpPost]
    public async Task<ActionResult<ScheduleEntry>> Create(
        [FromBody] CreateScheduleRequest request,
        CancellationToken ct)
    {
        var device = await _devices.GetAsync(ct);

        return Ok(await _schedules.CreateAsync(device.Id, request, ct));
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ScheduleEntry>>> GetSchedules(CancellationToken ct)
    {
        var device = await _devices.GetAsync(ct);

        return Ok(await _schedules.GetUpcomingAsync(device.Id, ct));
    }

    /// <summary>Deletes a scheduled period by its group id, removing both halves.</summary>
    [HttpDelete("{groupId:guid}")]
    public async Task<IActionResult> Delete(Guid groupId, CancellationToken ct)
    {
        await _schedules.DeleteAsync(groupId, ct);

        return NoContent();
    }
}
