using IoT.Application.Doors;
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

    [HttpPost("command")]
    public async Task<IActionResult> SendCommand(
        [FromBody] DoorCommandRequest request,
        CancellationToken ct)
    {
        await _doors.SendCommandAsync(request, ct);

        return Accepted();
    }

    [HttpGet("status")]
    public async Task<ActionResult<DoorStatusResult>> GetStatus(CancellationToken ct) =>
        Ok(await _doors.GetStatusAsync(ct));
}
