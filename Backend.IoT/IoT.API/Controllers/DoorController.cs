using IoT.Application.Common;
using IoT.Application.Common.Exceptions;
using IoT.Application.Devices.Commands.Create;
using IoT.Domain.Entities.Devices;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/door")]
[Authorize]
public class DoorController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IAppDbContext _db;

    public DoorController(
        IMediator mediator,
        IAppDbContext db)
    {
        _mediator = mediator;
        _db = db;
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

        if (request.Command == DeviceCommandType.Vent && request.Percentage is null or < 1 or > 99)
            throw new BusinessRuleException("Vent requires a percentage between 1 and 99.");

        // Single-device system: there is exactly one garage door.
        var device = await _db.Devices.AsNoTracking().FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("No device is registered in the system.");

        await _mediator.Send(
            new CreateDeviceCommandCommand(
                device.Id,          // ← GUID
                request.Command,
                request.Percentage),
            ct);

        return Accepted();
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus(CancellationToken ct)
    {
        // FirstAsync used to throw here when no device existed, surfacing as a raw 500.
        var device = await _db.Devices.FirstOrDefaultAsync(ct)
            ?? throw new NotFoundException("No device is registered in the system.");

        var status = await _db.DeviceStatuses
            .SingleOrDefaultAsync(s => s.DeviceId == device.Id, ct);

        // The device may not have reported yet; a closed door is the safe default.
        if (status is null)
        {
            status = new DeviceStatusEntity(device.Id);
            _db.DeviceStatuses.Add(status);
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new
        {
            position = status.PositionPercent,
            state = status.DoorState,
            obstacle = status.ObstacleDetected,
            lastUpdated = status.UpdatedAtUtc
        });

    }


}
