using IoT.Application.Identity;
using IoT.Domain.Entities.Devices;
using IoT.Domain.Entities.Identity;
using IoT.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace IoT.API.Configuration;

/// <summary>
/// Brings the database to a usable state before the app starts listening.
///
/// Ordering matters: this runs BEFORE <c>app.Run()</c>, so by the time Kestrel
/// accepts its first connection the schema is migrated and the required rows exist.
/// That is what makes the compose TCP port-probe healthcheck meaningful — an open
/// port also proves the database is reachable and seeded.
/// </summary>
public static class DatabaseStartup
{
    /// <summary>
    /// The device identity the ESP32 firmware hardcodes in its status payload
    /// (see Esp32.IoT/Esp32.IoT.ino, sendStatusUpdate). DeviceStatusController
    /// rejects any deviceId it does not recognise, so the seeded row must use
    /// this exact GUID or the firmware's status posts would fail with
    /// "Unknown device.".
    /// </summary>
    private static readonly Guid FirmwareDeviceId = new("0f8fad5b-d9cb-469f-a165-70867728950e");

    private const int MaxAttempts = 12;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);

    /// <summary>
    /// Applies pending EF migrations, retrying while SQL Server is still cold-booting.
    /// A fresh SQL Server container takes 20-60s before it accepts logins, so the
    /// first few attempts are expected to fail.
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

        // Final attempt outside the catch so a genuine failure surfaces with its real stack trace.
        await db.Database.MigrateAsync();
    }

    /// <summary>
    /// Creates the two rows the system cannot function without, if they are missing:
    /// the owner account used to log in, and the single device row the ESP32
    /// authenticates against via its <c>X-Device-Key</c> header.
    ///
    /// Idempotent: existing rows are left untouched, so restarting never clobbers
    /// data and an existing database is unaffected. Both credentials come from the
    /// environment (<c>OWNER_PIN</c>, <c>DEVICE_KEY</c>) and are stored only as
    /// salted hashes — no plaintext credential is ever written to the database,
    /// committed to source, or logged.
    /// </summary>
    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPinHasher>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var ownerEmail = config["Seed:OwnerEmail"];
        var ownerPin = config["Seed:OwnerPin"];
        var deviceKey = config["Seed:DeviceKey"];

        if (!await db.OwnerAccounts.AnyAsync())
        {
            if (string.IsNullOrWhiteSpace(ownerEmail) || string.IsNullOrWhiteSpace(ownerPin))
            {
                logger.LogWarning(
                    "No owner account exists and OWNER_EMAIL/OWNER_PIN are not set. " +
                    "Login will fail until an owner is seeded.");
            }
            else
            {
                var (pinHash, pinSalt) = hasher.Hash(ownerPin);
                db.OwnerAccounts.Add(new OwnerAccountEntity(ownerEmail, pinHash, pinSalt));
                await db.SaveChangesAsync();

                logger.LogInformation("Seeded owner account {Email}.", ownerEmail);
            }
        }

        if (!await db.Devices.AnyAsync())
        {
            if (string.IsNullOrWhiteSpace(deviceKey))
            {
                logger.LogWarning(
                    "No device exists and DEVICE_KEY is not set. " +
                    "Device endpoints will reject every request until a device is seeded.");
            }
            else
            {
                var (keyHash, keySalt) = hasher.Hash(deviceKey);
                db.Devices.Add(new DeviceEntity("Garage Door", keyHash, keySalt, FirmwareDeviceId));
                await db.SaveChangesAsync();

                // A status row so the dashboard renders a closed door immediately
                // rather than waiting for the device's first report.
                db.DeviceStatuses.Add(new DeviceStatusEntity(FirmwareDeviceId));
                await db.SaveChangesAsync();

                logger.LogInformation(
                    "Seeded device {DeviceId} for the ESP32 controller.", FirmwareDeviceId);
            }
        }
    }
}
