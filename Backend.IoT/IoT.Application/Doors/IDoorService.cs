using IoT.Domain.Entities.Devices;

namespace IoT.Application.Doors;

/// <summary>
/// Shape the dashboard polls. Flattens device and status into one payload so the header,
/// sidebar and door card can all be driven from a single request.
/// </summary>
public record DoorStatusResult(
    int Position,
    DoorState State,
    bool Obstacle,
    DateTime LastUpdated,
    /// <summary>When the controller last authenticated. The client decides what counts as stale.</summary>
    DateTime LastSeenAtUtc,
    /// <summary>What last reported in, so the UI need not guess between hardware and the simulator.</summary>
    DeviceClientKind LastClientKind);

public interface IDoorService
{
    Task<DoorStatusResult> GetStatusAsync(CancellationToken ct = default);

    /// <summary>Queues a door command for the controller to collect on its next poll.</summary>
    Task SendCommandAsync(DoorCommandRequest request, CancellationToken ct = default);
}
