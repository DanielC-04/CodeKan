using DevBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevBoard.Infrastructure.Persistence.Configurations;

internal sealed class GitHubInstallationNonceConfiguration : IEntityTypeConfiguration<GitHubInstallationNonce>
{
    public void Configure(EntityTypeBuilder<GitHubInstallationNonce> builder)
    {
        builder.ToTable("GitHubInstallationNonces");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.ProjectId)
            .IsRequired();

        builder.Property(item => item.UserId)
            .IsRequired();

        builder.Property(item => item.Nonce)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(item => item.ExpiresAt)
            .IsRequired();

        builder.Property(item => item.ConsumedAt);

        builder.HasIndex(item => item.Nonce)
            .IsUnique();

        builder.HasIndex(item => item.ExpiresAt);
    }
}
