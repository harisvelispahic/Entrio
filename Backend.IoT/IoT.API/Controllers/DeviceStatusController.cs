using IoT.API.Security;
using IoT.Application.Common;
using IoT.Domain.Entities.Devices;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/device/status")]
public class DeviceStatusController : ControllerBase
{
    private readonly IAppDbContext _db;

    public DeviceStatusController(IAppDbContext db)
    {
        _db = db;
    }

    [DeviceAuthorize]
    [HttpPost]
    public async Task<IActionResult> UpdateStatus(
        [FromBody] UpdateStatusRequest request,
        CancellationToken ct)
    {
        var deviceExists = await _db.Devices
            .AnyAsync(d => d.Id == request.DeviceId, ct);

        if (!deviceExists)
            return BadRequest("Unknown device.");

        var status = await _db.DeviceStatuses
            .SingleOrDefaultAsync(s => s.DeviceId == request.DeviceId, ct);

        if (status == null)
        {
            status = new DeviceStatusEntity(request.DeviceId);
            _db.DeviceStatuses.Add(status);
        }

        status.Update(
            request.DoorState,
            request.PositionPercent,
            request.ObstacleDetected
        );

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}

public class UpdateStatusRequest
{
    public Guid DeviceId { get; init; }
    public DoorState DoorState { get; init; }
    public int PositionPercent { get; init; }
    public bool ObstacleDetected { get; init; }
}
