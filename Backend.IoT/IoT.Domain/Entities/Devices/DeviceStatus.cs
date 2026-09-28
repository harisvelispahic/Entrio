namespace IoT.Domain.Entities.Devices;

/// <summary>
/// The door's last reported physical state, one row per device. Written only from what the
/// controller reports, never inferred from a command: a queued command is an intention,
/// this is what actually happened.
/// </summary>
public class DeviceStatus
{
    public Guid DeviceId { get; set; }

    public DoorState DoorState { get; set; }
    public int PositionPercent { get; set; }
    public bool ObstacleDetected { get; set; }

    /// <summary>When the door last reached a non-closed position. Null while closed.</summary>
    public DateTime? OpenedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
