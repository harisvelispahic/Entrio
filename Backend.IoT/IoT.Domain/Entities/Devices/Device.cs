namespace IoT.Domain.Entities.Devices;

/// <summary>
/// The garage door controller. This is a single-device system: exactly one row exists,
/// seeded with the GUID the ESP32 firmware hardcodes in its status payload.
/// </summary>
public class Device
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;

    /// <summary>PBKDF2 hash of the shared secret the device sends as X-Device-Key.</summary>
    public string DeviceKeyHash { get; set; } = null!;
    public string DeviceKeySalt { get; set; } = null!;

    /// <summary>
    /// Bumped on every authenticated device call. This is the only evidence the system has
    /// that the controller is alive, so the UI derives connectivity from how stale it is.
    /// </summary>
    public DateTime LastSeenAtUtc { get; set; }

    /// <summary>
    /// What last reported in. Recorded on every authenticated device call, alongside
    /// <see cref="LastSeenAtUtc"/>, so the dashboard can distinguish real hardware from
    /// the simulator rather than asserting one or the other.
    /// </summary>
    public DeviceClientKind LastClientKind { get; set; }

    public DeviceStatus Status { get; set; } = null!;
    public ICollection<DeviceCommand> Commands { get; set; } = new List<DeviceCommand>();
    public ICollection<DeviceEvent> Events { get; set; } = new List<DeviceEvent>();
    public ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
}
