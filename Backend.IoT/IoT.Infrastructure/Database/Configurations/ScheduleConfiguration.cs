using IoT.Domain.Entities.Devices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IoT.Infrastructure.Database.Configurations;

public class ScheduleConfiguration : IEntityTypeConfiguration<Schedule>
{
    public void Configure(EntityTypeBuilder<Schedule> builder)
    {
        builder.ToTable("Schedules");

        builder.HasKey(x => x.Id);

        // Every listing and delete goes via the group, so this is on the hot path.
        builder.HasIndex(x => x.ScheduleGroupId);

        // The worker polls this combination every 5 seconds.
        builder.HasIndex(x => new { x.DeviceId, x.IsActive, x.WasTriggered });
    }
}
