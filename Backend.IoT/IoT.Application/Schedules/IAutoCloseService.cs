using IoT.Domain.Entities.Devices;

namespace IoT.Application.Schedules;

public interface IAutoCloseService
{
    Task<AutoCloseSettings> GetSettingsAsync(Guid deviceId, CancellationToken ct = default);

    Task<AutoCloseSettings> UpdateSettingsAsync(
        Guid deviceId,
        bool enabled,
        int afterSeconds,
        CancellationToken ct = default);

    /// <summary>
    /// Queues an automatic close, if the feature is enabled for the device. Implemented as
    /// a schedule rather than a timer so it survives an API restart.
    /// </summary>
    Task ArmAsync(Guid deviceId, CancellationToken ct = default);
}
