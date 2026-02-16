using DevBoard.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DevBoard.Infrastructure.Persistence.Configurations;

internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        builder.ToTable("Projects");
        builder.HasKey(project => project.Id);

        builder.Property(project => project.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(project => project.RepoOwner)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(project => project.RepoName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(project => project.GitHubTokenEncrypted)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(project => project.CreatedAt)
            .IsRequired();

        builder.HasMany(project => project.Tasks)
            .WithOne(task => task.Project)
            .HasForeignKey(task => task.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
