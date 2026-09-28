namespace IoT.Application.Schedules;

public interface IScheduleService
{
    /// <summary>
    /// Upcoming entries, soonest first. User periods are collapsed to one entry per pair;
    /// a pending auto-close appears as its own entry.
    /// </summary>
    Task<IReadOnlyList<ScheduleEntry>> GetUpcomingAsync(Guid deviceId, CancellationToken ct = default);

    /// <summary>
    /// Creates a scheduled open period as two linked rows sharing a group id: the
    /// Open or Vent, and the Close that follows it.
    /// </summary>
    Task<ScheduleEntry> CreateAsync(
        Guid deviceId,
        CreateScheduleRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Deletes a user-created period by its group id, removing both halves. A pending
    /// auto-close is not deletable this way — it is owned by the auto-close setting.
    /// </summary>
    Task DeleteAsync(Guid groupId, CancellationToken ct = default);

    /// <summary>
    /// Raises commands for every schedule that has come due. Called by ScheduleWorker.
    /// Returns how many fired, so the worker can log something meaningful.
    /// </summary>
    Task<int> TriggerDueAsync(CancellationToken ct = default);
}
