using IoT.Application.Common;
using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace IoT.Infrastructure.Background;

/// <summary>
/// Turns due schedules into device commands. The ESP32 polls for commands, so this is the
/// only thing that makes a scheduled action actually happen.
/// </summary>
public class ScheduleWorker : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);

    private readonly IServiceProvider _provider;
    private readonly ILogger<ScheduleWorker> _logger;

    public ScheduleWorker(IServiceProvider provider, ILogger<ScheduleWorker> logger)
    {
        _provider = provider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // An exception escaping ExecuteAsync would stop the entire host, because
            // .NET's default BackgroundServiceExceptionBehavior is StopHost. A transient
            // SQL blip must not take the API down with it, so every tick is isolated.
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break; // normal shutdown
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Schedule tick failed; retrying in {Seconds}s.", PollInterval.TotalSeconds);
            }

            try
            {
                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        using var scope = _provider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<IAppDbContext>();

        var nowUtc = DateTime.UtcNow;

        var due = await db.Schedules
            .Where(s => s.IsActive && !s.WasTriggered && s.ExecuteAtUtc <= nowUtc)
            .ToListAsync(ct);

        if (due.Count == 0)
            return;

        foreach (var schedule in due)
        {
            db.DeviceCommands.Add(new DeviceCommandEntity(
                schedule.DeviceId,
                schedule.CommandType,
                schedule.CommandType == DeviceCommandType.Vent ? schedule.TargetPercentage : null,
                // A scheduled action must not re-arm auto-close, or an auto-close would
                // schedule the next one and the door would cycle forever.
                suppressAutoClose: true));

            schedule.MarkTriggered();
        }

        await db.SaveChangesAsync(ct);

        _logger.LogInformation("Triggered {Count} due schedule(s).", due.Count);
    }
}
