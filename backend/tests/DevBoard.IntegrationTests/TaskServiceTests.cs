using DevBoard.Application.Common.Exceptions;
using DevBoard.Application.Common.Interfaces;
using DevBoard.Application.Tasks.Dtos;
using DevBoard.Application.Tasks.Services;
using DevBoard.Infrastructure.Persistence;
using DevBoard.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using TaskStatus = DevBoard.Domain.Enums.TaskStatus;
using TaskEntity = DevBoard.Domain.Entities.Task;

namespace DevBoard.IntegrationTests;

public sealed class TaskServiceTests
{
    [Fact]
    public async Task CreateAsync_WhenGitHubSucceeds_PersistsTaskWithIssueNumber()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateContext(dbName);
        var project = SeedProject(dbContext);

        var tokenProtector = new Mock<ITokenProtector>();
        tokenProtector.Setup(item => item.Unprotect("encrypted-token")).Returns("plain-token");

        var gitHubIssueService = new Mock<IGitHubIssueService>();
        gitHubIssueService
            .Setup(item => item.CreateIssueAsync(project.RepoOwner, project.RepoName, "New task", null, "plain-token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(123);

        var service = new TaskService(dbContext, tokenProtector.Object, gitHubIssueService.Object, Mock.Of<ILogger<TaskService>>());

        var result = await service.CreateAsync(project.Id, new CreateTaskRequest("New task"));

        Assert.Equal("New task", result.Title);
        Assert.Equal(123, result.GitHubIssueNumber);

        var persistedTask = await dbContext.Tasks.FirstAsync();
        Assert.Equal(123, persistedTask.GitHubIssueNumber);
    }

    [Fact]
    public async Task CreateAsync_WhenGitHubFails_DoesNotPersistTask()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateContext(dbName);
        var project = SeedProject(dbContext);

        var tokenProtector = new Mock<ITokenProtector>();
        tokenProtector.Setup(item => item.Unprotect("encrypted-token")).Returns("plain-token");

        var gitHubIssueService = new Mock<IGitHubIssueService>();
        gitHubIssueService
            .Setup(item => item.CreateIssueAsync(project.RepoOwner, project.RepoName, "New task", null, "plain-token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitHubIntegrationException("GitHub unavailable."));

        var service = new TaskService(dbContext, tokenProtector.Object, gitHubIssueService.Object, Mock.Of<ILogger<TaskService>>());

        await Assert.ThrowsAsync<GitHubIntegrationException>(() => service.CreateAsync(project.Id, new CreateTaskRequest("New task")));
        Assert.Empty(await dbContext.Tasks.ToListAsync());
    }

    [Fact]
    public async Task CreateAsync_WhenDatabaseSaveFails_CompensatesByClosingIssue()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var dbContext = CreateContext(dbName);
        var project = SeedProject(dbContext);

        var cts = new CancellationTokenSource();

        var tokenProtector = new Mock<ITokenProtector>();
        tokenProtector.Setup(item => item.Unprotect("encrypted-token")).Returns("plain-token");

        var gitHubIssueService = new Mock<IGitHubIssueService>();
        gitHubIssueService
            .Setup(item => item.CreateIssueAsync(project.RepoOwner, project.RepoName, "New task", null, "plain-token", It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                cts.Cancel();
                return Task.FromResult(999);
            });

        gitHubIssueService
            .Setup(item => item.CloseIssueAsync(project.RepoOwner, project.RepoName, 999, "plain-token", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TaskService(dbContext, tokenProtector.Object, gitHubIssueService.Object, Mock.Of<ILogger<TaskService>>());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CreateAsync(project.Id, new CreateTaskRequest("New task"), cts.Token));

        gitHubIssueService.Verify(
            item => item.CloseIssueAsync(project.RepoOwner, project.RepoName, 999, "plain-token", It.IsAny<CancellationToken>()),
            Times.Once);

        Assert.Empty(await dbContext.Tasks.ToListAsync());
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenMovingToDone_ClosesGitHubIssueAndPersists()
    {
        var dbName = Guid.NewGuid().ToString();
        var taskId = await SeedTaskAsync(dbName, TaskStatus.InProgress);

        await using var dbContext = CreateContext(dbName);
        var project = await dbContext.Projects.AsNoTracking().FirstAsync();

        var tokenProtector = new Mock<ITokenProtector>();
        tokenProtector.Setup(item => item.Unprotect("encrypted-token")).Returns("plain-token");

        var gitHubIssueService = new Mock<IGitHubIssueService>();
        gitHubIssueService
            .Setup(item => item.CloseIssueAsync(project.RepoOwner, project.RepoName, 456, "plain-token", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TaskService(dbContext, tokenProtector.Object, gitHubIssueService.Object, Mock.Of<ILogger<TaskService>>());

        var result = await service.UpdateStatusAsync(taskId, new UpdateTaskStatusRequest("Done"));

        Assert.NotNull(result);
        Assert.Equal("Done", result!.Status);
        Assert.NotNull(result.CompletedAt);

        gitHubIssueService.Verify(
            item => item.CloseIssueAsync(project.RepoOwner, project.RepoName, 456, "plain-token", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenGitHubCloseFails_DoesNotPersistDoneStatus()
    {
        var dbName = Guid.NewGuid().ToString();
        var taskId = await SeedTaskAsync(dbName, TaskStatus.InProgress);

        await using var dbContext = CreateContext(dbName);
        var project = await dbContext.Projects.AsNoTracking().FirstAsync();

        var tokenProtector = new Mock<ITokenProtector>();
        tokenProtector.Setup(item => item.Unprotect("encrypted-token")).Returns("plain-token");

        var gitHubIssueService = new Mock<IGitHubIssueService>();
        gitHubIssueService
            .Setup(item => item.CloseIssueAsync(project.RepoOwner, project.RepoName, 456, "plain-token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitHubIntegrationException("Close failed."));

        var service = new TaskService(dbContext, tokenProtector.Object, gitHubIssueService.Object, Mock.Of<ILogger<TaskService>>());

        await Assert.ThrowsAsync<GitHubIntegrationException>(() =>
            service.UpdateStatusAsync(taskId, new UpdateTaskStatusRequest("Done"))!);

        await using var verificationContext = CreateContext(dbName);
        var persistedTask = await verificationContext.Tasks.AsNoTracking().FirstAsync(item => item.Id == taskId);

        Assert.Equal(TaskStatus.InProgress, persistedTask.Status);
        Assert.Null(persistedTask.CompletedAt);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenMovingFromDoneToInProgress_ReopensGitHubIssueAndPersists()
    {
        var dbName = Guid.NewGuid().ToString();
        var taskId = await SeedTaskAsync(dbName, TaskStatus.Done);

        await using var dbContext = CreateContext(dbName);
        var project = await dbContext.Projects.AsNoTracking().FirstAsync();

        var tokenProtector = new Mock<ITokenProtector>();
        tokenProtector.Setup(item => item.Unprotect("encrypted-token")).Returns("plain-token");

        var gitHubIssueService = new Mock<IGitHubIssueService>();
        gitHubIssueService
            .Setup(item => item.ReopenIssueAsync(project.RepoOwner, project.RepoName, 456, "plain-token", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new TaskService(dbContext, tokenProtector.Object, gitHubIssueService.Object, Mock.Of<ILogger<TaskService>>());

        var result = await service.UpdateStatusAsync(taskId, new UpdateTaskStatusRequest("InProgress"));

        Assert.NotNull(result);
        Assert.Equal("InProgress", result!.Status);
        Assert.Null(result.CompletedAt);

        gitHubIssueService.Verify(
            item => item.ReopenIssueAsync(project.RepoOwner, project.RepoName, 456, "plain-token", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenGitHubReopenFails_DoesNotPersistInProgressStatus()
    {
        var dbName = Guid.NewGuid().ToString();
        var taskId = await SeedTaskAsync(dbName, TaskStatus.Done);

        await using var dbContext = CreateContext(dbName);
        var project = await dbContext.Projects.AsNoTracking().FirstAsync();

        var tokenProtector = new Mock<ITokenProtector>();
        tokenProtector.Setup(item => item.Unprotect("encrypted-token")).Returns("plain-token");

        var gitHubIssueService = new Mock<IGitHubIssueService>();
        gitHubIssueService
            .Setup(item => item.ReopenIssueAsync(project.RepoOwner, project.RepoName, 456, "plain-token", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new GitHubIntegrationException("Reopen failed."));

        var service = new TaskService(dbContext, tokenProtector.Object, gitHubIssueService.Object, Mock.Of<ILogger<TaskService>>());

        await Assert.ThrowsAsync<GitHubIntegrationException>(() =>
            service.UpdateStatusAsync(taskId, new UpdateTaskStatusRequest("InProgress"))!);

        await using var verificationContext = CreateContext(dbName);
        var persistedTask = await verificationContext.Tasks.AsNoTracking().FirstAsync(item => item.Id == taskId);

        Assert.Equal(TaskStatus.Done, persistedTask.Status);
        Assert.NotNull(persistedTask.CompletedAt);
    }

    private static ApplicationDbContext CreateContext(string dbName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new ApplicationDbContext(options);
    }

    private static DevBoard.Domain.Entities.Project SeedProject(ApplicationDbContext dbContext)
    {
        var project = new DevBoard.Domain.Entities.Project("DevBoard", "carra", "devboard", "encrypted-token");
        dbContext.Projects.Add(project);
        dbContext.SaveChanges();
        return project;
    }

    private static async Task<Guid> SeedTaskAsync(string dbName, TaskStatus initialStatus)
    {
        await using var dbContext = CreateContext(dbName);
        var project = SeedProject(dbContext);

        var task = new TaskEntity(project.Id, "Sync from GitHub");
        task.SetGitHubIssueNumber(456);
        if (initialStatus == TaskStatus.InProgress)
        {
            task.MoveTo(TaskStatus.InProgress);
        }
        else if (initialStatus == TaskStatus.Done)
        {
            task.MoveTo(TaskStatus.InProgress);
            task.MoveTo(TaskStatus.Done);
        }

        dbContext.Tasks.Add(task);
        await dbContext.SaveChangesAsync();
        return task.Id;
    }
}
