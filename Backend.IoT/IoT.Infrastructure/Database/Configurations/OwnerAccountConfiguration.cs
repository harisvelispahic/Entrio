using IoT.Domain.Entities.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IoT.Infrastructure.Database.Configurations;

public class OwnerAccountConfiguration : IEntityTypeConfiguration<OwnerAccountEntity>
{
    public void Configure(EntityTypeBuilder<OwnerAccountEntity> builder)
    {
        builder.ToTable("OwnerAccounts");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Email).IsRequired().HasMaxLength(256);
        builder.Property(x => x.PasswordHash).IsRequired().HasMaxLength(200);
        builder.Property(x => x.PasswordSalt).IsRequired().HasMaxLength(100);

        builder.HasIndex(x => x.Email).IsUnique();
    }
}
