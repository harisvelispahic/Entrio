using IoT.Application.Identity;
using IoT.Domain.Entities.Devices;
using IoT.Domain.Entities.Identity;
using IoT.Infrastructure.Database;
using Microsoft.EntityFrameworkCore;

namespace IoT.API.Configuration;

/// <summary>
/// Creates the rows the system cannot function without, if they are missing.
///
/// WHY THE CREDENTIALS ARE IN SOURCE AND NOT IN .env
/// -------------------------------------------------
/// This is a single-tenant demo system with no registration flow, so the owner account
/// has to come from somewhere. Keeping it here rather than in configuration means anyone
/// who clones the repository can log in immediately, and the credentials are easy to find
/// when you forget them. They are not a secret: there is nothing behind this login but a
/// simulated garage door, and a real deployment would replace this seeder outright.
///
/// The ESP32 device key IS still a secret and still comes from DEVICE_KEY in the
/// repo-root .env, because it must match the value flashed onto the hardware.
///
/// Idempotent: existing rows are never touched, so restarting cannot clobber data.
/// </summary>
public static class DatabaseSeeder
{
    // ---- Demo owner account -------------------------------------------------
    // These are the credentials to log in with. Documented in the README.
    public const string OwnerEmail = "admin@entrio.local";
    public const string OwnerPassword = "Entrio123!";

    /// <summary>
    /// The device identity the ESP32 firmware hardcodes in its status payload
    /// (Esp32.IoT.ino, sendStatusUpdate) and the frontend hardcodes when creating
    /// schedules (scheduleService.ts). DeviceStatusController rejects any deviceId it
    /// does not recognise, so the seeded row must use this exact GUID.
    /// </summary>
    public static readonly Guid FirmwareDeviceId = new("0f8fad5b-d9cb-469f-a165-70867728950e");

    private const int DefaultAutoCloseSeconds = 30;

    public static async Task SeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await SeedOwnerAsync(db, hasher, logger);
        await SeedDeviceAsync(db, hasher, config, logger);
    }

    private static async Task SeedOwnerAsync(AppDbContext db, IPasswordHasher hasher, ILogger logger)
    {
        if (await db.OwnerAccounts.AnyAsync())
            return;

        var (hash, salt) = hasher.Hash(OwnerPassword);

        db.OwnerAccounts.Add(new OwnerAccount
        {
            Id = Guid.NewGuid(),
            Email = OwnerEmail,
            PasswordHash = hash,
            PasswordSalt = salt,
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        logger.LogInformation("Seeded owner account {Email}.", OwnerEmail);
    }

    private static async Task SeedDeviceAsync(
        AppDbContext db,
        IPasswordHasher hasher,
        IConfiguration config,
        ILogger logger)
    {
        if (await db.Devices.AnyAsync())
            return;

        var deviceKey = config["Device:Key"];

        if (string.IsNullOrWhiteSpace(deviceKey))
        {
            logger.LogWarning(
                "No device exists and DEVICE_KEY is not set. Device endpoints will reject " +
                "every request until a device is seeded.");
            return;
        }

        var (keyHash, keySalt) = hasher.Hash(deviceKey);

        db.Devices.Add(new Device
        {
            Id = FirmwareDeviceId,
            Name = "Garage Door",
            DeviceKeyHash = keyHash,
            DeviceKeySalt = keySalt,
            LastSeenAtUtc = DateTime.UtcNow
        });

        // A status row so the dashboard shows a closed door immediately, rather than
        // waiting for the device's first report.
        db.DeviceStatuses.Add(new DeviceStatus
        {
            DeviceId = FirmwareDeviceId,
            DoorState = DoorState.Closed,
            PositionPercent = 0,
            UpdatedAtUtc = DateTime.UtcNow
        });

        // Auto-close settings, disabled by default. Previously no row was ever created,
        // which meant AutoCloseService always returned early and the feature was dead
        // code. The dashboard toggle writes to this row.
        db.AutoCloseSettings.Add(new AutoCloseSettings
        {
            Id = Guid.NewGuid(),
            DeviceId = FirmwareDeviceId,
            Enabled = false,
            AfterSeconds = DefaultAutoCloseSeconds
        });

        await db.SaveChangesAsync();

        logger.LogInformation("Seeded device {DeviceId} for the ESP32 controller.", FirmwareDeviceId);
    }
}
