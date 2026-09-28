using IoT.Application.Common;
using IoT.Application.Common.Exceptions;
using IoT.Domain.Entities.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IoT.API.Controllers;

/// <summary>
/// Reads and updates the auto-close setting for the single device.
///
/// Previously no code path ever created an AutoCloseSettings row, so AutoCloseService
/// always returned early and the feature could never fire. The seeder now creates a
/// disabled row and this endpoint is how it gets turned on.
/// </summary>
[ApiController]
[Route("api/auto-close")]
[Authorize]
public class AutoCloseController : ControllerBase
{
    private const int MinSeconds = 5;
    private const int MaxSeconds = 3600;

    private readonly IAppDbContext _db;

    public AutoCloseController(IAppDbContext db)
    {
        _db = db;
    }

    public sealed class AutoCloseSettingsRequest
    {
        public bool Enabled { get; init; }
        public int AfterSeconds { get; init; }
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var settings = await GetSettingsAsync(ct);

        return Ok(new { enabled = settings.Enabled, afterSeconds = settings.AfterSeconds });
    }

    [HttpPut]
    public async Task<IActionResult> Update(
        [FromBody] AutoCloseSettingsRequest request,
        CancellationToken ct)
    {
        if (request.AfterSeconds is < MinSeconds or > MaxSeconds)
            throw new BusinessRuleException($"Auto-close delay must be between {MinSeconds} and {MaxSeconds} seconds.");

        var settings = await GetSettingsAsync(ct);
        settings.Update(request.Enabled, request.AfterSeconds);

        await _db.SaveChangesAsync(ct);

        return Ok(new { enabled = settings.Enabled, afterSeconds = settings.AfterSeconds });
    }

    private async Task<AutoCloseSettingsEntity> GetSettingsAsync(CancellationToken ct)
    {
        var device = await _db.Devices.FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("No device is registered in the system.");

        return await _db.AutoCloseSettings.FirstOrDefaultAsync(x => x.DeviceId == device.Id, ct)
            ?? throw new NotFoundException("Auto-close settings have not been initialised for this device.");
    }
}
