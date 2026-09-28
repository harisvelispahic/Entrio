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

    [DeviceAuthorize]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus([FromBody] DeviceStatusRequest request, CancellationToken ct)
    {
        // The device is resolved from its key, not from a deviceId in the body: an
        // authenticated device can only ever report its own status.
        var device = (Device)HttpContext.Items[DeviceAuthorizeAttribute.DeviceItemKey]!;

        await _statuses.UpdateAsync(device.Id, request, ct);

        return NoContent();
    }
}
