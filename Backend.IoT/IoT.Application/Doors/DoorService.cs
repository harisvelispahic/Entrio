using FluentValidation;
using IoT.Application.Devices;

namespace IoT.Application.Doors;

public class DoorService : IDoorService
{
    private readonly IDeviceService _devices;
    private readonly IDeviceStatusService _statuses;
    private readonly IDeviceCommandService _commands;
    private readonly IValidator<DoorCommandRequest> _commandValidator;

    public DoorService(
        IDeviceService devices,
        IDeviceStatusService statuses,
        IDeviceCommandService commands,
        IValidator<DoorCommandRequest> commandValidator)
    {
        _devices = devices;
        _statuses = statuses;
        _commands = commands;
        _commandValidator = commandValidator;
    }

    public async Task<DoorStatusResult> GetStatusAsync(CancellationToken ct = default)
    {
        var device = await _devices.GetAsync(ct);
        var status = await _statuses.GetOrCreateAsync(device.Id, ct);

        return new DoorStatusResult(
            status.PositionPercent,
            status.DoorState,
            status.ObstacleDetected,
            status.UpdatedAtUtc,
            device.LastSeenAtUtc);
    }

    public async Task SendCommandAsync(DoorCommandRequest request, CancellationToken ct = default)
    {
        // ValidateAndThrowAsync raises a ValidationException, which ValidationExceptionHandler
        // turns into a 400 keyed by property name.
        await _commandValidator.ValidateAndThrowAsync(request, ct);

        var device = await _devices.GetAsync(ct);

        await _commands.QueueAsync(
            device.Id, request.Command, request.Percentage, suppressAutoClose: false, ct);
    }
}
