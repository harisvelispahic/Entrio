using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IoT.Infrastructure.Database.Configurations;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Devices");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(x => x.Status)
            .WithOne()
            .HasForeignKey<DeviceStatus>(x => x.DeviceId);

        builder.HasMany(x => x.Commands)
            .WithOne()
            .HasForeignKey(x => x.DeviceId);

        builder.HasMany(x => x.Events)
            .WithOne()
            .HasForeignKey(x => x.DeviceId);

        builder.HasMany(x => x.Schedules)
            .WithOne()
            .HasForeignKey(x => x.DeviceId);

    }
}
