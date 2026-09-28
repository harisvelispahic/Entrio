using IoT.API.Security;
using IoT.Application.Common.Exceptions;
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

    /// <summary>
    /// Matches the body the firmware builds in sendDeviceEvent: enum NAMES, not numbers.
    /// </summary>
    public sealed class DeviceEventRequest
    {
        public string Type { get; init; } = null!;
        public string? Source { get; init; }
    }

    [DeviceAuthorize]
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DeviceEventRequest request, CancellationToken ct)
    {
        var device = (Device)HttpContext.Items["Device"]!;

        if (!Enum.TryParse<DeviceEventType>(request.Type, ignoreCase: true, out var eventType))
            throw new BusinessRuleException($"Invalid event type: {request.Type}");

        // Source is optional; the firmware omits it for system-raised events.
        var source = DeviceEventSource.System;

        if (!string.IsNullOrWhiteSpace(request.Source)
            && !Enum.TryParse(request.Source, ignoreCase: true, out source))
        {
            throw new BusinessRuleException($"Invalid event source: {request.Source}");
        }

        await _events.RecordAsync(device.Id, eventType, source, ct: ct);

        return Ok();
    }
}
