namespace IoT.Domain.Entities.Devices;

/// <summary>
/// Something the controller reported happening, as opposed to something that was asked of
/// it. Raised when travel FINISHES, not when a command is acknowledged, which is what the
/// auto-close delay is measured from.
/// </summary>
public class DeviceEvent
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }

    public DeviceEventType EventType { get; set; }
    public DeviceEventSource Source { get; set; }
    public string? Details { get; set; }

    public DateTime OccurredAtUtc { get; set; }
}
