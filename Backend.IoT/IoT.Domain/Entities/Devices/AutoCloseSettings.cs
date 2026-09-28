namespace IoT.Domain.Entities.Devices;

/// <summary>
/// Whether the door closes itself after being opened, and how long it waits. One row per
/// device, seeded disabled. The delay is measured from the moment the door finishes
/// opening, not from when the command was issued.
/// </summary>
public class AutoCloseSettings
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }

    public bool Enabled { get; set; }

    public int AfterSeconds { get; set; }
}
