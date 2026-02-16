using DevBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevBoard.Infrastructure.Persistence.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.PasswordHash)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(item => item.Role)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(item => item.IsActive)
            .IsRequired();

        builder.Property(item => item.CreatedAt)
            .IsRequired();

        builder.HasIndex(item => item.Email)
            .IsUnique();

        builder.HasMany(item => item.RefreshTokens)
            .WithOne(item => item.User)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
