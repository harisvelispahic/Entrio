using IoT.Domain.Entities.Devices;

namespace IoT.Application.Devices;

public interface IDeviceCommandService
{
    /// <summary>
    /// Queues a command, superseding anything still pending for the device.
    /// </summary>
    /// <param name="suppressAutoClose">
    /// True for schedule-raised commands, so an auto-close does not arm another one.
    /// </param>
    Task<DeviceCommand> QueueAsync(
        Guid deviceId,
        DeviceCommandType commandType,
        int? targetPercentage,
        bool suppressAutoClose = false,
        CancellationToken ct = default);

    /// <summary>The oldest pending command for the device, or null when there is nothing to do.</summary>
    Task<DeviceCommand?> GetPendingAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>Marks a command acknowledged. Idempotent: a repeat ack is a no-op.</summary>
    Task AcknowledgeAsync(Guid deviceId, Guid commandId, CancellationToken ct = default);
}
