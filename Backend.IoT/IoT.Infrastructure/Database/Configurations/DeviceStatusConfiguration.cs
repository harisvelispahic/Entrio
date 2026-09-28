using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IoT.Infrastructure.Database.Configurations;

public class DeviceStatusConfiguration : IEntityTypeConfiguration<DeviceStatus>
{
    public void Configure(EntityTypeBuilder<DeviceStatus> builder)
    {
        builder.ToTable("DeviceStatuses");

        builder.HasKey(x => x.DeviceId);

        builder.Property(x => x.DoorState)
            .IsRequired()
            .HasMaxLength(30);
    }
}
