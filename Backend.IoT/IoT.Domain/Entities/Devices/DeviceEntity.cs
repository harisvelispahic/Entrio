using IoT.Domain.Entities.Devices;

namespace IoT.Domain.Entities.Devices;

public class DeviceEntity
{
    public Guid Id { get; private set; }
    public string Name { get; private set; } = null!;
    public string DeviceKeyHash { get; private set; } = null!;
    public string DeviceKeySalt { get; private set; } = null!;

    public DateTime LastSeenAtUtc { get; private set; }



    // Navigation
    public DeviceStatusEntity Status { get; private set; } = null!;
    public ICollection<DeviceCommandEntity> Commands { get; private set; } = new List<DeviceCommandEntity>();
    public ICollection<DeviceEventEntity> Events { get; private set; } = new List<DeviceEventEntity>();
    public ICollection<ScheduleEntity> Schedules { get; private set; } = new List<ScheduleEntity>();

    private DeviceEntity() { }

    /// <param name="id">
    /// Optional fixed identity. Seeding passes an explicit id so the device row
    /// matches the GUID the ESP32 firmware posts in its status payload.
    /// </param>
    public DeviceEntity(string name, string deviceKeyHash, string deviceKeySalt, Guid? id = null)
    {
        Id = id ?? Guid.NewGuid();
        Name = name;
        DeviceKeyHash = deviceKeyHash;
        DeviceKeySalt = deviceKeySalt;
        LastSeenAtUtc = DateTime.UtcNow;
    }

    public void UpdateLastSeen()
    {
        LastSeenAtUtc = DateTime.UtcNow;
    }
}