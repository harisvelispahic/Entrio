using IoT.Application.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]
public class EventsController : ControllerBase
{
    private readonly IDeviceEventService _events;

    public EventsController(IDeviceEventService events)
    {
        _events = events;
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<DeviceEventResponse>>> GetEvents(CancellationToken ct)
    {
        var events = await _events.GetRecentAsync(ct: ct);

        return Ok(events.Select(e => new DeviceEventResponse(
            e.Id,
            e.EventType.ToString(),
            e.Source.ToString(),
            e.OccurredAtUtc,
            e.Details)).ToList());
    }
}
