using IoT.Domain.Entities.Devices;

namespace IoT.Application.Schedules;

/// <summary>
/// A scheduled period during which the door is open.
///
/// Scheduling an open without a close was how you left the door open overnight by
/// accident, so a closing time is mandatory. Only Open and Vent can be scheduled:
/// a bare Close has no meaning as a user action (auto-close covers the safety case),
/// and Stop is meaningless on a door that is not moving.
/// </summary>
public record CreateScheduleRequest
{
    public DeviceCommandType CommandType { get; init; }

    /// <summary>Target opening for Vent, 1-99. Ignored for Open.</summary>
    public int? TargetPercentage { get; init; }

    /// <summary>When the door opens, as a UTC instant.</summary>
    public DateTime OpensAtUtc { get; init; }

    /// <summary>When it closes again. Must be after <see cref="OpensAtUtc"/>.</summary>
    public DateTime ClosesAtUtc { get; init; }
}

public record AutoCloseSettingsRequest
{
    public bool Enabled { get; init; }

    public int AfterSeconds { get; init; }
}

/// <summary>Distinguishes a user-created period from a pending auto-close.</summary>
public enum ScheduleEntryKind
{
    Period = 0,
    AutoClose = 1
}

/// <summary>
/// One row in the schedules list. A user period collapses its two underlying rows into a
/// single entry keyed by the group id; a pending auto-close is reported separately so the
/// UI can show it without offering to delete it.
/// </summary>
public record ScheduleEntry(
    Guid Id,
    ScheduleEntryKind Kind,
    DeviceCommandType CommandType,
    int? TargetPercentage,
    DateTime? OpensAtUtc,
    DateTime ClosesAtUtc);
