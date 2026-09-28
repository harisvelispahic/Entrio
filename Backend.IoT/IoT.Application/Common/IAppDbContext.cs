using IoT.Domain.Entities.Devices;
using IoT.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;

namespace IoT.Application.Common;

public interface IAppDbContext
{
    // Identity
    DbSet<OwnerAccount> OwnerAccounts { get; }
    DbSet<RefreshToken> RefreshTokens { get; }

    // Devices
    DbSet<Device> Devices { get; }
    DbSet<DeviceStatus> DeviceStatuses { get; }
    DbSet<DeviceCommand> DeviceCommands { get; }
    DbSet<DeviceEvent> DeviceEvents { get; }
    DbSet<Schedule> Schedules { get; }
    DbSet<AutoCloseSettings> AutoCloseSettings { get; }


    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
