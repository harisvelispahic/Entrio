using IoT.Application.Devices;
using IoT.Application.Schedules;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/auto-close")]
[Authorize]
public class AutoCloseController : ControllerBase
{
    private readonly IAutoCloseService _autoClose;
    private readonly IDeviceService _devices;

    public AutoCloseController(IAutoCloseService autoClose, IDeviceService devices)
    {
        _autoClose = autoClose;
        _devices = devices;
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var device = await _devices.GetAsync(ct);
        var settings = await _autoClose.GetSettingsAsync(device.Id, ct);

        return Ok(new { enabled = settings.Enabled, afterSeconds = settings.AfterSeconds });
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] AutoCloseSettingsRequest request, CancellationToken ct)
    {
        var device = await _devices.GetAsync(ct);

        var settings = await _autoClose.UpdateSettingsAsync(device.Id, request, ct);

        return Ok(new { enabled = settings.Enabled, afterSeconds = settings.AfterSeconds });
    }
}
