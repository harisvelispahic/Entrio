using IoT.API.Security;
using IoT.Application.Devices;
using IoT.Domain.Entities.Devices;
using Microsoft.AspNetCore.Mvc;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/device/status")]
public class DeviceStatusController : ControllerBase
{
    private readonly IDeviceStatusService _statuses;

    public DeviceStatusController(IDeviceStatusService statuses)
    {
        _statuses = statuses;
    }

    public sealed class UpdateStatusRequest
    {
        public DoorState DoorState { get; init; }
        public int PositionPercent { get; init; }
        public bool ObstacleDetected { get; init; }
    }

    [DeviceAuthorize]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromBody] UpdateStatusRequest request, CancellationToken ct)
    {
        // The device is resolved from its key, not from a deviceId in the body: an
        // authenticated device can only ever report its own status.
        var device = (Device)HttpContext.Items["Device"]!;

        await _statuses.UpdateAsync(
            device.Id,
            request.DoorState,
            request.PositionPercent,
            request.ObstacleDetected,
            ct);

        return NoContent();
    }
}
