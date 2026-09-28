using IoT.Application.Common.Exceptions;
using IoT.Application.Devices;
using IoT.Domain.Entities.Devices;

namespace IoT.Application.Doors;

public class DoorService : IDoorService
{
    private readonly IDeviceService _devices;
    private readonly IDeviceStatusService _statuses;
    private readonly IDeviceCommandService _commands;

    public DoorService(
        IDeviceService devices,
        IDeviceStatusService statuses,
        IDeviceCommandService commands)
    {
        _devices = devices;
        _statuses = statuses;
        _commands = commands;
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

    public async Task SendCommandAsync(
        DeviceCommandType command,
        int? percentage,
        CancellationToken ct = default)
    {
        if (command == DeviceCommandType.Vent && percentage is null or < 1 or > 99)
            throw new BusinessRuleException("Vent requires a percentage between 1 and 99.");

        var device = await _devices.GetAsync(ct);

        await _commands.QueueAsync(device.Id, command, percentage, suppressAutoClose: false, ct);
    }
}
