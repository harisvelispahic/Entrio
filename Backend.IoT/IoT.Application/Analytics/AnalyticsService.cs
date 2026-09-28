using System.Globalization;
using IoT.Application.Common;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Analytics;

public class AnalyticsService : IAnalyticsService
{
    private const int DaysInWeek = 7;

    private readonly IAppDbContext _db;

    public AnalyticsService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<AnalyticsResult> GetAsync(CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddDays(-DaysInWeek);

        // Materialised once: all three aggregates read the same window, and the set is
        // small enough that a round trip per chart would cost more than it saves.
        var events = await _db.DeviceEvents
            .AsNoTracking()
            .Where(e => e.OccurredAtUtc >= since)
            .ToListAsync(ct);

        return new AnalyticsResult(
            BuildOpensPerDay(events),
            BuildOpenVsClosed(events),
            BuildEventSources(events));
    }

    /// <summary>
    /// Opens for each day of the current week, Monday first, including days with none so
    /// the chart keeps a stable shape.
    /// </summary>
    private static IReadOnlyList<DailyOpens> BuildOpensPerDay(IReadOnlyCollection<DeviceEvent> events)
    {
        var today = DateTime.UtcNow.Date;

        // DayOfWeek starts on Sunday; shift so the week starts on Monday.
        var offsetToMonday = ((int)today.DayOfWeek + 6) % DaysInWeek;
        var monday = today.AddDays(-offsetToMonday);

        var opensByDate = events
            .Where(e => e.EventType == DeviceEventType.DoorOpened)
            .GroupBy(e => e.OccurredAtUtc.Date)
            .ToDictionary(g => g.Key, g => g.Count());

        return Enumerable.Range(0, DaysInWeek)
            .Select(offset => monday.AddDays(offset))
            .Select(date => new DailyOpens(
                date.ToString("ddd", CultureInfo.InvariantCulture),
                opensByDate.GetValueOrDefault(date)))
            .ToList();
    }

    private static IReadOnlyList<NamedCount> BuildOpenVsClosed(IReadOnlyCollection<DeviceEvent> events) =>
    [
        new("Open", events.Count(e => e.EventType == DeviceEventType.DoorOpened)),
        new("Closed", events.Count(e => e.EventType == DeviceEventType.DoorClosed))
    ];

    private static IReadOnlyList<NamedCount> BuildEventSources(IReadOnlyCollection<DeviceEvent> events) =>
        events
            .GroupBy(e => e.Source.ToString())
            .Select(g => new NamedCount(g.Key, g.Count()))
            .ToList();
}
