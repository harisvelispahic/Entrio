namespace IoT.Application.Devices;

/// <summary>
/// A report from the controller. Matches the body the firmware builds in
/// sendDeviceEvent: enum NAMES as strings, not numbers, and Source omitted for
/// system-raised events.
/// </summary>
public record DeviceEventRequest
{
    public string Type { get; init; } = string.Empty;

    public string? Source { get; init; }
}

/// <summary>
/// A status report from the controller. The device is resolved from its X-Device-Key,
/// never from the body, so there is no deviceId here. The firmware still sends one and
/// it is ignored.
/// </summary>
public record DeviceStatusRequest
{
    public Domain.Entities.Devices.DoorState DoorState { get; init; }

    public int PositionPercent { get; init; }

    public bool ObstacleDetected { get; init; }
}
