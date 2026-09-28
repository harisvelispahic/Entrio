using IoT.Domain.Entities.Devices;

namespace IoT.Application.Schedules;

public record CreateScheduleRequest
{
    public DeviceCommandType CommandType { get; init; }

    /// <summary>Target opening for Vent, 1-99. Ignored for the other commands.</summary>
    public int? TargetPercentage { get; init; }

    /// <summary>When to execute, as a UTC instant (e.g. 2026-12-25T20:30:00Z).</summary>
    public DateTime ExecuteAtUtc { get; init; }
}

public record AutoCloseSettingsRequest
{
    public bool Enabled { get; init; }

    public int AfterSeconds { get; init; }
}
