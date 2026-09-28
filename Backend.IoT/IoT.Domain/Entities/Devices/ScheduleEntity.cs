namespace IoT.Domain.Entities.Devices;

public class ScheduleEntity
{
    public Guid Id { get; private set; }
    public Guid DeviceId { get; private set; }

    public DeviceCommandType CommandType { get; private set; }
    public int? TargetPercentage { get; private set; }

    public DateTime ExecuteAtUtc { get; private set; }

    public bool IsActive { get; private set; } = true;

    public bool WasTriggered { get; private set; } = false;

    private ScheduleEntity() { }

    public ScheduleEntity(
        Guid deviceId,
        DeviceCommandType commandType,
        int? targetPercentage,
        DateTime executeAtUtc)
    {
        Id = Guid.NewGuid();
        DeviceId = deviceId;
        CommandType = commandType;
        TargetPercentage = targetPercentage;
        ExecuteAtUtc = executeAtUtc;
        IsActive = true;
        WasTriggered = false;
    }

    public void MarkTriggered()
    {
        WasTriggered = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }
}
