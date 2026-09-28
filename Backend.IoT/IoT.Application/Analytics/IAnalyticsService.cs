namespace IoT.Application.Analytics;

public record DailyOpens(string Day, int Opens);
public record NamedCount(string Name, int Value);

public record AnalyticsResult(
    IReadOnlyList<DailyOpens> OpensPerDay,
    IReadOnlyList<NamedCount> OpenVsClosed,
    IReadOnlyList<NamedCount> EventSources);

public interface IAnalyticsService
{
    /// <summary>Activity over the current week, for the dashboard charts.</summary>
    Task<AnalyticsResult> GetAsync(CancellationToken ct = default);
}
