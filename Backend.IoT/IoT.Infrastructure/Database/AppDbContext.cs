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
    public DbSet<OwnerAccountEntity> OwnerAccounts => Set<OwnerAccountEntity>();
    public DbSet<RefreshTokenEntity> RefreshTokens => Set<RefreshTokenEntity>();

    // Devices
    public DbSet<DeviceEntity> Devices => Set<DeviceEntity>();
    public DbSet<DeviceStatusEntity> DeviceStatuses => Set<DeviceStatusEntity>();
    public DbSet<DeviceCommandEntity> DeviceCommands => Set<DeviceCommandEntity>();
    public DbSet<DeviceEventEntity> DeviceEvents => Set<DeviceEventEntity>();
    public DbSet<ScheduleEntity> Schedules => Set<ScheduleEntity>();
    public DbSet<AutoCloseSettingsEntity> AutoCloseSettings => Set<AutoCloseSettingsEntity>();



    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        modelBuilder.Entity<AutoCloseSettingsEntity>()
            .HasOne<DeviceEntity>()
            .WithOne()
            .HasForeignKey<AutoCloseSettingsEntity>(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);


    }
}
