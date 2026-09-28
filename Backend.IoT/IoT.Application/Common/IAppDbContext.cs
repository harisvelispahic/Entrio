using IoT.Domain.Entities.Devices;
using IoT.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Common;

public interface IAppDbContext
{
    // Identity
    DbSet<OwnerAccountEntity> OwnerAccounts { get; }
    DbSet<RefreshTokenEntity> RefreshTokens { get; }

    // Devices
    DbSet<DeviceEntity> Devices { get; }
    DbSet<DeviceStatusEntity> DeviceStatuses { get; }
    DbSet<DeviceCommandEntity> DeviceCommands { get; }
    DbSet<DeviceEventEntity> DeviceEvents { get; }
    DbSet<ScheduleEntity> Schedules { get; }
    DbSet<AutoCloseSettingsEntity> AutoCloseSettings { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
