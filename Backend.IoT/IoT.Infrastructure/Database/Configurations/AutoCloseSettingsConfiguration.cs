using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IoT.Infrastructure.Database.Configurations;

public class AutoCloseSettingsConfiguration : IEntityTypeConfiguration<AutoCloseSettings>
{
    public void Configure(EntityTypeBuilder<AutoCloseSettings> builder)
    {
        // Pinned explicitly, like every other entity. This was previously the only one
        // relying on the DbSet property name, and was configured inline in OnModelCreating.
        builder.ToTable("AutoCloseSettings");

        builder.HasKey(x => x.Id);

        builder.HasOne<Device>()
            .WithOne()
            .HasForeignKey<AutoCloseSettings>(x => x.DeviceId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
