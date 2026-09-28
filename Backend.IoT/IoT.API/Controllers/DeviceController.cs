using IoT.API.Security;
using Microsoft.AspNetCore.Mvc;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/device")]
public class DeviceController : ControllerBase
{
    /// <summary>Connectivity check for the controller. The attribute does the work.</summary>
    [DeviceAuthorize]
    [HttpPost("ping")]
    public IActionResult Ping() => Ok("DEVICE AUTH OK");
}
