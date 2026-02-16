using DevBoard.Domain.Exceptions;
using TaskEntity = DevBoard.Domain.Entities.Task;
using TaskStatus = DevBoard.Domain.Enums.TaskStatus;

namespace DevBoard.Domain.Tests;

public sealed class TaskTests
{
    [Fact]
    public void MoveToDone_SetsCompletedAt()
    {
        var task = new TaskEntity(Guid.NewGuid(), "Implement webhook endpoint");
        var completedAt = new DateTime(2026, 2, 8, 15, 30, 0, DateTimeKind.Utc);

        task.MoveTo(TaskStatus.Done, completedAt);

        Assert.Equal(TaskStatus.Done, task.Status);
        Assert.Equal(completedAt, task.CompletedAt);
    }

    [Fact]
    public void MoveFromDoneToInProgress_ClearsCompletedAt()
    {
        var task = new TaskEntity(Guid.NewGuid(), "Close issue on done");
        task.MoveTo(TaskStatus.Done);
        Assert.NotNull(task.CompletedAt);

        task.MoveTo(TaskStatus.InProgress);

        Assert.Equal(TaskStatus.InProgress, task.Status);
        Assert.Null(task.CompletedAt);
    }

    [Fact]
    public void SetGitHubIssueNumber_WithInvalidValue_ThrowsDomainException()
    {
        var task = new TaskEntity(Guid.NewGuid(), "Integrate Octokit");

        var action = () => task.SetGitHubIssueNumber(0);

        var exception = Assert.Throws<DomainException>(action);
        Assert.Equal("gitHubIssueNumber must be greater than zero.", exception.Message);
    }
}
