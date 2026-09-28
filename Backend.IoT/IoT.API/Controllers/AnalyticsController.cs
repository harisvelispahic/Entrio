using IoT.Application.Common;
using IoT.Domain.Entities.Devices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace IoT.API.Controllers;

[ApiController]
[Route("api/analytics")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAppDbContext _db;

    public AnalyticsController(IAppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAnalytics(CancellationToken ct)
    {
        // last 7 days events
        var since = DateTime.UtcNow.AddDays(-7);

        var recentEvents = await _db.DeviceEvents
            .Where(e => e.OccurredAtUtc >= since)
            .ToListAsync(ct);

        // ============================
        // 1) Opens per day
        // ============================
        var today = DateTime.UtcNow.Date;

        // shift so Monday is start
        int offset = ((int)today.DayOfWeek + 6) % 7;

        var weekDays = Enumerable.Range(0, 7)
            .Select(i => today.AddDays(-offset + i))
            .ToList();

        var opensPerDay = weekDays
            .GroupJoin(
                recentEvents.Where(e => e.EventType == DeviceEventType.DoorOpened),
                d => d,
                e => e.OccurredAtUtc.Date,
                (d, events) => new
                {
                    day = d.ToString("ddd", CultureInfo.InvariantCulture),
                    opens = events.Count()
                }
            )
            .ToList();


        // ============================
        // 2) Open vs Closed
        // ============================
        var openCount = recentEvents.Count(e => e.EventType == DeviceEventType.DoorOpened);
        var closeCount = recentEvents.Count(e => e.EventType == DeviceEventType.DoorClosed);

        var openVsClosed = new[]
        {
            new { name = "Open", value = openCount },
            new { name = "Closed", value = closeCount }
        };

        // ============================
        // 3) Event sources
        // ============================
        var eventSources = recentEvents
            .GroupBy(e => e.Source.ToString())
            .Select(g => new
            {
                name = g.Key,
                value = g.Count()
            })
            .ToList();

        return Ok(new
        {
            opensPerDay,
            openVsClosed,
            eventSources
        });
    }
}
