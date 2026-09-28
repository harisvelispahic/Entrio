using IoT.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace IoT.API.Configuration;

/// <summary>
/// Applies pending EF migrations before the app starts listening.
///
/// Ordering matters: this runs before <c>app.Run()</c>, so by the time Kestrel accepts
/// its first connection the schema is migrated and seeded. That is what makes the compose
/// TCP port-probe healthcheck meaningful — an open port also proves the database is ready.
/// </summary>
public static class DatabaseStartup
{
    private const int MaxAttempts = 12;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Migrates, retrying while SQL Server is still cold-booting. A fresh SQL Server
    /// container takes 20-60s before it accepts logins, so early attempts are expected
    /// to fail.
    /// </summary>
    public static async Task MigrateAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                await db.Database.MigrateAsync();
                logger.LogInformation("Database migrations applied.");
                return;
            }
            catch (Exception ex) when (attempt < MaxAttempts)
            {
                logger.LogWarning(
                    "Database not ready (attempt {Attempt}/{MaxAttempts}): {Message}. Retrying in {Delay}s.",
                    attempt, MaxAttempts, ex.Message, RetryDelay.TotalSeconds);

                await Task.Delay(RetryDelay);
            }
        }

        // Final attempt outside the catch so a genuine failure surfaces its real stack trace.
        await db.Database.MigrateAsync();
    }
}
