using DevBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevBoard.Infrastructure.Persistence.Configurations;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.UserId)
            .IsRequired();

        builder.Property(item => item.TokenHash)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.ExpiresAt)
            .IsRequired();

        builder.Property(item => item.CreatedAt)
            .IsRequired();

        builder.Property(item => item.RevokedAt);

        builder.Property(item => item.CreatedByIp)
            .HasMaxLength(100);

        builder.HasIndex(item => item.TokenHash)
            .IsUnique();
    }
}
