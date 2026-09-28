using IoT.Application.Common;
using IoT.Domain.Entities.Devices;
using IoT.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace IoT.Infrastructure.Database;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    // Identity
    public DbSet<OwnerAccount> OwnerAccounts => Set<OwnerAccount>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Devices
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<DeviceStatus> DeviceStatuses => Set<DeviceStatus>();
    public DbSet<DeviceCommand> DeviceCommands => Set<DeviceCommand>();
    public DbSet<DeviceEvent> DeviceEvents => Set<DeviceEvent>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<AutoCloseSettings> AutoCloseSettings => Set<AutoCloseSettings>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Every entity is configured by an IEntityTypeConfiguration in Configurations/.
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}
