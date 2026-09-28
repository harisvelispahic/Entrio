using IoT.API.Security;
using IoT.Application.Devices;
using IoT.Domain.Entities.Devices;
using Microsoft.AspNetCore.Mvc;

namespace IoT.API.Controllers;

/// <summary>
/// The controller-facing half of the command loop. The device polls for work and
/// acknowledges it; nothing is ever pushed to the device.
/// </summary>
[ApiController]
[Route("api/device/commands")]
public class DeviceCommandsController : ControllerBase
{
    private readonly IDeviceCommandService _commands;

    public DeviceCommandsController(IDeviceCommandService commands)
    {
        _commands = commands;
    }

    private Device CurrentDevice => (Device)HttpContext.Items[DeviceAuthorizeAttribute.DeviceItemKey]!;

    [DeviceAuthorize]
    [HttpGet("pending")]
    public async Task<IActionResult> GetPending(CancellationToken ct)
    {
        var command = await _commands.GetPendingAsync(CurrentDevice.Id, ct);

        // 204 rather than an empty body: the firmware branches on the status code.
        if (command is null)
            return NoContent();

        return Ok(new
        {
            id = command.Id,
            commandType = (int)command.CommandType,
            targetPercentage = command.TargetPercentage
        });
    }

    [DeviceAuthorize]
    [HttpPost("{id:guid}/ack")]
    public async Task<IActionResult> Acknowledge(Guid id, CancellationToken ct)
    {
        await _commands.AcknowledgeAsync(CurrentDevice.Id, id, ct);

        return Ok();
    }
}
