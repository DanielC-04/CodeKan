using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskStatus = DevBoard.Domain.Enums.TaskStatus;
using TaskEntity = DevBoard.Domain.Entities.Task;

namespace DevBoard.Infrastructure.Persistence.Configurations;

internal sealed class TaskConfiguration : IEntityTypeConfiguration<TaskEntity>
{
    public void Configure(EntityTypeBuilder<TaskEntity> builder)
    {
        builder.ToTable("Tasks");
        builder.HasKey(task => task.Id);

        builder.Property(task => task.ProjectId)
            .IsRequired();

        builder.Property(task => task.Title)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(task => task.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(TaskStatus.Todo)
            .IsRequired();

        builder.Property(task => task.GitHubIssueNumber);

        builder.Property(task => task.CreatedAt)
            .IsRequired();

        builder.Property(task => task.CompletedAt);
    }
}
