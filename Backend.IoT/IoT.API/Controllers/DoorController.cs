using IoT.Application.Doors;
using IoT.Domain.Entities.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/door")]
[Authorize]
public class DoorController : ControllerBase
{
    private readonly IDoorService _doors;

    public DoorController(IDoorService doors)
    {
        _doors = doors;
    }

    public sealed class DoorCommandRequest
    {
        public DeviceCommandType Command { get; init; }
        public int? Percentage { get; init; }
    }

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand(
        [FromBody] DoorCommandRequest request,
        CancellationToken ct)
    {
        await _doors.SendCommandAsync(request.Command, request.Percentage, ct);

        return Accepted();
    }

    [HttpGet("status")]
    public async Task<ActionResult<DoorStatusResult>> GetStatus(CancellationToken ct) =>
        Ok(await _doors.GetStatusAsync(ct));
}
