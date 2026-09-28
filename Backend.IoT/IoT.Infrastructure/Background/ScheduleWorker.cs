using IoT.Application.Schedules;
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
                using var scope = _provider.CreateScope();
                var schedules = scope.ServiceProvider.GetRequiredService<IScheduleService>();

                var triggered = await schedules.TriggerDueAsync(stoppingToken);

                if (triggered > 0)
                    _logger.LogInformation("Triggered {Count} due schedule(s).", triggered);
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
}
