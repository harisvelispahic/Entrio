using IoT.Domain.Entities.Devices;

namespace IoT.Application.Schedules;

public interface IScheduleService
{
    /// <summary>Upcoming schedules: active, not yet triggered, soonest first.</summary>
    Task<IReadOnlyList<Schedule>> GetUpcomingAsync(Guid deviceId, CancellationToken ct = default);

    Task<Schedule> CreateAsync(
        Guid deviceId,
        CreateScheduleRequest request,
        CancellationToken ct = default);

    Task DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Raises commands for every schedule that has come due. Called by ScheduleWorker.
    /// Returns how many fired, so the worker can log something meaningful.
    /// </summary>
    Task<int> TriggerDueAsync(CancellationToken ct = default);
}
