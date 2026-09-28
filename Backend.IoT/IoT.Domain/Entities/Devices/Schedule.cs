namespace IoT.Domain.Entities.Devices;

/// <summary>
/// A future action. ScheduleWorker turns a due row into a DeviceCommand; the device then
/// picks it up on its next poll like any other command.
/// </summary>
public class Schedule
{
    public Guid Id { get; set; }
    public Guid DeviceId { get; set; }

    public DeviceCommandType CommandType { get; set; }
    public int? TargetPercentage { get; set; }

    public DateTime ExecuteAtUtc { get; set; }

    /// <summary>
    /// Links the two halves of a user-created schedule: the Open or Vent and the Close
    /// that must follow it. Both rows share one id, so the pair is listed as a single
    /// entry and deleting one deletes both.
    ///
    /// Null means the row was raised by the system rather than the user — currently only
    /// auto-close. That distinction is load-bearing: arming auto-close supersedes pending
    /// system rows and must leave user schedules alone.
    /// </summary>
    public Guid? ScheduleGroupId { get; set; }

    /// <summary>False once superseded, e.g. a pending auto-close replaced by a newer open.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Set once the worker has raised the command, so it cannot fire twice.</summary>
    public bool WasTriggered { get; set; }
}
