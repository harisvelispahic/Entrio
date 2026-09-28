using IoT.Domain.Entities.Devices;

namespace IoT.Application.Doors;

/// <summary>
/// A manual door command from the web UI.
///
/// Lives in the Application layer rather than on the controller so its validator has
/// something to attach to, and so the controller can bind and pass it straight through.
/// </summary>
public record DoorCommandRequest
{
    public DeviceCommandType Command { get; init; }

    /// <summary>Target opening for Vent, 1-99. Ignored for Open, Close and Stop.</summary>
    public int? Percentage { get; init; }
}
