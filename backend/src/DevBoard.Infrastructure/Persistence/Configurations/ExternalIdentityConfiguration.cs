using DevBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevBoard.Infrastructure.Persistence.Configurations;

internal sealed class ExternalIdentityConfiguration : IEntityTypeConfiguration<ExternalIdentity>
{
    public void Configure(EntityTypeBuilder<ExternalIdentity> builder)
    {
        builder.ToTable("ExternalIdentities");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Provider)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(item => item.ProviderUserId)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.LinkedAt)
            .IsRequired();

        builder.HasIndex(item => new { item.Provider, item.ProviderUserId })
            .IsUnique();

        builder.HasIndex(item => item.UserId);

        builder.HasOne(item => item.User)
            .WithMany(item => item.ExternalIdentities)
            .HasForeignKey(item => item.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
