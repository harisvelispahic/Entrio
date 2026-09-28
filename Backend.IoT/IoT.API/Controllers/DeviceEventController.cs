using IoT.API.Security;
using IoT.Application.Devices;
using IoT.Domain.Entities.Devices;
using Microsoft.AspNetCore.Mvc;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/device/events")]
public class DeviceEventsController : ControllerBase
{
    private readonly IDeviceEventService _events;

    public DeviceEventsController(IDeviceEventService events)
    {
        _events = events;
    }

    [DeviceAuthorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DeviceEventRequest request, CancellationToken ct)
    {
        var device = (Device)HttpContext.Items[DeviceAuthorizeAttribute.DeviceItemKey]!;

        await _events.RecordAsync(device.Id, request, ct);

        return Ok();
    }
}
