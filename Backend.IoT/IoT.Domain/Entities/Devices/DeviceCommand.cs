namespace IoT.Domain.Entities.Devices;

/// <summary>
/// An instruction queued for the controller to collect. The device polls for the oldest
/// pending command, acts on it, and acknowledges it; nothing is ever pushed to the device.
/// </summary>
public class DeviceCommand
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }

    public DeviceCommandType CommandType { get; set; }

    /// <summary>Target opening for a Vent command. Null for Open, Close and Stop.</summary>
    public int? TargetPercentage { get; set; }

    public DeviceCommandStatus Status { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? AcknowledgedAtUtc { get; set; }

    /// <summary>
    /// Set on commands raised by a schedule. Without it an auto-close would itself be
    /// treated as a fresh open and arm the next one, cycling the door forever.
    /// </summary>
    public bool SuppressAutoClose { get; set; }
}
