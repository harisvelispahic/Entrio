using IoT.Domain.Entities.Devices;

namespace IoT.Application.Doors;

/// <summary>Shape the dashboard polls. Flattens device and status into one payload.</summary>
public record DoorStatusResult(
    int Position,
    DoorState State,
    bool Obstacle,
    DateTime LastUpdated,
    DateTime LastSeenAtUtc);

public interface IDoorService
{
    Task<DoorStatusResult> GetStatusAsync(CancellationToken ct = default);

    /// <summary>Queues a door command for the controller to collect on its next poll.</summary>
    Task SendCommandAsync(DeviceCommandType command, int? percentage, CancellationToken ct = default);
}
